using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.Models.Entities;
using QLNS.Models.ViewModels;
using QLNS.Data;

namespace QLNS.Controllers;

public class AuthController(
    UserManager<TaiKhoan> userManager,
    SignInManager<TaiKhoan> signInManager,
    AppDbContext context) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return LandingPage();

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var loginIdentifier = model.TaiKhoan.Trim();
        var isEmail = loginIdentifier.Contains('@');
        var normalizedEmail = isEmail ? userManager.NormalizeEmail(loginIdentifier) : null;
        var employeeCode = isEmail ? null : loginIdentifier.ToUpperInvariant();
        var accounts = userManager.Users
            .Include(x => x.NhanVien).ThenInclude(x => x!.ChucVu);
        var account = isEmail
            ? await accounts.SingleOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail)
            : await accounts.SingleOrDefaultAsync(x => x.MaNhanVien == employeeCode);
        if (account is null)
        {
            ModelState.AddModelError(string.Empty, "Email, mã nhân viên hoặc mật khẩu không chính xác.");
            return View(model);
        }

        if (account.NhanVien is { Trangthai: false })
        {
            ModelState.AddModelError(string.Empty, "Tài khoản nhân viên đã ngừng hoạt động.");
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(
            account, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty,
                "Tài khoản tạm khóa 15 phút do đăng nhập sai quá nhiều lần.");
            return View(model);
        }
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Email, mã nhân viên hoặc mật khẩu không chính xác.");
            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);

        var roles = account.NhanVienID.HasValue
            ? AuthorizationRoles.Parse(account.NhanVien?.ChucVu?.PhanQuyen).ToList()
            : (await userManager.GetRolesAsync(account)).ToList();
        if (roles.Count==1&&roles.Contains(AuthorizationRoles.Employee))
            return RedirectToAction("Index", "Home", new { area = "Employee" });
        if (roles.Contains(AuthorizationRoles.Manager)&&!roles.Contains(AuthorizationRoles.HumanResources)&&!roles.Contains(AuthorizationRoles.Administrator))
        {
            var isHrManager = account.NhanVienID.HasValue && await context.PhongBans.AnyAsync(x =>
                x.Matruongphong == account.NhanVienID.Value && x.NhanViens.Any(n =>
                    n.ChucVu != null && (","+n.ChucVu.PhanQuyen+",").Contains(",HR,")));
            if (!isHrManager) return RedirectToAction("Index", "NhanVien", new { area = "HR" });
        }
        return RedirectToAction("Index", "Dashboard", new { area = "HR" });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var account = await userManager.GetUserAsync(User);
        if (account is null) return Challenge();

        var result = await userManager.ChangePasswordAsync(
            account, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, IdentityError(error));
            return View(model);
        }

        await signInManager.RefreshSignInAsync(account);
        TempData["Success"] = "Đã đổi mật khẩu thành công.";
        return LandingPage();
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    private static string IdentityError(IdentityError error) => error.Code switch
    {
        "PasswordMismatch" => "Mật khẩu hiện tại không đúng.",
        "PasswordTooShort" => "Mật khẩu mới phải có ít nhất 8 ký tự.",
        "PasswordRequiresDigit" => "Mật khẩu mới phải có ít nhất một chữ số.",
        "PasswordRequiresLower" => "Mật khẩu mới phải có ít nhất một chữ thường.",
        "PasswordRequiresUpper" => "Mật khẩu mới phải có ít nhất một chữ hoa.",
        "PasswordRequiresNonAlphanumeric" => "Mật khẩu mới phải có ít nhất một ký tự đặc biệt.",
        _ => error.Description
    };

    private IActionResult LandingPage()
    {
        if (User.IsInRole(AuthorizationRoles.Employee) && !User.IsInRole(AuthorizationRoles.Manager) &&
            !User.IsInRole(AuthorizationRoles.HumanResources) && !User.IsInRole(AuthorizationRoles.Administrator))
            return RedirectToAction("Index", "Home", new { area = "Employee" });
        if (User.IsInRole(AuthorizationRoles.Manager) && !User.IsInRole(AuthorizationRoles.HumanResources) &&
            !User.IsInRole(AuthorizationRoles.Administrator))
            return RedirectToAction("Index", "NhanVien", new { area = "HR" });
        return RedirectToAction("Index", "Dashboard", new { area = "HR" });
    }
}
