using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.Data;
using QLNS.Models.Entities;
using QLNS.Services;

namespace QLNS.Areas.HR.Controllers;

[Area("HR")]
[Authorize(Roles="Admin,HR")]
public class LuongController : Controller
{
    private readonly AppDbContext _context;
    private readonly AuditService _audit;
    private readonly PayrollCalculationService _payrollCalculation;
    public LuongController(
        AppDbContext context,
        AuditService audit,
        PayrollCalculationService payrollCalculation)
    {
        _context=context;
        _audit=audit;
        _payrollCalculation=payrollCalculation;
    }

    public async Task<IActionResult> Index(int thang=0,int nam=0,CancellationToken cancellationToken=default)
    {
        NormalizePeriod(ref thang,ref nam);ViewBag.Thang=thang;ViewBag.Nam=nam;
        ViewBag.Readiness=await _payrollCalculation.GetReadinessAsync(thang,nam,cancellationToken);
        return View(await _context.Luongs.AsNoTracking().Include(l=>l.NhanVien).Where(l=>l.Thang==thang&&l.Nam==nam).OrderBy(l=>l.NhanVien!.Ten).ToListAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> TinhLuong(int thang,int nam,CancellationToken cancellationToken)
    {
        if(thang is <1 or >12||nam is <2000 or >2200)return BadRequest();
        var result=await _payrollCalculation.CalculateAsync(thang,nam,cancellationToken);
        if(result.IsBlocked)
        {
            TempData["Error"]=result.Error;
            return RedirectToAction(nameof(Index),new{thang,nam});
        }
        _audit.Add("AutoCalculate","Luong",$"{nam}-{thang:00}",
            $"Tính: {result.Calculated}; tạo: {result.Created}; cập nhật: {result.Updated}; bỏ cũ: {result.Removed}; cảnh báo: {result.Warnings.Count}");
        await _context.SaveChangesAsync(cancellationToken);
        TempData["Success"]=$"Đã tự động tính lương cho {result.Calculated} nhân viên.";
        if(result.Warnings.Count>0)
            TempData["Warning"]=$"{result.Warnings.Count} hồ sơ chưa được tính: {string.Join(" | ",result.Warnings.Take(3))}";
        return RedirectToAction(nameof(Index),new{thang,nam});
    }

    [HttpPost]
    public async Task<IActionResult> ReconcilePeriod(int thang,int nam)
    {
        var items=await _context.Luongs.Where(l=>l.Thang==thang&&l.Nam==nam&&l.TrangThai==PayrollStatuses.Draft).ToListAsync();
        if(items.Count==0){TempData["Error"]="Không có bảng lương nháp để đối soát.";return RedirectToAction(nameof(Index),new{thang,nam});}
        foreach(var item in items){item.TrangThai=PayrollStatuses.Reconciled;item.NgayDoiSoat=DateTime.UtcNow;item.NguoiDoiSoat=User.Identity?.Name;}
        _audit.Add("Reconcile","Luong",$"{nam}-{thang:00}");await _context.SaveChangesAsync();TempData["Success"]="Đã phát hành bảng lương tạm để nhân viên đối soát.";return RedirectToAction(nameof(Index),new{thang,nam});
    }

    [HttpPost]
    public async Task<IActionResult> ReopenPeriod(int thang,int nam,string? reason)
    {
        var items=await _context.Luongs.Where(l=>l.Thang==thang&&l.Nam==nam&&l.TrangThai==PayrollStatuses.Reconciled).ToListAsync();
        if(items.Count==0){TempData["Error"]="Chỉ có thể mở lại kỳ lương đang đối soát.";return RedirectToAction(nameof(Index),new{thang,nam});}
        foreach(var item in items){item.TrangThai=PayrollStatuses.Draft;item.NgayDoiSoat=null;item.NguoiDoiSoat=null;}
        _audit.Add("Reopen","Luong",$"{nam}-{thang:00}",reason?.Trim());await _context.SaveChangesAsync();TempData["Success"]="Đã mở lại bảng lương để xử lý phản hồi và tính lại.";return RedirectToAction(nameof(Index),new{thang,nam});
    }

    [Authorize(Roles="Admin")]
    [HttpPost]
    public async Task<IActionResult> LockPeriod(int thang,int nam)
    {
        var items=await _context.Luongs.Where(l=>l.Thang==thang&&l.Nam==nam&&l.TrangThai==PayrollStatuses.Reconciled).ToListAsync();
        if(items.Count==0){TempData["Error"]="Kỳ lương phải được đối soát trước khi khóa.";return RedirectToAction(nameof(Index),new{thang,nam});}
        foreach(var item in items){item.TrangThai=PayrollStatuses.Locked;item.NgayKhoa=DateTime.UtcNow;item.NguoiKhoa=User.Identity?.Name;}
        _audit.Add("Lock","Luong",$"{nam}-{thang:00}");await _context.SaveChangesAsync();TempData["Success"]="Đã khóa kỳ lương.";return RedirectToAction(nameof(Index),new{thang,nam});
    }

    [Authorize(Roles="Admin")]
    [HttpPost]
    public async Task<IActionResult> MarkPaid(int thang,int nam)
    {
        var items=await _context.Luongs.Where(l=>l.Thang==thang&&l.Nam==nam&&l.TrangThai==PayrollStatuses.Locked).ToListAsync();foreach(var item in items)item.TrangThai=PayrollStatuses.Paid;
        _audit.Add("MarkPaid","Luong",$"{nam}-{thang:00}");await _context.SaveChangesAsync();TempData["Success"]="Đã đánh dấu kỳ lương là đã thanh toán.";return RedirectToAction(nameof(Index),new{thang,nam});
    }

    private static void NormalizePeriod(ref int month,ref int year){if(month is<1 or>12)month=DateTime.Today.Month;if(year is<2000 or>2200)year=DateTime.Today.Year;}
}
