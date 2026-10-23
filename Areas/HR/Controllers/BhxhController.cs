using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.Data;
using QLNS.Models.Entities;
using QLNS.Models.ViewModels;
using QLNS.Services;

namespace QLNS.Areas.HR.Controllers;

[Area("HR")]
[Authorize(Roles = "Admin,HR")]
public sealed class BhxhController(AppDbContext context, AuditService audit) : Controller
{
    public async Task<IActionResult> Index(string? search = null, bool showInactive = false)
    {
        var query = context.ChinhSachBhxhs.AsNoTracking()
            .Include(x => x.PhongBan).Include(x => x.ChucVu).Include(x => x.NhanVien)
            .AsQueryable();
        if (!showInactive) query = query.Where(x => x.HoatDong);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.TenKhoan.Contains(term) ||
                (x.PhongBan != null && x.PhongBan.Tenphongban.Contains(term)) ||
                (x.ChucVu != null && x.ChucVu.Chucvu.Contains(term)) ||
                (x.NhanVien != null && (x.NhanVien.Ten.Contains(term) || x.NhanVien.MaNhanVien.Contains(term))));
        }
        ViewBag.Search = search;
        ViewBag.ShowInactive = showInactive;
        return View(await query.OrderByDescending(x => x.NgayTao).ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadTargetsAsync();
        return View(new ScopedSocialInsuranceFormViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(ScopedSocialInsuranceFormViewModel model)
    {
        await ValidateTargetAsync(model);
        if (!ModelState.IsValid) { await LoadTargetsAsync(); return View(model); }
        var item = new ChinhSachBhxh();
        Map(model, item);
        context.ChinhSachBhxhs.Add(item);
        await context.SaveChangesAsync();
        audit.Add("Create", "ChinhSachBHXH", item.ID.ToString(), item.TenKhoan);
        await context.SaveChangesAsync();
        TempData["Success"] = "Đã thêm chính sách BHXH.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await context.ChinhSachBhxhs.FindAsync(id);
        if (item is null) return NotFound();
        await LoadTargetsAsync();
        return View(ToForm(item));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, ScopedSocialInsuranceFormViewModel model)
    {
        if (id != model.ID) return BadRequest();
        var item = await context.ChinhSachBhxhs.FindAsync(id);
        if (item is null) return NotFound();
        await ValidateTargetAsync(model);
        if (!ModelState.IsValid) { await LoadTargetsAsync(); return View(model); }
        Map(model, item);
        item.NgayCapNhat = DateTime.UtcNow;
        audit.Add("Update", "ChinhSachBHXH", item.ID.ToString(), item.TenKhoan);
        await context.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật chính sách BHXH.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var item = await context.ChinhSachBhxhs.FindAsync(id);
        if (item is null) return NotFound();
        item.HoatDong = !item.HoatDong;
        item.NgayCapNhat = DateTime.UtcNow;
        audit.Add(item.HoatDong ? "Activate" : "Deactivate", "ChinhSachBHXH", id.ToString(), item.TenKhoan);
        await context.SaveChangesAsync();
        TempData["Success"] = item.HoatDong ? "Đã kích hoạt chính sách BHXH." : "Đã ngừng chính sách BHXH.";
        return RedirectToAction(nameof(Index), new { showInactive = true });
    }

    private async Task ValidateTargetAsync(ScopedSocialInsuranceFormViewModel model)
    {
        if (model.PhamVi == AllowanceScopes.Department && model.PhongBanID.HasValue &&
            !await context.PhongBans.AnyAsync(x => x.ID == model.PhongBanID.Value))
            ModelState.AddModelError(nameof(model.PhongBanID), "Phòng ban không tồn tại.");
        if (model.PhamVi == AllowanceScopes.Position && model.ChucVuID.HasValue &&
            !await context.ChucVus.AnyAsync(x => x.ID == model.ChucVuID.Value))
            ModelState.AddModelError(nameof(model.ChucVuID), "Chức vụ không tồn tại.");
        if (model.PhamVi == AllowanceScopes.Employee && model.NhanVienID.HasValue &&
            !await context.NhanViens.AnyAsync(x => x.ID == model.NhanVienID.Value && x.Trangthai))
            ModelState.AddModelError(nameof(model.NhanVienID), "Nhân viên không tồn tại hoặc đã ngừng làm việc.");
    }

    private async Task LoadTargetsAsync()
    {
        ViewBag.PhongBans = await context.PhongBans.AsNoTracking().OrderBy(x => x.Tenphongban).ToListAsync();
        ViewBag.ChucVus = await context.ChucVus.AsNoTracking().OrderBy(x => x.Chucvu).ToListAsync();
        ViewBag.NhanViens = await context.NhanViens.AsNoTracking().Where(x => x.Trangthai).OrderBy(x => x.Ten).ToListAsync();
    }

    private static void Map(ScopedSocialInsuranceFormViewModel source, ChinhSachBhxh target)
    {
        target.TenKhoan = source.TenKhoan.Trim();
        target.SoTienDong = source.SoTienDong;
        target.PhamVi = source.PhamVi;
        target.PhongBanID = source.PhamVi == AllowanceScopes.Department ? source.PhongBanID : null;
        target.ChucVuID = source.PhamVi == AllowanceScopes.Position ? source.ChucVuID : null;
        target.NhanVienID = source.PhamVi == AllowanceScopes.Employee ? source.NhanVienID : null;
        target.NgayBatDau = source.NgayBatDau.Date;
        target.KieuThoiHan = source.KieuThoiHan;
        target.SoThang = source.KieuThoiHan == AllowanceDurations.FixedMonths ? source.SoThang : null;
        target.HoatDong = source.HoatDong;
    }

    private static ScopedSocialInsuranceFormViewModel ToForm(ChinhSachBhxh item) => new()
    {
        ID = item.ID, TenKhoan = item.TenKhoan, SoTienDong = item.SoTienDong, PhamVi = item.PhamVi,
        PhongBanID = item.PhongBanID, ChucVuID = item.ChucVuID, NhanVienID = item.NhanVienID,
        NgayBatDau = item.NgayBatDau, KieuThoiHan = item.KieuThoiHan, SoThang = item.SoThang,
        HoatDong = item.HoatDong
    };
}
