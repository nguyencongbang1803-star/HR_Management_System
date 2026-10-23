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
public class NghiPhepController(
    AppDbContext context,
    AuditService audit,
    LeaveService leave,
    FileStorageService files) : Controller
{
    public async Task<IActionResult> Index(string? status = null, string? employeeSearch = null, int? employeeId = null)
    {
        var query = context.NghiPheps.AsNoTracking()
            .Include(x => x.NhanVien).ThenInclude(x => x!.ChucVu)
            .Include(x => x.MinhChungs)
            .AsQueryable();

        if (IsManagerOnly())
        {
            var actor = await CurrentEmployeeAsync();
            if (actor is null) return Forbid();
            var route = await leave.GetApprovalRouteAsync(actor.ID);
            var isHrManager = route.HrManagerId == actor.ID;
            query = isHrManager
                ? query.Where(x => x.NhanVienID == actor.ID ||
                    (x.NhanVien != null && x.NhanVien.ChucVu != null &&
                     ((","+x.NhanVien.ChucVu.PhanQuyen+",").Contains(",QuanLy,") ||
                      (","+x.NhanVien.ChucVu.PhanQuyen+",").Contains(",HR,"))))
                : query.Where(x => x.NhanVienID == actor.ID ||
                    (x.NhanVien != null && x.NhanVien.PhongBanID == actor.PhongBanID));
        }

        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Trangthai == status);
        if (employeeId.HasValue) query = query.Where(x => x.NhanVienID == employeeId.Value);
        else if (!string.IsNullOrWhiteSpace(employeeSearch))
        {
            var term = employeeSearch.Trim();
            query = query.Where(x => x.NhanVien != null &&
                (x.NhanVien.MaNhanVien.Contains(term) || x.NhanVien.Ten.Contains(term)));
        }

        var items = await query.OrderByDescending(x => x.NgayTao).ToListAsync();
        var canDecideIds = new HashSet<int>();
        foreach (var item in items)
            if (await CanCurrentUserDecideAsync(item)) canDecideIds.Add(item.ID);

        ViewBag.Status = status;
        ViewBag.EmployeeSearch = employeeSearch;
        ViewBag.CanDecideIds = canDecideIds;
        return View(items);
    }

    public async Task<IActionResult> Create()
    {
        await LoadEmployeesAsync();
        return View(new LeaveFormViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(LeaveFormViewModel model)
    {
        if (model.NhanVienID > 0 && !await CanManageEmployeeAsync(model.NhanVienID)) return Forbid();
        var type = await context.LoaiNghiPheps.AsNoTracking()
            .SingleOrDefaultAsync(x => x.MaLoai == "PHEP_NAM" && x.HoatDong);
        model.LoaiNghiPhepID = type?.ID;
        if (type is null) ModelState.AddModelError(string.Empty, "Hệ thống chưa cấu hình loại nghỉ phép năm.");
        var error = await leave.ValidateRequestAsync(
            model.NhanVienID, model.LoaiNghiPhepID, model.Ngaybatdau, model.NgayKetThuc, model.LaDotXuat);
        if (error is not null) ModelState.AddModelError(string.Empty, error);
        var route = await leave.GetApprovalRouteAsync(model.NhanVienID);
        if (route.Error is not null) ModelState.AddModelError(string.Empty, route.Error);
        if (!ModelState.IsValid)
        {
            await LoadEmployeesAsync();
            return View(model);
        }

        var allocation = await leave.CalculatePayAllocationAsync(
            model.NhanVienID, model.LoaiNghiPhepID!.Value, model.Ngaybatdau, model.NgayKetThuc);
        var storedFiles = new List<StoredFile>();
        try { foreach (var file in model.FilesMinhChung) storedFiles.Add(await files.SaveAsync(file, "leave-proofs")); }
        catch (InvalidOperationException exception)
        {
            foreach (var file in storedFiles) files.Delete(file.RelativePath);
            ModelState.AddModelError(nameof(model.FilesMinhChung), exception.Message);
            await LoadEmployeesAsync();
            return View(model);
        }
        var firstProof = storedFiles[0];

        var autoApproved = route.Route == LeaveApprovalRoute.AutoApprove;
        var item = new NghiPhep
        {
            NhanVienID = model.NhanVienID,
            LoaiNghiPhepID = model.LoaiNghiPhepID,
            LoaiNghi = type!.TenLoai,
            Ngaybatdau = model.Ngaybatdau.Date,
            NgayKetThuc = model.NgayKetThuc.Date,
            Lydo = model.Lydo.Trim(),
            LaDotXuat = model.LaDotXuat,
            Trangthai = autoApproved ? LeaveStatuses.Approved : LeaveStatuses.Pending,
            SoNgayHuongLuong = allocation.PaidDays,
            SoNgayKhongLuong = allocation.UnpaidDays,
            TenFileMinhChung = firstProof.OriginalName,
            DuongDanMinhChung = firstProof.RelativePath,
            ContentTypeMinhChung = firstProof.ContentType,
            MinhChungs = storedFiles.Select(x => new NghiPhepMinhChung
            {
                TenFileGoc = x.OriginalName, DuongDan = x.RelativePath,
                ContentType = x.ContentType, KichThuoc = x.Length
            }).ToList(),
            NguoiDuyet = autoApproved ? CurrentEmail() : null,
            NgayDuyet = autoApproved ? DateTime.UtcNow : null,
            NgayTao = DateTime.UtcNow
        };
        context.NghiPheps.Add(item);
        if (autoApproved) await leave.ApplyApprovedAttendanceAsync(item);
        try { await context.SaveChangesAsync(); }
        catch { foreach (var file in storedFiles) files.Delete(file.RelativePath); throw; }
        audit.Add(autoApproved ? "AutoApprove" : "Create", "NghiPhep", item.ID.ToString());
        await context.SaveChangesAsync();
        TempData["Success"] = autoApproved ? "Đơn nghỉ đã được tự động duyệt." :
            allocation.UnpaidDays > 0
                ? $"Đã tạo đơn: {allocation.PaidDays} ngày có lương, {allocation.UnpaidDays} ngày không lương."
                : "Đã tạo đơn nghỉ phép.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Decide(LeaveDecisionViewModel model, bool returnToRequests = false, string? returnTab = null)
    {
        IActionResult Back() => returnToRequests
            ? RedirectToAction("Index", "YeuCau", new { area = "HR", tab = returnTab })
            : RedirectToAction(nameof(Index));
        var item = await context.NghiPheps.Include(x => x.NhanVien).ThenInclude(x => x!.ChucVu)
            .FirstOrDefaultAsync(x => x.ID == model.ID);
        if (item is null) return NotFound();
        if (item.Trangthai is not (LeaveStatuses.Pending or ApprovalStatuses.ManagerApproved))
        {
            TempData["Error"] = "Đơn này đã được xử lý.";
            return Back();
        }
        if (!await CanCurrentUserDecideAsync(item)) return Forbid();
        if (model.Decision is not (LeaveStatuses.Approved or LeaveStatuses.Rejected)) return BadRequest();
        if (string.IsNullOrWhiteSpace(model.Response))
        {
            TempData["Error"] = "Cần nhập phản hồi trước khi hoàn thành duyệt đơn.";
            return Back();
        }

        var route = await leave.GetApprovalRouteAsync(item.NhanVienID);
        var isDepartmentManagerStage = item.Trangthai == LeaveStatuses.Pending &&
            route.Route == LeaveApprovalRoute.DepartmentManagerThenHr;
        if (model.Decision == LeaveStatuses.Rejected)
        {
            item.Trangthai = LeaveStatuses.Rejected;
            if (isDepartmentManagerStage)
            {
                item.PhanHoiQuanLy = model.Response?.Trim();
                item.NguoiDuyetQuanLy = CurrentEmail();
                item.NgayDuyetQuanLy = DateTime.UtcNow;
            }
            else
            {
                item.PhanHoi = model.Response?.Trim();
                item.NguoiDuyet = CurrentEmail();
                item.NgayDuyet = DateTime.UtcNow;
            }
            audit.Add("Reject", "NghiPhep", item.ID.ToString(), model.Response?.Trim());
            await context.SaveChangesAsync();
            TempData["Success"] = "Đã từ chối đơn nghỉ.";
            return Back();
        }

        var validationError = await leave.ValidateRequestAsync(
            item.NhanVienID, item.LoaiNghiPhepID, item.Ngaybatdau, item.NgayKetThuc, item.LaDotXuat, item.ID);
        if (validationError is not null)
        {
            TempData["Error"] = validationError;
            return Back();
        }

        if (isDepartmentManagerStage)
        {
            item.Trangthai = ApprovalStatuses.ManagerApproved;
            item.PhanHoiQuanLy = model.Response?.Trim();
            item.NguoiDuyetQuanLy = CurrentEmail();
            item.NgayDuyetQuanLy = DateTime.UtcNow;
            audit.Add("ManagerApprove", "NghiPhep", item.ID.ToString(), item.PhanHoiQuanLy);
            await context.SaveChangesAsync();
            TempData["Success"] = "Quản lý đã duyệt; đơn đang chờ HR xác nhận.";
            return Back();
        }

        var allocation = await leave.CalculatePayAllocationAsync(
            item.NhanVienID, item.LoaiNghiPhepID!.Value, item.Ngaybatdau, item.NgayKetThuc, item.ID);
        item.SoNgayHuongLuong = allocation.PaidDays;
        item.SoNgayKhongLuong = allocation.UnpaidDays;
        item.Trangthai = LeaveStatuses.Approved;
        item.PhanHoi = model.Response?.Trim();
        item.NguoiDuyet = CurrentEmail();
        item.NgayDuyet = DateTime.UtcNow;
        await leave.ApplyApprovedAttendanceAsync(item);
        audit.Add("Approve", "NghiPhep", item.ID.ToString(), item.PhanHoi);
        await context.SaveChangesAsync();
        TempData["Success"] = allocation.UnpaidDays > 0
            ? $"Đã duyệt: {allocation.PaidDays} ngày có lương, {allocation.UnpaidDays} ngày không lương."
            : "Đã duyệt đơn nghỉ.";
        return Back();
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        var item = await context.NghiPheps.FindAsync(id);
        if (item is null) return NotFound();
        if (item.Trangthai is not (LeaveStatuses.Pending or ApprovalStatuses.ManagerApproved))
        {
            TempData["Error"] = "Chỉ có thể hủy đơn chưa được duyệt hoàn tất.";
            return RedirectToAction(nameof(Index));
        }
        item.Trangthai = LeaveStatuses.Cancelled;
        audit.Add("Cancel", "NghiPhep", id.ToString());
        await context.SaveChangesAsync();
        TempData["Success"] = "Đã hủy đơn nghỉ.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> DownloadProof(int id)
    {
        var item = await context.NghiPheps.AsNoTracking().Include(x => x.NhanVien).ThenInclude(x => x!.ChucVu)
            .FirstOrDefaultAsync(x => x.ID == id);
        if (item?.DuongDanMinhChung is null) return NotFound();
        if (!await CanViewLeaveAsync(item)) return Forbid();
        try { return File(files.OpenRead(item.DuongDanMinhChung), item.ContentTypeMinhChung ?? "application/octet-stream", item.TenFileMinhChung ?? "minh-chung"); }
        catch (FileNotFoundException) { return NotFound(); }
    }

    [HttpGet]
    public async Task<IActionResult> DownloadEvidence(int id)
    {
        var proof = await context.NghiPhepMinhChungs.AsNoTracking()
            .Include(x => x.NghiPhep).ThenInclude(x => x!.NhanVien).ThenInclude(x => x!.ChucVu)
            .FirstOrDefaultAsync(x => x.ID == id);
        if (proof?.NghiPhep is null) return NotFound();
        if (!await CanViewLeaveAsync(proof.NghiPhep)) return Forbid();
        try { return File(files.OpenRead(proof.DuongDan), proof.ContentType, proof.TenFileGoc); }
        catch (FileNotFoundException) { return NotFound(); }
    }

    [HttpGet]
    public async Task<IActionResult> PreviewEvidence(int id)
    {
        var proof = await context.NghiPhepMinhChungs.AsNoTracking()
            .Include(x => x.NghiPhep).ThenInclude(x => x!.NhanVien).ThenInclude(x => x!.ChucVu)
            .FirstOrDefaultAsync(x => x.ID == id);
        if (proof?.NghiPhep is null) return NotFound();
        if (!await CanViewLeaveAsync(proof.NghiPhep)) return Forbid();
        var previewType = FileStorageService.InlinePreviewContentType(proof.TenFileGoc);
        if (previewType is null) return StatusCode(StatusCodes.Status415UnsupportedMediaType);
        try { return File(files.OpenRead(proof.DuongDan), previewType); }
        catch (FileNotFoundException) { return NotFound(); }
    }

    private async Task<bool> CanCurrentUserDecideAsync(NghiPhep item)
    {
        if (User.IsInRole(AuthorizationRoles.Administrator)) return true;
        // Bước sau quản lý phòng là bước HR duyệt cuối, không phụ thuộc NhanVienID của tài khoản HR.
        if (item.Trangthai == ApprovalStatuses.ManagerApproved)
            return User.IsInRole(AuthorizationRoles.HumanResources);
        var actorId = CurrentEmployeeId();
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

    private async Task<bool> CanViewLeaveAsync(NghiPhep item)
    {
        if (User.IsInRole(AuthorizationRoles.Administrator) || User.IsInRole(AuthorizationRoles.HumanResources)) return true;
        var actor = await CurrentEmployeeAsync();
        if (actor is null) return false;
        if (item.NhanVienID == actor.ID) return true;
        if (!User.IsInRole(AuthorizationRoles.Manager)) return false;
        var route = await leave.GetApprovalRouteAsync(actor.ID);
        if (route.HrManagerId == actor.ID)
        {
            var applicantRoles = AuthorizationRoles.Parse(item.NhanVien?.ChucVu?.PhanQuyen);
            return applicantRoles.Contains(AuthorizationRoles.Manager) || applicantRoles.Contains(AuthorizationRoles.HumanResources);
        }
        return item.NhanVien?.PhongBanID == actor.PhongBanID;
    }

    private async Task<bool> CanManageEmployeeAsync(int employeeId)
    {
        if (!IsManagerOnly()) return true;
        var actor = await CurrentEmployeeAsync();
        return actor is not null && await context.NhanViens.AsNoTracking()
            .AnyAsync(x => x.ID == employeeId && x.PhongBanID == actor.PhongBanID);
    }

    private async Task LoadEmployeesAsync()
    {
        var query = context.NhanViens.AsNoTracking().Where(x => x.Trangthai);
        if (IsManagerOnly())
        {
            var actor = await CurrentEmployeeAsync();
            query = actor is null ? query.Where(_ => false) : query.Where(x => x.PhongBanID == actor.PhongBanID);
        }
        ViewBag.NhanViens = await query.OrderBy(x => x.Ten).ToListAsync();
    }

    private bool IsManagerOnly() => User.IsInRole(AuthorizationRoles.Manager) &&
        !User.IsInRole(AuthorizationRoles.Administrator) && !User.IsInRole(AuthorizationRoles.HumanResources);
    private int? CurrentEmployeeId() => int.TryParse(User.FindFirstValue("NhanVienID"), out var id) ? id : null;
    private string? CurrentEmail() => User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
    private async Task<NhanVien?> CurrentEmployeeAsync()
    {
        var id = CurrentEmployeeId();
        return id.HasValue ? await context.NhanViens.AsNoTracking().FirstOrDefaultAsync(x => x.ID == id.Value) : null;
    }
}
