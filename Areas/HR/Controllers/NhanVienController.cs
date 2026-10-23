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
public class NhanVienController : Controller
{
    private const int PageSize = 20;
    private readonly AppDbContext _context;
    private readonly AuditService _audit;
    private readonly FileStorageService _files;

    public NhanVienController(AppDbContext context, AuditService audit, FileStorageService files)
    {
        _context = context;
        _audit = audit;
        _files = files;
    }

    public async Task<IActionResult> Index(string? search, int? departmentId, int? positionId, bool showInactive = false,
        bool withoutContract = false, int? contractMonth = null, int? contractYear = null, int page = 1)
    {
        page = Math.Max(1, page);
        var query = _context.NhanViens.AsNoTracking()
            .Include(n => n.PhongBan).Include(n => n.ChucVu).AsQueryable();

        if (withoutContract)
        {
            var month = contractMonth is >= 1 and <= 12 ? contractMonth.Value : DateTime.Today.Month;
            var year = contractYear is >= 2000 and <= 2200 ? contractYear.Value : DateTime.Today.Year;
            var from = new DateTime(year, month, 1);
            var to = from.AddMonths(1).AddTicks(-1);
            contractMonth = month;
            contractYear = year;
            query = query.Where(n => n.Ngayvaolam <= to && (!n.Ngayxoa.HasValue || n.Ngayxoa >= from) &&
                !n.HopDongs.Any(h => h.Ngaybatdau <= to && (!h.Ngayketthuc.HasValue || h.Ngayketthuc >= from)));
        }
        else if (!showInactive) query = query.Where(n => n.Trangthai);
        if (IsManagerOnly())
        {
            var managerDepartmentId = await ManagerDepartmentIdAsync();
            if (!managerDepartmentId.HasValue) return Forbid();
            departmentId = managerDepartmentId;
            query = query.Where(n => n.PhongBanID == managerDepartmentId.Value);
        }
        else if (departmentId.HasValue) query = query.Where(n => n.PhongBanID == departmentId);
        if (positionId.HasValue) query = query.Where(n => n.ChucVuID == positionId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(n => n.Ten.Contains(keyword) || n.MaNhanVien.Contains(keyword) ||
                                     (n.Email != null && n.Email.Contains(keyword)));
        }

        var count = await query.CountAsync();
        var model = new EmployeeIndexViewModel
        {
            Items = await query.OrderBy(n => n.Ten).Skip((page - 1) * PageSize).Take(PageSize).ToListAsync(),
            Search = search,
            DepartmentId = departmentId,
            PositionId = positionId,
            ShowInactive = showInactive,
            WithoutContract = withoutContract,
            ContractMonth = contractMonth,
            ContractYear = contractYear,
            Page = page,
            TotalPages = Math.Max(1, (int)Math.Ceiling(count / (double)PageSize))
        };
        await LoadDropdownsAsync();
        return View(model);
    }

    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> Create()
    {
        await LoadDropdownsAsync();
        return View(new EmployeeFormViewModel());
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPost]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> Create(EmployeeFormViewModel model)
    {
        await ValidateUniqueAsync(model);
        await ValidatePositionAssignmentAsync(model.ChucVuID);
        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return View(model);
        }

        var employee = new NhanVien();
        Map(model, employee);
        if (model.Avatar is not null)
        {
            try
            {
                employee.AnhDaiDien = (await _files.SaveAvatarAsync(model.Avatar)).RelativePath;
            }
            catch (InvalidOperationException exception)
            {
                ModelState.AddModelError(nameof(model.Avatar), exception.Message);
                await LoadDropdownsAsync();
                return View(model);
            }
        }
        employee.Ngaytao = employee.Ngaycapnhat = DateTime.UtcNow;
        _context.NhanViens.Add(employee);
        await _context.SaveChangesAsync();
        var positionQuota=await _context.ChucVus.Where(x=>x.ID==employee.ChucVuID).Select(x=>(decimal?)x.SoNgayPhepNam).FirstOrDefaultAsync()??16m;
        var quotaTypes=await _context.LoaiNghiPheps.Where(x=>x.TruQuyPhep&&x.HoatDong).ToListAsync();
        _context.SoDuNghiPheps.AddRange(quotaTypes.Select(type=>new SoDuNghiPhep
        {
            NhanVienID=employee.ID,LoaiNghiPhepID=type.ID,Nam=DateTime.Today.Year,SoNgayDuocHuong=positionQuota
        }));
        _audit.Add("Create", "NhanVien", employee.ID.ToString(), $"Tạo nhân viên {employee.MaNhanVien}");
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm nhân viên {employee.MaNhanVien} - {employee.Ten} thành công.";
        TempData["ToastActionText"] = "Thêm hợp đồng";
        TempData["ToastActionUrl"] = Url.Action("Create", "HopDong", new { area = "HR", nhanVienId = employee.ID });
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> Edit(int id)
    {
        var employee = await _context.NhanViens.FindAsync(id);
        if (employee is null) return NotFound();
        await LoadDropdownsAsync();
        return View(ToForm(employee));
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPost]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> Edit(int id, EmployeeFormViewModel model)
    {
        if (id != model.ID) return BadRequest();
        var employee = await _context.NhanViens.FindAsync(id);
        if (employee is null) return NotFound();
        await ValidateUniqueAsync(model, id);
        await ValidatePositionAssignmentAsync(model.ChucVuID, id);
        if (!ModelState.IsValid)
        {
            model.CurrentAvatar = employee.AnhDaiDien;
            await LoadDropdownsAsync();
            return View(model);
        }

        var previousAvatar = employee.AnhDaiDien;
        if (model.Avatar is not null)
        {
            try
            {
                employee.AnhDaiDien = (await _files.SaveAvatarAsync(model.Avatar)).RelativePath;
            }
            catch (InvalidOperationException exception)
            {
                ModelState.AddModelError(nameof(model.Avatar), exception.Message);
                model.CurrentAvatar = previousAvatar;
                await LoadDropdownsAsync();
                return View(model);
            }
        }
        else if (model.RemoveAvatar)
        {
            employee.AnhDaiDien = null;
        }

        Map(model, employee);
        employee.Ngaycapnhat = DateTime.UtcNow;
        _audit.Add("Update", "NhanVien", id.ToString(), $"Cập nhật nhân viên {employee.MaNhanVien}");
        await _context.SaveChangesAsync();
        var updatedQuota=await _context.ChucVus.Where(x=>x.ID==employee.ChucVuID).Select(x=>(decimal?)x.SoNgayPhepNam).FirstOrDefaultAsync()??16m;
        await _context.SoDuNghiPheps.Where(x=>x.NhanVienID==employee.ID&&x.Nam==DateTime.Today.Year&&x.LoaiNghiPhep!=null&&x.LoaiNghiPhep.TruQuyPhep)
            .ExecuteUpdateAsync(update=>update.SetProperty(x=>x.SoNgayDuocHuong,updatedQuota));
        if (previousAvatar != employee.AnhDaiDien)
            _files.Delete(previousAvatar);
        TempData["Success"] = "Đã cập nhật nhân viên.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    public async Task<IActionResult> Detail(int id)
    {
        var query = _context.NhanViens.AsNoTracking()
            .Include(n => n.PhongBan).Include(n => n.ChucVu)
            .Include(n => n.HopDongs.OrderByDescending(h => h.Ngaybatdau)).ThenInclude(h => h.TaiLieuHopDongs)
            .Include(n => n.NghiPheps.OrderByDescending(p => p.Ngaybatdau).Take(10))
            .Include(n => n.TrinhDoChungChis.OrderByDescending(x => x.NgayCap))
            .Include(n => n.TaiLieuNhanViens.OrderByDescending(x => x.NgayTaiLen))
            .Include(n => n.SoDuNghiPheps).ThenInclude(x => x.LoaiNghiPhep).AsQueryable();
        if (IsManagerOnly())
        {
            var managerDepartmentId = await ManagerDepartmentIdAsync();
            if (!managerDepartmentId.HasValue) return Forbid();
            query = query.Where(x => x.PhongBanID == managerDepartmentId.Value);
        }
        var employee = await query.FirstOrDefaultAsync(n => n.ID == id);
        return employee is null ? NotFound() : View(employee);
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPost]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var employee = await _context.NhanViens.FindAsync(id);
        if (employee is null) return NotFound();
        employee.Trangthai = !employee.Trangthai;
        employee.Ngayxoa = employee.Trangthai ? null : DateTime.UtcNow;
        employee.Ngaycapnhat = DateTime.UtcNow;
        _audit.Add(employee.Trangthai ? "Reactivate" : "Deactivate", "NhanVien", id.ToString());
        await _context.SaveChangesAsync();
        TempData["Success"] = employee.Trangthai ? "Đã kích hoạt lại nhân viên." : "Đã ngừng hoạt động nhân viên.";
        return RedirectToAction(nameof(Index), new { showInactive = true });
    }

    private async Task ValidateUniqueAsync(EmployeeFormViewModel model, int? exceptId = null)
    {
        var code = model.MaNhanVien.Trim();
        if (await _context.NhanViens.AnyAsync(n => n.MaNhanVien == code && n.ID != exceptId))
            ModelState.AddModelError(nameof(model.MaNhanVien), "Mã nhân viên đã tồn tại.");
        if (!string.IsNullOrWhiteSpace(model.Email) &&
            await _context.NhanViens.AnyAsync(n => n.Email == model.Email.Trim() && n.ID != exceptId))
            ModelState.AddModelError(nameof(model.Email), "Email đã tồn tại.");
        if (!string.IsNullOrWhiteSpace(model.CanCuocCongDan) &&
            await _context.NhanViens.AnyAsync(n => n.CanCuocCongDan == model.CanCuocCongDan.Trim() && n.ID != exceptId))
            ModelState.AddModelError(nameof(model.CanCuocCongDan), "CCCD/Hộ chiếu đã tồn tại.");
    }

    private async Task LoadDropdownsAsync()
    {
        var departments = _context.PhongBans.AsNoTracking().AsQueryable();
        if (IsManagerOnly())
        {
            var managerDepartmentId = await ManagerDepartmentIdAsync();
            departments = managerDepartmentId.HasValue
                ? departments.Where(x => x.ID == managerDepartmentId.Value)
                : departments.Where(_ => false);
        }
        ViewBag.PhongBans = await departments.OrderBy(p => p.Tenphongban).ToListAsync();
        ViewBag.ChucVus = await _context.ChucVus.AsNoTracking().OrderBy(c => c.Chucvu).ToListAsync();
    }

    private bool IsManagerOnly() => User.IsInRole(AuthorizationRoles.Manager) &&
        !User.IsInRole(AuthorizationRoles.Administrator) && !User.IsInRole(AuthorizationRoles.HumanResources);

    private async Task<int?> ManagerDepartmentIdAsync()
    {
        if (!int.TryParse(User.FindFirstValue("NhanVienID"), out var employeeId)) return null;
        return await _context.NhanViens.AsNoTracking().Where(x => x.ID == employeeId)
            .Select(x => x.PhongBanID).SingleOrDefaultAsync();
    }

    private async Task ValidatePositionAssignmentAsync(int? positionId, int? employeeId = null)
    {
        if (User.IsInRole(AuthorizationRoles.Administrator)) return;

        if (employeeId.HasValue)
        {
            var currentRole = await _context.NhanViens.AsNoTracking()
                .Where(x => x.ID == employeeId.Value)
                .Select(x => x.ChucVu == null ? null : x.ChucVu.PhanQuyen)
                .SingleOrDefaultAsync();
            if (AuthorizationRoles.Contains(currentRole,AuthorizationRoles.Administrator))
                ModelState.AddModelError(string.Empty,
                    "Chỉ quản trị viên được cập nhật nhân viên thuộc chức vụ quản trị.");
        }

        if (positionId.HasValue && await _context.ChucVus.AsNoTracking()
            .AnyAsync(x => x.ID == positionId.Value &&
                           (","+x.PhanQuyen+",").Contains(",Admin,")))
            ModelState.AddModelError(nameof(EmployeeFormViewModel.ChucVuID),
                "Chỉ quản trị viên được gán chức vụ có nhóm quyền Admin.");
    }

    private static void Map(EmployeeFormViewModel source, NhanVien target)
    {
        target.MaNhanVien = source.MaNhanVien.Trim().ToUpperInvariant();
        target.Ten = source.Ten.Trim();
        target.Email = string.IsNullOrWhiteSpace(source.Email) ? null : source.Email.Trim().ToLowerInvariant();
        target.SDT = source.SDT?.Trim();
        target.Gioitinh = source.Gioitinh;
        target.NgaySinh = source.NgaySinh?.Date;
        target.CanCuocCongDan = source.CanCuocCongDan?.Trim();
        target.DiaChi = source.DiaChi?.Trim();
        target.MaSothue = source.MaSothue?.Trim();
        target.SoTaiKhoan = source.SoTaiKhoan?.Trim();
        target.Ngayvaolam = source.Ngayvaolam.Date;
        target.PhongBanID = source.PhongBanID;
        target.ChucVuID = source.ChucVuID;
        target.Trangthai = source.Trangthai;
    }

    private static EmployeeFormViewModel ToForm(NhanVien n) => new()
    {
        ID = n.ID, MaNhanVien = n.MaNhanVien, Ten = n.Ten, Email = n.Email, SDT = n.SDT,
        Gioitinh = n.Gioitinh, NgaySinh = n.NgaySinh, CanCuocCongDan = n.CanCuocCongDan,
        DiaChi = n.DiaChi, MaSothue = n.MaSothue, SoTaiKhoan = n.SoTaiKhoan,
        Ngayvaolam = n.Ngayvaolam, PhongBanID = n.PhongBanID, ChucVuID = n.ChucVuID,
        Trangthai = n.Trangthai, CurrentAvatar = n.AnhDaiDien
    };
}
