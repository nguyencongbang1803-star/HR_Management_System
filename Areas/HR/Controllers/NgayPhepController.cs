using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.Data;
using QLNS.Models.Entities;
using QLNS.Models.ViewModels;
using QLNS.Services;

namespace QLNS.Areas.HR.Controllers;

[Area("HR")]
[Authorize(Roles="Admin,HR")]
public class NgayPhepController(AppDbContext context,AuditService audit) : Controller
{
    public async Task<IActionResult> Index(int nam=0)
    {
        if(nam==0)nam=DateTime.Today.Year;ViewBag.Nam=nam;
        var employees=await context.NhanViens.AsNoTracking().Include(x=>x.ChucVu)
            .Where(x=>x.Trangthai).OrderBy(x=>x.Ten).ToListAsync();
        var types=await context.LoaiNghiPheps.AsNoTracking().Where(x=>x.TruQuyPhep&&x.HoatDong)
            .OrderBy(x=>x.TenLoai).ToListAsync();
        var saved=await context.SoDuNghiPheps.AsNoTracking().Where(x=>x.Nam==nam).ToListAsync();
        var savedByKey=saved.ToDictionary(x=>(x.NhanVienID,x.LoaiNghiPhepID));
        var rows=employees.SelectMany(employee=>types.Select(type=>
        {
            savedByKey.TryGetValue((employee.ID,type.ID),out var balance);
            return new SoDuNghiPhep
            {
                ID=balance?.ID??0,NhanVienID=employee.ID,LoaiNghiPhepID=type.ID,Nam=nam,
                SoNgayDuocHuong=employee.ChucVu?.SoNgayPhepNam??16m,
                DieuChinh=balance?.DieuChinh??0m,GhiChu=balance?.GhiChu,
                NhanVien=employee,LoaiNghiPhep=type
            };
        })).ToList();
        ViewBag.NhanViens=employees;ViewBag.LoaiNghi=types;
        return View(rows);
    }

    [HttpPost]
    public async Task<IActionResult> SaveBalance(LeaveBalanceFormViewModel model)
    {
        if(!ModelState.IsValid){TempData["Error"]="Thông tin quỹ phép không hợp lệ.";return RedirectToAction(nameof(Index),new{nam=model.Nam});}
        var employee=await context.NhanViens.AsNoTracking().Include(x=>x.ChucVu).FirstOrDefaultAsync(x=>x.ID==model.NhanVienID&&x.Trangthai);
        var type=await context.LoaiNghiPheps.AsNoTracking().FirstOrDefaultAsync(x=>x.ID==model.LoaiNghiPhepID&&x.TruQuyPhep&&x.HoatDong);
        if(employee is null||type is null){TempData["Error"]="Nhân viên hoặc loại nghỉ phép không hợp lệ.";return RedirectToAction(nameof(Index),new{nam=model.Nam});}
        var item=await context.SoDuNghiPheps.FirstOrDefaultAsync(x=>x.NhanVienID==model.NhanVienID&&x.LoaiNghiPhepID==model.LoaiNghiPhepID&&x.Nam==model.Nam);
        if(item is null){item=new SoDuNghiPhep{NhanVienID=model.NhanVienID,LoaiNghiPhepID=model.LoaiNghiPhepID,Nam=model.Nam};context.Add(item);}
        item.SoNgayDuocHuong=employee.ChucVu?.SoNgayPhepNam??16m;item.DieuChinh=model.DieuChinh;item.GhiChu=model.GhiChu?.Trim();
        audit.Add("Upsert","SoDuNghiPhep",$"{model.NhanVienID}-{model.LoaiNghiPhepID}-{model.Nam}");await context.SaveChangesAsync();TempData["Success"]="Đã cập nhật quỹ phép.";return RedirectToAction(nameof(Index),new{nam=model.Nam});
    }
}
