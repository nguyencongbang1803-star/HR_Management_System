using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.Data;
using QLNS.Services;

namespace QLNS.ViewComponents;

public class NotificationMenuViewComponent(AppDbContext context,PersonalNotificationService notifications):ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        if(!int.TryParse(UserClaimsPrincipal.FindFirst("NhanVienID")?.Value,out var employeeId))return Content(string.Empty);
        await notifications.SyncAsync(employeeId);
        var unread=await context.ThongBaoCaNhans.CountAsync(x=>x.NhanVienID==employeeId&&!x.DaDoc);
        return View(unread);
    }
}
