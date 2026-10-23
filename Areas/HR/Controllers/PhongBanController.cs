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
public class PhongBanController(AppDbContext context, AuditService audit) : Controller
{
    public async Task<IActionResult> Index(
        string? search,
        string? managerSearch,
        string sort = "name",
        bool descending = false)
    {
        var query = context.PhongBans.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(x => x.Tenphongban.Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(managerSearch))
        {
            var managerKeyword = managerSearch.Trim();
            query = query.Where(x => context.NhanViens.Any(n =>
                n.ID == x.Matruongphong && n.Ten.Contains(managerKeyword)));
        }

        query = (sort, descending) switch
        {
            ("employees", false) => query.OrderBy(x =>
                x.NhanViens.Count(n => n.Trangthai)),
            ("employees", true) => query.OrderByDescending(x =>
                x.NhanViens.Count(n => n.Trangthai)),
            ("manager", false) => query.OrderBy(x => context.NhanViens
                .Where(n => n.ID == x.Matruongphong).Select(n => n.Ten).FirstOrDefault()),
            ("manager", true) => query.OrderByDescending(x => context.NhanViens
                .Where(n => n.ID == x.Matruongphong).Select(n => n.Ten).FirstOrDefault()),
            ("name", true) => query.OrderByDescending(x => x.Tenphongban),
            _ => query.OrderBy(x => x.Tenphongban)
        };

        var departments = await query
            .Select(x => new
            {
                Department = x,
                EmployeeCount = x.NhanViens.Count(n => n.Trangthai),
                ManagerName = context.NhanViens
                    .Where(n => n.ID == x.Matruongphong && n.PhongBanID == x.ID && n.Trangthai)
                    .Select(n => n.Ten)
                    .FirstOrDefault(),
                Employees = x.NhanViens
                    .Where(n => n.Trangthai)
                    .OrderBy(n => n.Ten)
                    .Select(n => new { n.ID, n.MaNhanVien, n.Ten, n.Email, n.SDT, ChucVu = n.ChucVu == null ? null : n.ChucVu.Chucvu })
                    .ToList(),
                Manager = context.NhanViens
                    .Where(n => n.ID == x.Matruongphong && n.PhongBanID == x.ID && n.Trangthai)
                    .Select(n => new { n.ID, n.MaNhanVien, n.Ten, n.Email, n.SDT, ChucVu = n.ChucVu == null ? null : n.ChucVu.Chucvu })
                    .FirstOrDefault()
            })
            .ToListAsync();

        var items = departments.Select(x =>
            {
                var activeEmployees = x.Employees.Select(e => new NhanVien
                {
                    ID = e.ID,
                    MaNhanVien = e.MaNhanVien,
                    Ten = e.Ten,
                    Email = e.Email,
                    SDT = e.SDT,
                    ChucVu = e.ChucVu == null ? null : new ChucVu { Chucvu = e.ChucVu }
                }).ToList();

                return new DepartmentListItemViewModel
                {
                    Department = x.Department,
                    EmployeeCount = activeEmployees.Count,
                    ManagerName = x.ManagerName,
                    Employees = activeEmployees.OrderBy(e => e.Ten).ToList()
                };
            }).ToList();

        await LoadFormDataAsync();
        return View(new DepartmentIndexViewModel
        {
            Items = items,
            Roots = BuildTree(items),
            Search = search,
            ManagerSearch = managerSearch,
            Sort = sort,
            Descending = descending
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(DepartmentViewModel model)
    {
        var isNew = model.ID == 0;
        if (isNew)
            model.Matruongphong = null;

        var name = model.Tenphongban?.Trim() ?? string.Empty;
        if (await context.PhongBans.AnyAsync(x => x.Tenphongban == name && x.ID != model.ID))
            ModelState.AddModelError(nameof(model.Tenphongban), "Tên phòng ban đã tồn tại.");
        if (model.Matruongphong.HasValue &&
            !await context.NhanViens.AnyAsync(x => x.ID == model.Matruongphong &&
                x.Trangthai && x.PhongBanID == model.ID))
            ModelState.AddModelError(nameof(model.Matruongphong), "Trưởng phòng phải là nhân viên đang làm việc thuộc đúng phòng ban này.");
        if (model.Matruongphong.HasValue &&
            await context.PhongBans.AnyAsync(x =>
                x.Matruongphong == model.Matruongphong && x.ID != model.ID))
            return Error("Nhân viên này đang là trưởng phòng của phòng ban khác.");
        if (model.PhongBanChaID.HasValue &&
            !await context.PhongBans.AnyAsync(x => x.ID == model.PhongBanChaID))
            ModelState.AddModelError(nameof(model.PhongBanChaID), "Phòng ban cấp trên không tồn tại.");
        if (model.ID != 0 && model.PhongBanChaID == model.ID)
            ModelState.AddModelError(nameof(model.PhongBanChaID), "Phòng ban không thể trực thuộc chính nó.");
        if (model.ID != 0 && model.PhongBanChaID.HasValue &&
            await CreatesHierarchyCycleAsync(model.ID, model.PhongBanChaID.Value))
            ModelState.AddModelError(nameof(model.PhongBanChaID), "Không thể chọn phòng ban cấp dưới làm cấp trên.");

        if (!ModelState.IsValid)
            return Error("Không thể lưu phòng ban. Vui lòng kiểm tra dữ liệu hoặc tên bị trùng.");

        var department = isNew ? new PhongBan() : await context.PhongBans.FindAsync(model.ID);
        if (department is null) return NotFound();

        department.Tenphongban = name;
        department.Matruongphong = model.Matruongphong;
        department.PhongBanChaID = model.PhongBanChaID;
        if (isNew) context.PhongBans.Add(department);

        await context.SaveChangesAsync();

        audit.Add(isNew ? "Create" : "Update", "PhongBan", department.ID.ToString(), department.Tenphongban);
        await context.SaveChangesAsync();
        TempData["Success"] = isNew ? "Đã tạo phòng ban mới." : "Đã cập nhật phòng ban.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var department = await context.PhongBans
            .Include(x => x.NhanViens)
            .FirstOrDefaultAsync(x => x.ID == id);
        if (department is null) return NotFound();
        if (department.NhanViens.Count != 0)
            return Error("Không thể xóa phòng ban đang có nhân viên.");
        if (await context.PhongBans.AnyAsync(x => x.PhongBanChaID == id))
            return Error("Không thể xóa phòng ban đang có đơn vị trực thuộc.");

        context.PhongBans.Remove(department);
        audit.Add("Delete", "PhongBan", id.ToString(), department.Tenphongban);
        await context.SaveChangesAsync();
        TempData["Success"] = "Đã xóa phòng ban.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadFormDataAsync()
    {
        var managerAssignments = await context.PhongBans.AsNoTracking()
            .Where(x => x.Matruongphong.HasValue)
            .Select(x => new { EmployeeId = x.Matruongphong!.Value, DepartmentId = x.ID })
            .ToListAsync();
        ViewBag.ManagerDepartmentIds = managerAssignments
            .GroupBy(x => x.EmployeeId)
            .ToDictionary(group => group.Key, group => group.First().DepartmentId);
        ViewBag.AllDepartments = await context.PhongBans.AsNoTracking()
            .OrderBy(x => x.Tenphongban)
            .ToListAsync();
    }

    private async Task<bool> CreatesHierarchyCycleAsync(int departmentId, int parentId)
    {
        var parentById = await context.PhongBans.AsNoTracking()
            .ToDictionaryAsync(x => x.ID, x => x.PhongBanChaID);
        var visited = new HashSet<int>();
        int? current = parentId;
        while (current.HasValue && visited.Add(current.Value))
        {
            if (current.Value == departmentId) return true;
            current = parentById.GetValueOrDefault(current.Value);
        }
        return false;
    }

    private static IReadOnlyList<DepartmentTreeNodeViewModel> BuildTree(
        IReadOnlyList<DepartmentListItemViewModel> items)
    {
        var nodes = items.ToDictionary(
            x => x.Department.ID,
            x => new DepartmentTreeNodeViewModel { Item = x });
        var roots = new List<DepartmentTreeNodeViewModel>();
        foreach (var item in items)
        {
            var node = nodes[item.Department.ID];
            if (item.Department.PhongBanChaID.HasValue &&
                nodes.TryGetValue(item.Department.PhongBanChaID.Value, out var parent))
                parent.Children.Add(node);
            else
                roots.Add(node);
        }
        return roots;
    }

    private RedirectToActionResult Error(string message)
    {
        TempData["Error"] = message;
        return RedirectToAction(nameof(Index));
    }
}
