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
[Authorize(Roles = "Admin,HR")]
public class ChucVuController(
    AppDbContext context,
    AuditService audit,
    UserManager<TaiKhoan> userManager) : Controller
{
    public async Task<IActionResult> Index(
        string? search,
        decimal? minCoefficient,
        decimal? maxCoefficient,
        string sort = "name",
        bool descending = false)
    {
        var query = context.ChucVus.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(x => x.Chucvu.Contains(keyword));
        }

        if (minCoefficient.HasValue)
            query = query.Where(x => x.Hesoluong >= minCoefficient);
        if (maxCoefficient.HasValue)
            query = query.Where(x => x.Hesoluong <= maxCoefficient);

        query = (sort, descending) switch
        {
            ("coefficient", false) => query.OrderBy(x => x.Hesoluong),
            ("coefficient", true) => query.OrderByDescending(x => x.Hesoluong),
            ("employees", false) => query.OrderBy(x => x.NhanViens.Count(n => n.Trangthai)),
            ("employees", true) => query.OrderByDescending(x => x.NhanViens.Count(n => n.Trangthai)),
            ("name", true) => query.OrderByDescending(x => x.Chucvu),
            _ => query.OrderBy(x => x.Chucvu)
        };

        var positions = await query
            .Select(x => new
            {
                Position = x,
                EmployeeCount = x.NhanViens.Count(n => n.Trangthai),
                Employees = x.NhanViens
                    .Where(n => n.Trangthai)
                    .OrderBy(n => n.Ten)
                    .Select(n => new
                    {
                        n.ID,
                        n.MaNhanVien,
                        n.Ten,
                        n.Email,
                        n.SDT,
                        Department = n.PhongBan == null ? null : n.PhongBan.Tenphongban
                    })
                    .ToList()
            })
            .ToListAsync();

        return View(new PositionIndexViewModel
        {
            Items = positions.Select(x => new PositionListItemViewModel
            {
                Position = x.Position,
                EmployeeCount = x.EmployeeCount,
                Employees = x.Employees.Select(employee => new NhanVien
                {
                    ID = employee.ID,
                    MaNhanVien = employee.MaNhanVien,
                    Ten = employee.Ten,
                    Email = employee.Email,
                    SDT = employee.SDT,
                    PhongBan = employee.Department == null
                        ? null
                        : new PhongBan { Tenphongban = employee.Department }
                }).ToList()
            }).ToList(),
            Search = search,
            MinCoefficient = minCoefficient,
            MaxCoefficient = maxCoefficient,
            Sort = sort,
            Descending = descending
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(PositionViewModel model)
    {
        var name = model.Chucvu?.Trim() ?? string.Empty;
        var selectedRoles = (model.PhanQuyens ?? [])
            .Where(AuthorizationRoles.IsValid)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (selectedRoles.Count == 0)
            ModelState.AddModelError(nameof(model.PhanQuyens), "Vui lòng chọn ít nhất một nhóm quyền.");
        if (!User.IsInRole(AuthorizationRoles.Administrator) &&
            selectedRoles.Contains(AuthorizationRoles.Administrator))
            ModelState.AddModelError(nameof(model.PhanQuyens), "Chỉ quản trị viên được gán nhóm quyền Admin.");
        if (await context.ChucVus.AnyAsync(x => x.Chucvu == name && x.ID != model.ID))
            ModelState.AddModelError(nameof(model.Chucvu), "Tên chức vụ đã tồn tại.");

        if (!ModelState.IsValid)
            return Error("Không thể lưu chức vụ. Vui lòng kiểm tra dữ liệu hoặc tên bị trùng.");

        var isNew = model.ID == 0;
        var position = isNew ? new ChucVu() : await context.ChucVus.FindAsync(model.ID);
        if (position is null) return NotFound();
        if (!User.IsInRole(AuthorizationRoles.Administrator) &&
            !isNew && AuthorizationRoles.Contains(position.PhanQuyen, AuthorizationRoles.Administrator))
            return Error("Chỉ quản trị viên được cập nhật chức vụ có nhóm quyền Admin.");

        position.Chucvu = name;
        var requestedRoles = AuthorizationRoles.Serialize(selectedRoles);
        position.PhanQuyen = requestedRoles;
        if (isNew || model.Hesoluong.HasValue)
            position.Hesoluong = model.Hesoluong ?? 1m;
        position.SoNgayPhepNam = model.SoNgayPhepNam;
        if (isNew) context.ChucVus.Add(position);

        await context.SaveChangesAsync();
        await context.SoDuNghiPheps
            .Where(x => x.Nam == DateTime.Today.Year &&
                x.LoaiNghiPhep != null && x.LoaiNghiPhep.TruQuyPhep &&
                x.NhanVien != null && x.NhanVien.ChucVuID == position.ID)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.SoNgayDuocHuong, position.SoNgayPhepNam));
        await SynchronizeAccountsAsync(position.ID, AuthorizationRoles.Parse(position.PhanQuyen));
        audit.Add(isNew ? "Create" : "Update", "ChucVu", position.ID.ToString(), position.Chucvu);
        await context.SaveChangesAsync();
        TempData["Success"] = isNew ? "Đã tạo chức vụ mới." : "Đã cập nhật chức vụ.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var position = await context.ChucVus
            .Include(x => x.NhanViens)
            .FirstOrDefaultAsync(x => x.ID == id);
        if (position is null) return NotFound();
        if (position.NhanViens.Count != 0)
            return Error("Không thể xóa chức vụ đang được sử dụng.");

        context.ChucVus.Remove(position);
        audit.Add("Delete", "ChucVu", id.ToString(), position.Chucvu);
        await context.SaveChangesAsync();
        TempData["Success"] = "Đã xóa chức vụ.";
        return RedirectToAction(nameof(Index));
    }

    private RedirectToActionResult Error(string message)
    {
        TempData["Error"] = message;
        return RedirectToAction(nameof(Index));
    }

    private async Task SynchronizeAccountsAsync(int positionId, IReadOnlyList<string> roles)
    {
        var accounts = await userManager.Users
            .Where(x => x.NhanVien != null && x.NhanVien.ChucVuID == positionId)
            .ToListAsync();
        foreach (var account in accounts)
        {
            var currentRoles = await userManager.GetRolesAsync(account);
            var desired=roles.ToHashSet(StringComparer.Ordinal);
            var toRemove=currentRoles.Where(x=>!desired.Contains(x)).ToList();if(toRemove.Count>0)await userManager.RemoveFromRolesAsync(account,toRemove);
            var toAdd=desired.Where(x=>!currentRoles.Contains(x)).ToList();if(toAdd.Count>0)await userManager.AddToRolesAsync(account,toAdd);
            var primary=AuthorizationRoles.All.Reverse().First(desired.Contains);
            if (account.Role != primary)
            {
                account.Role = primary;
                await userManager.UpdateAsync(account);
            }
        }
    }
}
