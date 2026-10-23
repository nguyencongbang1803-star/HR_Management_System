using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.Data;
using QLNS.Models.Entities;
using QLNS.Models.ViewModels;

namespace QLNS.Areas.HR.Controllers;

[Area("HR")]
[Authorize(Roles = "Admin,HR")]
public class DashboardController : Controller
{
    private readonly AppDbContext _context;
    public DashboardController(AppDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var in60Days = today.AddDays(60);
        var currentMonth = today.Month;
        var currentYear = today.Year;

        var model = new DashboardViewModel
        {
            ActiveEmployees = await _context.NhanViens.CountAsync(n => n.Trangthai),
            Departments = await _context.PhongBans.CountAsync(),
            PendingLeaves = await _context.NghiPheps.CountAsync(n => n.Trangthai == LeaveStatuses.Pending || n.Trangthai == ApprovalStatuses.ManagerApproved),
            ExpiringContracts = await _context.HopDongs.CountAsync(h => h.Trangthai && h.Ngayketthuc >= today && h.Ngayketthuc <= in60Days),
            CurrentPayroll = await _context.Luongs
                .Where(l => l.Thang == currentMonth && l.Nam == currentYear)
                .SumAsync(l => (decimal?)l.Tongluong) ?? 0,
            RecentLeaves = await _context.NghiPheps.AsNoTracking()
                .Include(n => n.NhanVien)
                .OrderByDescending(n => n.NgayTao)
                .Take(5).ToListAsync(),
            ContractsExpiringSoon = await _context.HopDongs.AsNoTracking()
                .Include(h => h.NhanVien)
                .Where(h => h.Trangthai && h.Ngayketthuc >= today && h.Ngayketthuc <= in60Days)
                .OrderBy(h => h.Ngayketthuc)
                .Take(5).ToListAsync()
        };
        return View(model);
    }
}
