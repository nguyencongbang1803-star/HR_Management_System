using System.Net.Http.Json;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.Data;
using QLNS.Models.Entities;
using QLNS.Models.ViewModels;
using QLNS.Services;

namespace QLNS.Areas.HR.Controllers;

[Area("HR")]
[Authorize(Roles = "Admin")]
public class TaiKhoanController(
    AppDbContext context,
    UserManager<TaiKhoan> userManager,
    AuditService audit,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.Employees = await context.NhanViens.AsNoTracking()
            .Include(x => x.ChucVu)
            .Where(x => x.Trangthai && x.TaiKhoan == null)
            .OrderBy(x => x.Ten).ToListAsync();
        return View(await userManager.Users.AsNoTracking()
            .Include(x => x.NhanVien).ThenInclude(x => x!.ChucVu)
            .OrderBy(x => x.Email).ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Create(AccountCreateViewModel model)
    {
        var employee = model.NhanVienID.HasValue
            ? await context.NhanViens.Include(x => x.ChucVu)
                .FirstOrDefaultAsync(x => x.ID == model.NhanVienID && x.Trangthai)
            : null;
        if (employee is null)
            ModelState.AddModelError(nameof(model.NhanVienID), "Hãy chọn một nhân viên đang làm việc.");
        else if (await context.TaiKhoans.AnyAsync(x => x.NhanVienID == employee.ID))
            ModelState.AddModelError(nameof(model.NhanVienID), "Nhân viên đã có tài khoản.");
        else if (employee.ChucVu is null)
            ModelState.AddModelError(nameof(model.NhanVienID), "Nhân viên chưa được gán chức vụ.");

        var email = employee?.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (employee is not null && !new EmailAddressAttribute().IsValid(email))
            ModelState.AddModelError(nameof(model.NhanVienID), "Nhân viên chưa có Email hợp lệ. Hãy cập nhật hồ sơ nhân viên trước.");
        else if (!string.IsNullOrEmpty(email) && await userManager.FindByEmailAsync(email) is not null)
            ModelState.AddModelError(nameof(model.NhanVienID), "Email của nhân viên đã được sử dụng cho tài khoản khác.");

        var employeeCode = employee?.MaNhanVien.Trim().ToUpperInvariant() ?? string.Empty;
        if (employee is not null && await context.TaiKhoans.AnyAsync(x => x.MaNhanVien == employeeCode))
            ModelState.AddModelError(nameof(model.NhanVienID), "Mã nhân viên đã có tài khoản.");

        if (!ModelState.IsValid) return Error("Không thể tạo tài khoản: dữ liệu không hợp lệ hoặc bị trùng.");

        var roles = AuthorizationRoles.Parse(employee!.ChucVu?.PhanQuyen);
        var role = AuthorizationRoles.All.Reverse().First(roles.Contains);
        var account = new TaiKhoan
        {
            NhanVienID = employee.ID,
            MaNhanVien = employeeCode,
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            Role = role
        };
        var result = await userManager.CreateAsync(account, model.Password);
        if (!result.Succeeded) return Error(Errors(result));

        result = await userManager.AddToRolesAsync(account, roles);
        if (!result.Succeeded)
        {
            await userManager.DeleteAsync(account);
            return Error(Errors(result));
        }

        audit.Add("Create", "TaiKhoan", account.Id.ToString(), account.Email);
        await context.SaveChangesAsync();
        TempData["Success"] = "Đã tạo tài khoản.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> StartFaceEnrollment(int id, CancellationToken cancellationToken)
    {
        var account = await context.TaiKhoans.AsNoTracking().Include(x => x.NhanVien)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (account?.NhanVien is null)
            return Error("Tài khoản chưa được liên kết với nhân viên để đăng ký khuôn mặt.");
        if (!account.NhanVien.Trangthai)
            return Error("Không thể đăng ký khuôn mặt cho nhân viên đã ngừng làm việc.");

        var baseUrl = (configuration["FaceAttendance:ApiBaseUrl"] ?? "http://localhost:5000").TrimEnd('/');
        var deviceId = configuration["FaceAttendance:DeviceId"] ?? "front-door";
        try
        {
            using var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            using var response = await client.PostAsJsonAsync(
                $"{baseUrl}/api/kiosk-mode/enrollment",
                new { DeviceId = deviceId, EmployeeId = account.NhanVien.ID },
                cancellationToken);
            if (!response.IsSuccessStatusCode)
                return Error("Kiosk không nhận được yêu cầu đăng ký khuôn mặt. Vui lòng kiểm tra trạng thái thiết bị.");
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return Error("Không kết nối được kiosk cửa văn phòng. Hãy kiểm tra dịch vụ nhận diện khuôn mặt.");
        }

        audit.Add("StartFaceEnrollment", "NhanVien", account.NhanVien.ID.ToString(), deviceId);
        await context.SaveChangesAsync(cancellationToken);
        TempData["Success"] = $"Hệ thống chuyển sang chế độ đăng ký khuôn mặt cho nhân viên {account.NhanVien.Ten}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> RemoveFaceEnrollment(int id, CancellationToken cancellationToken)
    {
        var account = await context.TaiKhoans.AsNoTracking().Include(x => x.NhanVien)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (account?.NhanVien is null)
            return Error("Tài khoản chưa được liên kết với nhân viên.");

        var baseUrl = (configuration["FaceAttendance:ApiBaseUrl"] ?? "http://localhost:5000").TrimEnd('/');
        try
        {
            using var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            using var response = await client.DeleteAsync(
                $"{baseUrl}/api/employees/{account.NhanVien.ID}/face", cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return Error("Nhân viên này chưa đăng ký khuôn mặt.");
            if (!response.IsSuccessStatusCode)
                return Error("Không thể xóa dữ liệu khuôn mặt. Vui lòng kiểm tra kiosk.");
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return Error("Không kết nối được hệ thống nhận diện khuôn mặt.");
        }

        audit.Add("RemoveFaceEnrollment", "NhanVien", account.NhanVien.ID.ToString());
        await context.SaveChangesAsync(cancellationToken);
        TempData["Success"] = $"Đã xóa dữ liệu khuôn mặt của {account.NhanVien.Ten}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(int id, string newPassword)
    {
        var account = await userManager.FindByIdAsync(id.ToString());
        if (account is null) return NotFound();
        var token = await userManager.GeneratePasswordResetTokenAsync(account);
        var result = await userManager.ResetPasswordAsync(account, token, newPassword);
        if (!result.Succeeded) return Error(Errors(result));

        audit.Add("ResetPassword", "TaiKhoan", id.ToString());
        await context.SaveChangesAsync();
        TempData["Success"] = "Đã đặt lại mật khẩu bằng Identity.";
        return RedirectToAction(nameof(Index));
    }

    private RedirectToActionResult Error(string message)
    {
        TempData["Error"] = message;
        return RedirectToAction(nameof(Index));
    }
    private static string Errors(IdentityResult result) =>
        string.Join(" ", result.Errors.Select(IdentityError));

    private static string IdentityError(IdentityError error) => error.Code switch
    {
        "PasswordTooShort" => "Mật khẩu phải có ít nhất 8 ký tự.",
        "PasswordRequiresDigit" => "Mật khẩu phải có ít nhất một chữ số.",
        "PasswordRequiresNonAlphanumeric" => "Mật khẩu phải có ít nhất một ký tự đặc biệt.",
        _ => error.Description
    };
}
