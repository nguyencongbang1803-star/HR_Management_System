using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.Data;
using QLNS.Models.Entities;
using QLNS.Services;

namespace QLNS.Areas.Employee.Controllers;

[Area("Employee"),Authorize(Roles="NhanVien,QuanLy,HR")]
public class ThongBaoController(AppDbContext context,PersonalNotificationService notifications):Controller
{
    public async Task<IActionResult> Index(string? type=null,CancellationToken cancellationToken=default)
    {
        var employeeId=EmployeeId();if(!employeeId.HasValue)return Challenge();
        await notifications.SyncAsync(employeeId.Value,cancellationToken);
        var query=context.ThongBaoCaNhans.AsNoTracking().Where(x=>x.NhanVienID==employeeId.Value);
        if(!string.IsNullOrWhiteSpace(type))query=query.Where(x=>x.Loai==type);
        ViewBag.Type=type;return View(await query.OrderByDescending(x=>x.NgayTao).ToListAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> MarkRead(int? id,string? returnUrl=null)
    {
        var employeeId=EmployeeId();if(!employeeId.HasValue)return Challenge();
        var query=context.ThongBaoCaNhans.Where(x=>x.NhanVienID==employeeId.Value&&!x.DaDoc);
        if(id.HasValue)query=query.Where(x=>x.ID==id.Value);
        var items=await query.ToListAsync();foreach(var item in items){item.DaDoc=true;item.NgayDoc=DateTime.UtcNow;}await context.SaveChangesAsync();
        if(id.HasValue&&items.FirstOrDefault()?.DuongDan is string url&&Url.IsLocalUrl(url))return LocalRedirect(url);
        return !string.IsNullOrWhiteSpace(returnUrl)&&Url.IsLocalUrl(returnUrl)?LocalRedirect(returnUrl):RedirectToAction(nameof(Index));
    }
    private int? EmployeeId()=>int.TryParse(User.FindFirstValue("NhanVienID"),out var id)?id:null;
}
