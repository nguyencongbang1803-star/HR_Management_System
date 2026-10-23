using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.Data;
using QLNS.Models.Entities;
using QLNS.Models.ViewModels;
using QLNS.Services;

namespace QLNS.Areas.HR.Controllers;
[Area("HR")][Authorize(Roles="Admin,HR")]
public class CheDoController:Controller
{
    private readonly AppDbContext _context;private readonly AuditService _audit;public CheDoController(AppDbContext context,AuditService audit){_context=context;_audit=audit;}
    public async Task<IActionResult> Index(string? employeeSearch = null, int? employeeId = null)
    {
        ViewBag.EmployeeSearch = employeeSearch;
        await LoadAsync();
        var adjustmentQuery = _context.ThuongPhats.AsNoTracking().Include(p => p.NhanVien)
            .Where(p => p.Loai != "ThueTNCN");

        if (employeeId.HasValue)
        {
            adjustmentQuery = adjustmentQuery.Where(p => p.NhanVienID == employeeId);
        }
        else if (!string.IsNullOrWhiteSpace(employeeSearch))
        {
            var term = employeeSearch.Trim();
            adjustmentQuery = adjustmentQuery.Where(p => p.NhanVien != null &&
                (p.NhanVien.MaNhanVien.Contains(term) || p.NhanVien.Ten.Contains(term)));
        }

        ViewBag.Adjustments = await adjustmentQuery.OrderByDescending(p => p.NgayApDung).Take(100).ToListAsync();
        return View();
    }
    [HttpPost]public async Task<IActionResult> AddAdjustment(AdjustmentFormViewModel m){if(!ModelState.IsValid){TempData["Error"]="Thưởng/phạt không hợp lệ.";return RedirectToAction(nameof(Index));}var x=new ThuongPhat{NhanVienID=m.NhanVienID,Loai=m.Loai,LyDo=m.LyDo.Trim(),SoTien=m.SoTien,NgayApDung=m.NgayApDung.Date};_context.ThuongPhats.Add(x);await _context.SaveChangesAsync();_audit.Add("Create","ThuongPhat",x.MaThuongPhat.ToString());await _context.SaveChangesAsync();TempData["Success"]="Đã thêm khoản thưởng/phạt.";return RedirectToAction(nameof(Index),new{employeeId=m.NhanVienID});}
    [HttpPost]public async Task<IActionResult> AddOtherIncome(AdjustmentFormViewModel m){if(!ModelState.IsValid){TempData["Error"]="Thu nhập khác không hợp lệ.";return RedirectToAction(nameof(Index));}var x=new ThuongPhat{NhanVienID=m.NhanVienID,Loai="ThuNhapKhac",LyDo=m.LyDo.Trim(),SoTien=m.SoTien,NgayApDung=m.NgayApDung.Date};_context.ThuongPhats.Add(x);await _context.SaveChangesAsync();_audit.Add("Create","ThuNhapKhac",x.MaThuongPhat.ToString());await _context.SaveChangesAsync();TempData["Success"]="Đã thêm khoản thu nhập khác.";return RedirectToAction(nameof(Index),new{employeeId=m.NhanVienID});}
    private async Task LoadAsync()=>ViewBag.Employees=await _context.NhanViens.AsNoTracking().Where(n=>n.Trangthai).OrderBy(n=>n.Ten).ToListAsync();
}
