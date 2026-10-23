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
public class DanhMucController : Controller
{
    private readonly AppDbContext _context;
    private readonly AuditService _audit;
    public DanhMucController(AppDbContext context, AuditService audit) { _context = context; _audit = audit; }

    public IActionResult Index() => RedirectToAction("Index", "PhongBan");

    [HttpPost]
    public async Task<IActionResult> SaveDepartment(DepartmentViewModel model)
    {
        if (await _context.PhongBans.AnyAsync(p => p.Tenphongban == model.Tenphongban.Trim() && p.ID != model.ID))
            ModelState.AddModelError(nameof(model.Tenphongban), "Tên phòng ban đã tồn tại.");
        if (!ModelState.IsValid) { TempData["Error"] = "Dữ liệu phòng ban không hợp lệ hoặc bị trùng."; return RedirectToAction(nameof(Index)); }
        var item = model.ID == 0 ? new PhongBan() : await _context.PhongBans.FindAsync(model.ID);
        if (item is null) return NotFound();
        item.Tenphongban = model.Tenphongban.Trim();
        item.Matruongphong = model.Matruongphong;
        if (model.ID == 0) _context.PhongBans.Add(item);
        await _context.SaveChangesAsync();
        _audit.Add(model.ID == 0 ? "Create" : "Update", "PhongBan", item.ID.ToString());
        await _context.SaveChangesAsync();
        TempData["Success"] = "Đã lưu phòng ban.";
        return RedirectToAction("Index", "PhongBan");
    }

    [HttpPost]
    public async Task<IActionResult> SavePosition(PositionViewModel model)
    {
        if (await _context.ChucVus.AnyAsync(c => c.Chucvu == model.Chucvu.Trim() && c.ID != model.ID))
            ModelState.AddModelError(nameof(model.Chucvu), "Tên chức vụ đã tồn tại.");
        if (!ModelState.IsValid) { TempData["Error"] = "Dữ liệu chức vụ không hợp lệ hoặc bị trùng."; return RedirectToAction(nameof(Index)); }
        var item = model.ID == 0 ? new ChucVu() : await _context.ChucVus.FindAsync(model.ID);
        if (item is null) return NotFound();
        item.Chucvu = model.Chucvu.Trim();
        item.Hesoluong = model.Hesoluong;
        if (model.ID == 0) _context.ChucVus.Add(item);
        await _context.SaveChangesAsync();
        _audit.Add(model.ID == 0 ? "Create" : "Update", "ChucVu", item.ID.ToString());
        await _context.SaveChangesAsync();
        TempData["Success"] = "Đã lưu chức vụ.";
        return RedirectToAction("Index", "ChucVu");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteDepartment(int id)
    {
        var item = await _context.PhongBans.Include(p => p.NhanViens).FirstOrDefaultAsync(p => p.ID == id);
        if (item is null) return NotFound();
        if (item.NhanViens.Count != 0) { TempData["Error"] = "Không thể xóa phòng ban đang có nhân viên."; return RedirectToAction(nameof(Index)); }
        _context.PhongBans.Remove(item); _audit.Add("Delete", "PhongBan", id.ToString()); await _context.SaveChangesAsync();
        TempData["Success"] = "Đã xóa phòng ban."; return RedirectToAction("Index", "PhongBan");
    }

    [HttpPost]
    public async Task<IActionResult> DeletePosition(int id)
    {
        var item = await _context.ChucVus.Include(c => c.NhanViens).FirstOrDefaultAsync(c => c.ID == id);
        if (item is null) return NotFound();
        if (item.NhanViens.Count != 0) { TempData["Error"] = "Không thể xóa chức vụ đang được sử dụng."; return RedirectToAction(nameof(Index)); }
        _context.ChucVus.Remove(item); _audit.Add("Delete", "ChucVu", id.ToString()); await _context.SaveChangesAsync();
        TempData["Success"] = "Đã xóa chức vụ."; return RedirectToAction("Index", "ChucVu");
    }
}
