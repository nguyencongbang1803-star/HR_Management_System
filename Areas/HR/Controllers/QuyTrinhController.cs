using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.Data;
using QLNS.Models.Entities;
using QLNS.Services;

namespace QLNS.Areas.HR.Controllers;

[Area("HR")][Authorize(Roles="Admin,HR")]
public class QuyTrinhController(AppDbContext context,AuditService audit) : Controller
{
    public async Task<IActionResult> Index()=>View(await context.QuyTrinhPheDuyets.AsNoTracking().Include(x=>x.CacBuoc.OrderBy(b=>b.ThuTu)).OrderBy(x=>x.TenQuyTrinh).ToListAsync());
    [HttpPost]public async Task<IActionResult> Save(string requestType,string name,string steps)
    {
        var roles=steps.Split(new[]{'\r','\n',',',';'},StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries).ToList();
        if(string.IsNullOrWhiteSpace(requestType)||string.IsNullOrWhiteSpace(name)||roles.Count==0||roles.Any(x=>x is not("QuanLy" or "HR" or "Admin"))){TempData["Error"]="Luồng duyệt không hợp lệ. Vai trò hỗ trợ: QuanLy, HR, Admin.";return RedirectToAction(nameof(Index));}
        var item=await context.QuyTrinhPheDuyets.Include(x=>x.CacBuoc).FirstOrDefaultAsync(x=>x.LoaiYeuCau==requestType.Trim());
        if(item is null){item=new QuyTrinhPheDuyet{LoaiYeuCau=requestType.Trim()};context.Add(item);}else context.RemoveRange(item.CacBuoc);
        item.TenQuyTrinh=name.Trim();item.HoatDong=true;item.CacBuoc=roles.Select((role,index)=>new BuocPheDuyet{ThuTu=index+1,VaiTroDuyet=role,TenBuoc=$"Bước {index+1}: {role}"}).ToList();audit.Add("Upsert","QuyTrinhPheDuyet",requestType);await context.SaveChangesAsync();TempData["Success"]="Đã lưu luồng phê duyệt.";return RedirectToAction(nameof(Index));
    }
    [HttpPost]public async Task<IActionResult> Toggle(int id){var item=await context.QuyTrinhPheDuyets.FindAsync(id);if(item is null)return NotFound();item.HoatDong=!item.HoatDong;audit.Add("Toggle","QuyTrinhPheDuyet",id.ToString());await context.SaveChangesAsync();return RedirectToAction(nameof(Index));}
}
