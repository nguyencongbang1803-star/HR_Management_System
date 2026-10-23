using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.Data;
using QLNS.Models.Entities;
using QLNS.Models.ViewModels;
using QLNS.Services;

namespace QLNS.Areas.HR.Controllers;

[Area("HR")]
[Authorize(Roles = "Admin,HR,QuanLy")]
public class QuyTrinhNhanSuController(AppDbContext context, AuditService audit) : Controller
{
    public async Task<IActionResult> Index()
    {
        var requests = context.YeuCauNhanLuc.AsNoTracking()
            .Include(x => x.PhongBan).Include(x => x.NhanVienNoiBo).AsQueryable();
        var reviews = context.DanhGiaHieuSuat.AsNoTracking()
            .Include(x => x.NhanVien).ThenInclude(x => x!.PhongBan).AsQueryable();
        var departments = context.PhongBans.AsNoTracking().AsQueryable();

        if (IsManagerOnly())
        {
            var email = CurrentUser();
            var employeeId = CurrentEmployeeId();
            var departmentId = await context.NhanViens.Where(x => x.ID == employeeId)
                .Select(x => x.PhongBanID).FirstOrDefaultAsync();
            requests = requests.Where(x => x.NguoiDeXuat == email);
            reviews = reviews.Where(x => x.NguoiDanhGia == email);
            departments = departments.Where(x => departmentId.HasValue && x.ID == departmentId);
        }

        var employees = await ScopedEmployees().AsNoTracking().OrderBy(x => x.Ten).ToListAsync();
        return View(new HrWorkflowIndexViewModel
        {
            WorkforceRequests = await requests.OrderByDescending(x => x.NgayTao).ToListAsync(),
            PerformanceReviews = await reviews.OrderByDescending(x => x.NgayDanhGia).ToListAsync(),
            Departments = await departments.OrderBy(x => x.Tenphongban).ToListAsync(),
            Employees = employees
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateRequest(WorkforceRequestFormViewModel model)
    {
        if (!await context.PhongBans.AnyAsync(x => x.ID == model.PhongBanID))
            ModelState.AddModelError(nameof(model.PhongBanID), "Phòng ban không tồn tại.");
        if (IsManagerOnly() && !await CanManageDepartment(model.PhongBanID))
            return Forbid();
        if (!ModelState.IsValid) return await InvalidForm("Thông tin yêu cầu nhân lực không hợp lệ.");

        var code = await NextRequestCode();
        var item = new YeuCauNhanLuc
        {
            MaYeuCau = code,
            PhongBanID = model.PhongBanID,
            ViTriCanBoSung = model.ViTriCanBoSung.Trim(),
            SoLuong = model.SoLuong,
            LyDo = model.LyDo.Trim(),
            NguoiDeXuat = CurrentUser()
        };
        context.Add(item);
        await context.SaveChangesAsync();
        audit.Add("Create", "YeuCauNhanLuc", item.MaYeuCau, item.LyDo);
        await context.SaveChangesAsync();
        TempData["Success"] = $"Đã gửi yêu cầu nhân lực {item.MaYeuCau} đến bộ phận Nhân sự.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPost]
    public async Task<IActionResult> PlanRequest(WorkforcePlanViewModel model)
    {
        var item = await context.YeuCauNhanLuc.FindAsync(model.ID);
        if (item is null) return NotFound();
        if (item.TrangThai != WorkforceRequestStatuses.Pending)
            return Error("Yêu cầu đã được lập kế hoạch hoặc kết thúc.");
        if (model.HinhThucBoSung == WorkforceSupplyMethods.InternalTransfer)
        {
            if (!model.NhanVienNoiBoID.HasValue ||
                !await context.NhanViens.AnyAsync(x => x.ID == model.NhanVienNoiBoID && x.Trangthai))
                ModelState.AddModelError(nameof(model.NhanVienNoiBoID), "Cần chọn một nhân viên nội bộ đang làm việc.");
        }
        if (!ModelState.IsValid) return await InvalidForm("Kế hoạch bổ sung nhân sự chưa hợp lệ.");

        item.HinhThucBoSung = model.HinhThucBoSung;
        item.MoTaCongViec = model.MoTaCongViec.Trim();
        item.NhanVienNoiBoID = model.HinhThucBoSung == WorkforceSupplyMethods.InternalTransfer
            ? model.NhanVienNoiBoID : null;
        item.TrangThai = model.HinhThucBoSung == WorkforceSupplyMethods.InternalTransfer
            ? WorkforceRequestStatuses.InternalSelection : WorkforceRequestStatuses.Recruiting;
        item.GhiChuXuLy = model.GhiChuXuLy?.Trim();
        item.NguoiXuLy = CurrentUser();
        item.NgayCapNhat = DateTime.UtcNow;
        audit.Add("Plan", "YeuCauNhanLuc", item.MaYeuCau, item.HinhThucBoSung);
        await context.SaveChangesAsync();
        TempData["Success"] = "Đã lập và lưu kế hoạch bổ sung nhân sự.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPost]
    public async Task<IActionResult> UpdateStage(WorkforceStageViewModel model)
    {
        var item = await context.YeuCauNhanLuc.FindAsync(model.ID);
        if (item is null) return NotFound();
        if (!CanTransition(item.TrangThai, model.TrangThai))
            ModelState.AddModelError(nameof(model.TrangThai), "Bước xử lý không đúng thứ tự của quy trình.");
        if (model.TrangThai == WorkforceRequestStatuses.Interviewing && string.IsNullOrWhiteSpace(model.TenUngVien))
            ModelState.AddModelError(nameof(model.TenUngVien), "Cần nhập tên ứng viên được phỏng vấn.");
        if (item.HinhThucBoSung == WorkforceSupplyMethods.InternalTransfer &&
            model.TrangThai == WorkforceRequestStatuses.Onboarding && !model.NhanVienNoiBoID.HasValue && !item.NhanVienNoiBoID.HasValue)
            ModelState.AddModelError(nameof(model.NhanVienNoiBoID), "Cần chọn nhân viên được điều chuyển.");
        if (!ModelState.IsValid) return await InvalidForm("Không thể cập nhật bước xử lý yêu cầu.");

        item.TrangThai = model.TrangThai;
        item.TenUngVien = model.TenUngVien?.Trim() ?? item.TenUngVien;
        item.NgayPhongVan = model.NgayPhongVan ?? item.NgayPhongVan;
        item.NhanVienNoiBoID = model.NhanVienNoiBoID ?? item.NhanVienNoiBoID;
        item.GhiChuXuLy = model.GhiChuXuLy?.Trim();
        item.NguoiXuLy = CurrentUser();
        item.NgayCapNhat = DateTime.UtcNow;
        audit.Add("UpdateStage", "YeuCauNhanLuc", item.MaYeuCau, model.TrangThai);
        await context.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật tiến độ yêu cầu nhân lực.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> CreateReview(PerformanceReviewFormViewModel model)
    {
        if (!await ScopedEmployees().AnyAsync(x => x.ID == model.NhanVienID)) return Forbid();
        if (await context.DanhGiaHieuSuat.AnyAsync(x => x.NhanVienID == model.NhanVienID &&
            x.TuNgay <= model.DenNgay.Date && x.DenNgay >= model.TuNgay.Date))
            ModelState.AddModelError(string.Empty, "Nhân viên đã có kỳ đánh giá trùng thời gian.");
        if (!ModelState.IsValid) return await InvalidForm("Phiếu đánh giá hiệu quả chưa hợp lệ.");

        var item = new DanhGiaHieuSuat
        {
            NhanVienID = model.NhanVienID,
            TuNgay = model.TuNgay.Date,
            DenNgay = model.DenNgay.Date,
            ChatLuongCongViec = model.ChatLuongCongViec,
            TienDoCongViec = model.TienDoCongViec,
            TinhThanHopTac = model.TinhThanHopTac,
            KyLuat = model.KyLuat,
            DiemManh = model.DiemManh?.Trim(),
            NoiDungCanCaiThien = model.NoiDungCanCaiThien?.Trim(),
            DeXuat = model.DeXuat?.Trim(),
            NguoiDanhGia = CurrentUser()
        };
        context.Add(item);
        await context.SaveChangesAsync();
        audit.Add("Create", "DanhGiaHieuSuat", item.ID.ToString(), $"Điểm: {item.DiemTrungBinh:0.00}");
        await context.SaveChangesAsync();
        TempData["Success"] = "Đã lưu phiếu đánh giá hiệu quả công việc.";
        return RedirectToAction(nameof(Index));
    }

    private IQueryable<NhanVien> ScopedEmployees()
    {
        var query = context.NhanViens.Where(x => x.Trangthai);
        if (!IsManagerOnly()) return query;
        var employeeId = CurrentEmployeeId();
        var departmentId = context.NhanViens.Where(x => x.ID == employeeId).Select(x => x.PhongBanID).FirstOrDefault();
        return departmentId.HasValue ? query.Where(x => x.PhongBanID == departmentId) : query.Where(x => false);
    }

    private async Task<bool> CanManageDepartment(int departmentId)
    {
        var employeeId = CurrentEmployeeId();
        return employeeId.HasValue && await context.PhongBans.AnyAsync(x => x.ID == departmentId &&
            (x.Matruongphong == employeeId || context.NhanViens.Any(n => n.ID == employeeId && n.PhongBanID == departmentId)));
    }

    private bool IsManagerOnly()=>User.IsInRole(AuthorizationRoles.Manager)&&!User.IsInRole(AuthorizationRoles.HumanResources)&&!User.IsInRole(AuthorizationRoles.Administrator);

    private async Task<string> NextRequestCode()
    {
        var prefix = $"YCNL-{DateTime.Today:yyyyMMdd}-";
        var latest = await context.YeuCauNhanLuc.Where(x => x.MaYeuCau.StartsWith(prefix))
            .OrderByDescending(x => x.MaYeuCau).Select(x => x.MaYeuCau).FirstOrDefaultAsync();
        var sequence = latest is not null && int.TryParse(latest[prefix.Length..], out var value) ? value + 1 : 1;
        return $"{prefix}{sequence:000}";
    }

    private static bool CanTransition(string current, string next) => next == WorkforceRequestStatuses.Rejected ||
        (current == WorkforceRequestStatuses.Recruiting && next == WorkforceRequestStatuses.Interviewing) ||
        (current is WorkforceRequestStatuses.Interviewing or WorkforceRequestStatuses.InternalSelection && next == WorkforceRequestStatuses.Onboarding) ||
        (current == WorkforceRequestStatuses.Onboarding && next == WorkforceRequestStatuses.Completed);

    private string CurrentUser() => User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? "unknown";
    private int? CurrentEmployeeId() => int.TryParse(User.FindFirstValue("NhanVienID"), out var id) ? id : null;
    private RedirectToActionResult Error(string message) { TempData["Error"] = message; return RedirectToAction(nameof(Index)); }
    private async Task<RedirectToActionResult> InvalidForm(string message)
    {
        TempData["Error"] = $"{message} {string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).Take(3))}";
        await Task.CompletedTask;
        return RedirectToAction(nameof(Index));
    }
}
