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
public sealed class YeuCauController(AppDbContext context, LeaveService leave) : Controller
{
    public async Task<IActionResult> Index(string? tab = null)
    {
        var actorId = CurrentEmployeeId();
        var isHrOrAdmin = User.IsInRole(AuthorizationRoles.HumanResources) ||
                          User.IsInRole(AuthorizationRoles.Administrator);
        var managerOnly = User.IsInRole(AuthorizationRoles.Manager) && !isHrOrAdmin;

        var leaveQuery = context.NghiPheps.AsNoTracking()
            .Include(x => x.NhanVien).ThenInclude(x => x!.ChucVu)
            .Include(x => x.MinhChungs)
            .AsQueryable();
        var overtimeQuery = context.DangKyTangCas.AsNoTracking()
            .Include(x => x.NhanVien).ThenInclude(x => x!.ChucVu)
            .Include(x => x.MinhChungs)
            .AsQueryable();

        if (managerOnly)
        {
            if (!actorId.HasValue) return Forbid();
            var actor = await context.NhanViens.AsNoTracking().FirstOrDefaultAsync(x => x.ID == actorId.Value);
            if (actor is null) return Forbid();
            var actorRoute = await leave.GetApprovalRouteAsync(actor.ID);
            var isHrManager = actorRoute.HrManagerId == actor.ID;

            leaveQuery = isHrManager
                ? leaveQuery.Where(x => x.NhanVienID == actor.ID ||
                    (x.NhanVien != null && x.NhanVien.ChucVu != null &&
                     (("," + x.NhanVien.ChucVu.PhanQuyen + ",").Contains(",QuanLy,") ||
                      ("," + x.NhanVien.ChucVu.PhanQuyen + ",").Contains(",HR,"))))
                : leaveQuery.Where(x => x.NhanVienID == actor.ID ||
                    (x.NhanVien != null && x.NhanVien.PhongBanID == actor.PhongBanID));

            overtimeQuery = isHrManager
                ? overtimeQuery.Where(x => x.NhanVien != null && x.NhanVien.ChucVu != null &&
                    (("," + x.NhanVien.ChucVu.PhanQuyen + ",").Contains(",QuanLy,") ||
                     ("," + x.NhanVien.ChucVu.PhanQuyen + ",").Contains(",HR,")))
                : overtimeQuery.Where(x => x.NhanVien != null && x.NhanVien.PhongBanID == actor.PhongBanID);
        }

        var leaves = await leaveQuery.OrderByDescending(x => x.NgayTao).Take(100).ToListAsync();
        var overtime = await overtimeQuery.OrderByDescending(x => x.NgayTao).Take(100).ToListAsync();
        var decidableLeaves = new HashSet<int>();
        foreach (var item in leaves)
            if (await CanDecideLeaveAsync(item, actorId)) decidableLeaves.Add(item.ID);
        var decidableOvertime = new HashSet<int>();
        foreach (var item in overtime)
            if (await CanDecideOvertimeAsync(item, actorId)) decidableOvertime.Add(item.ID);

        var adjustments = isHrOrAdmin
            ? await context.DieuChinhChamCongs.AsNoTracking().Include(x => x.NhanVien).Include(x => x.MinhChungs)
                .OrderByDescending(x => x.NgayTao).Take(100).ToListAsync()
            : [];
        var profileChanges = isHrOrAdmin
            ? await context.YeuCauCapNhatThongTinCaNhans.AsNoTracking().Include(x => x.NhanVien)
                .OrderByDescending(x => x.NgayTao).Take(100).ToListAsync()
            : [];

        ViewBag.ActiveTab = tab;
        return View(new ApprovalRequestCenterViewModel
        {
            Leaves = leaves,
            DecidableLeaveIds = decidableLeaves,
            OvertimeRequests = overtime,
            DecidableOvertimeIds = decidableOvertime,
            AttendanceAdjustments = adjustments,
            ProfileChanges = profileChanges,
            CanReviewHrRequests = isHrOrAdmin
        });
    }

    private async Task<bool> CanDecideLeaveAsync(NghiPhep item, int? actorId)
    {
        if (item.Trangthai is not (LeaveStatuses.Pending or ApprovalStatuses.ManagerApproved)) return false;
        if (User.IsInRole(AuthorizationRoles.Administrator)) return true;
        // Sau khi quản lý phòng duyệt, mọi tài khoản HR đều được thực hiện bước duyệt cuối.
        // HR không bắt buộc phải có hồ sơ nhân viên/NhanVienID mới nhìn thấy nút này.
        if (item.Trangthai == ApprovalStatuses.ManagerApproved)
            return User.IsInRole(AuthorizationRoles.HumanResources);
        if (!actorId.HasValue || actorId.Value == item.NhanVienID) return false;
        var route = await leave.GetApprovalRouteAsync(item.NhanVienID);
        if (route.Error is not null) return false;
        return route.Route switch
        {
            LeaveApprovalRoute.DepartmentManagerThenHr => route.DepartmentManagerId == actorId,
            LeaveApprovalRoute.HrManagerDirect => route.HrManagerId == actorId,
            _ => false
        };
    }

    private async Task<bool> CanDecideOvertimeAsync(DangKyTangCa item, int? actorId)
    {
        if (item.TrangThai is ApprovalStatuses.Approved or ApprovalStatuses.Rejected) return false;
        var route = await leave.GetApprovalRouteAsync(item.NhanVienID);
        if (route.Error is not null) return false;
        return route.Route switch
        {
            LeaveApprovalRoute.DepartmentManagerThenHr =>
                item.TrangThai == ApprovalStatuses.Pending && actorId == route.DepartmentManagerId ||
                item.TrangThai == ApprovalStatuses.ManagerApproved &&
                (User.IsInRole(AuthorizationRoles.HumanResources) || User.IsInRole(AuthorizationRoles.Administrator)),
            LeaveApprovalRoute.HrManagerDirect =>
                item.TrangThai == ApprovalStatuses.Pending && actorId == route.HrManagerId,
            _ => false
        };
    }

    private int? CurrentEmployeeId() =>
        int.TryParse(User.FindFirstValue("NhanVienID"), out var id) ? id : null;
}
