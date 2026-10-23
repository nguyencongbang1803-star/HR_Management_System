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
public sealed class PhuCapController(AppDbContext context, AuditService audit) : Controller
{
    public async Task<IActionResult> Index(string? search = null, bool showInactive = false)
    {
        var query = context.ChinhSachPhuCaps.AsNoTracking()
            .Include(x => x.PhongBan).Include(x => x.ChucVu).Include(x => x.NhanVien)
            .AsQueryable();
        if (!showInactive) query = query.Where(x => x.HoatDong);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.TenPhuCap.Contains(term) ||
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
        return View(new ScopedAllowanceFormViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(ScopedAllowanceFormViewModel model)
    {
        await ValidateTargetAsync(model);
        if (!ModelState.IsValid)
        {
            await LoadTargetsAsync();
            return View(model);
        }
        var item = new ChinhSachPhuCap();
        Map(model, item);
        context.ChinhSachPhuCaps.Add(item);
        await context.SaveChangesAsync();
        audit.Add("Create", "ChinhSachPhuCap", item.ID.ToString(), item.TenPhuCap);
        await context.SaveChangesAsync();
        TempData["Success"] = "Đã thêm chính sách phụ cấp.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await context.ChinhSachPhuCaps.FindAsync(id);
        if (item is null) return NotFound();
        await LoadTargetsAsync();
        return View(ToForm(item));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, ScopedAllowanceFormViewModel model)
    {
        if (id != model.ID) return BadRequest();
        var item = await context.ChinhSachPhuCaps.FindAsync(id);
        if (item is null) return NotFound();
        await ValidateTargetAsync(model);
        if (!ModelState.IsValid)
        {
            await LoadTargetsAsync();
            return View(model);
        }
        Map(model, item);
        item.NgayCapNhat = DateTime.UtcNow;
        audit.Add("Update", "ChinhSachPhuCap", item.ID.ToString(), item.TenPhuCap);
        await context.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật chính sách phụ cấp.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var item = await context.ChinhSachPhuCaps.FindAsync(id);
        if (item is null) return NotFound();
        item.HoatDong = !item.HoatDong;
        item.NgayCapNhat = DateTime.UtcNow;
        audit.Add(item.HoatDong ? "Activate" : "Deactivate", "ChinhSachPhuCap", id.ToString(), item.TenPhuCap);
        await context.SaveChangesAsync();
        TempData["Success"] = item.HoatDong ? "Đã kích hoạt phụ cấp." : "Đã ngừng phụ cấp.";
        return RedirectToAction(nameof(Index), new { showInactive = true });
    }

    private async Task ValidateTargetAsync(ScopedAllowanceFormViewModel model)
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
        ViewBag.NhanViens = await context.NhanViens.AsNoTracking().Where(x => x.Trangthai)
            .OrderBy(x => x.Ten).ToListAsync();
    }

    private static void Map(ScopedAllowanceFormViewModel source, ChinhSachPhuCap target)
    {
        target.TenPhuCap = source.TenPhuCap.Trim();
        target.SoTien = source.SoTien;
        target.PhamVi = source.PhamVi;
        target.PhongBanID = source.PhamVi == AllowanceScopes.Department ? source.PhongBanID : null;
        target.ChucVuID = source.PhamVi == AllowanceScopes.Position ? source.ChucVuID : null;
        target.NhanVienID = source.PhamVi == AllowanceScopes.Employee ? source.NhanVienID : null;
        target.NgayBatDau = source.NgayBatDau.Date;
        target.KieuThoiHan = source.KieuThoiHan;
        target.SoThang = source.KieuThoiHan == AllowanceDurations.FixedMonths ? source.SoThang : null;
        target.HoatDong = source.HoatDong;
    }

    private static ScopedAllowanceFormViewModel ToForm(ChinhSachPhuCap item) => new()
    {
        ID = item.ID, TenPhuCap = item.TenPhuCap, SoTien = item.SoTien, PhamVi = item.PhamVi,
        PhongBanID = item.PhongBanID, ChucVuID = item.ChucVuID, NhanVienID = item.NhanVienID,
        NgayBatDau = item.NgayBatDau, KieuThoiHan = item.KieuThoiHan, SoThang = item.SoThang,
        HoatDong = item.HoatDong
    };
}
