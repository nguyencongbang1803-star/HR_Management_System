using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.Data;
using QLNS.Models.Entities;

namespace QLNS.Areas.HR.Controllers;

[Area("HR")]
[Authorize(Roles="Admin,HR")]
public class ThongBaoController : Controller
{
    private readonly AppDbContext _context;public ThongBaoController(AppDbContext context)=>_context=context;
    public async Task<IActionResult> Index()
    {
        var today=DateTime.Today;var soon=today.AddDays(60);
        ViewBag.PendingLeaves=await _context.NghiPheps.AsNoTracking().Include(n=>n.NhanVien).Where(n=>n.Trangthai==LeaveStatuses.Pending||n.Trangthai==ApprovalStatuses.ManagerApproved).OrderBy(n=>n.Ngaybatdau).ToListAsync();
        ViewBag.ExpiringContracts=await _context.HopDongs.AsNoTracking().Include(h=>h.NhanVien).Where(h=>h.Trangthai&&h.Ngayketthuc>=today&&h.Ngayketthuc<=soon).OrderBy(h=>h.Ngayketthuc).ToListAsync();
        return View();
    }
}
