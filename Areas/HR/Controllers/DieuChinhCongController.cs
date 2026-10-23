using System.Security.Claims;
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
public class DieuChinhCongController(AppDbContext context,AuditService audit,FileStorageService files) : Controller
{
    public async Task<IActionResult> Index(string? status=null)
    {
        var query=context.DieuChinhChamCongs.AsNoTracking().Include(x=>x.NhanVien).Include(x=>x.MinhChungs).AsQueryable();
        if(!string.IsNullOrWhiteSpace(status))query=query.Where(x=>x.TrangThai==status);ViewBag.Status=status;
        return View(await query.OrderByDescending(x=>x.NgayTao).ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Decide(int id,string decision,string? response,bool returnToRequests=false,string? returnTab=null)
    {
        IActionResult Back()=>returnToRequests?RedirectToAction("Index","YeuCau",new{area="HR",tab=returnTab}):RedirectToAction(nameof(Index));
        var item=await context.DieuChinhChamCongs.FindAsync(id);if(item is null)return NotFound();
        if(item.TrangThai!=ApprovalStatuses.Pending){TempData["Error"]="Yêu cầu đã được xử lý.";return Back();}
        if(decision is not (ApprovalStatuses.Approved or ApprovalStatuses.Rejected))return BadRequest();
        if(string.IsNullOrWhiteSpace(response)){TempData["Error"]="Cần nhập phản hồi trước khi hoàn thành duyệt đơn.";return Back();}
        if(await context.Luongs.AnyAsync(x=>x.Thang==item.Ngay.Month&&x.Nam==item.Ngay.Year&&x.TrangThai!=PayrollStatuses.Draft)){TempData["Error"]="Kỳ lương đã khóa, không thể sửa công.";return Back();}
        if(decision==ApprovalStatuses.Approved)
        {
            var attendance=await context.ChamCongs.FirstOrDefaultAsync(x=>x.NhanVienID==item.NhanVienID&&x.Ngay==item.Ngay);
            if(attendance is null){attendance=new ChamCong{NhanVienID=item.NhanVienID,Ngay=item.Ngay,TrangThai=AttendanceStatuses.Present};context.Add(attendance);}
            if(item.GioVaoDeXuat.HasValue)attendance.Giovaolam=item.Ngay.Date+item.GioVaoDeXuat.Value;
            if(item.GioRaDeXuat.HasValue)attendance.Giotanlam=item.Ngay.Date+item.GioRaDeXuat.Value;
            attendance.GhiChu=$"Điều chỉnh theo yêu cầu #{item.ID}";
            AttendancePolicy.ApplyLateRule(attendance);
        }
        item.TrangThai=decision;item.PhanHoi=response?.Trim();item.NguoiDuyet=User.FindFirstValue(ClaimTypes.Email);item.NgayDuyet=DateTime.UtcNow;
        audit.Add(decision==ApprovalStatuses.Approved?"Approve":"Reject","DieuChinhChamCong",id.ToString(),response);await context.SaveChangesAsync();TempData["Success"]="Đã xử lý yêu cầu điều chỉnh công.";return Back();
    }

    [HttpGet]
    public async Task<IActionResult> DownloadEvidence(int id)
    {
        var evidence=await context.DieuChinhChamCongMinhChungs.AsNoTracking().FirstOrDefaultAsync(x=>x.ID==id);if(evidence is null)return NotFound();
        try{return File(files.OpenRead(evidence.DuongDan),evidence.ContentType,evidence.TenFileGoc);}catch(FileNotFoundException){return NotFound();}
    }

    [HttpGet]
    public async Task<IActionResult> PreviewEvidence(int id)
    {
        var evidence=await context.DieuChinhChamCongMinhChungs.AsNoTracking().FirstOrDefaultAsync(x=>x.ID==id);if(evidence is null)return NotFound();
        var previewType=FileStorageService.InlinePreviewContentType(evidence.TenFileGoc);if(previewType is null)return StatusCode(StatusCodes.Status415UnsupportedMediaType);
        try{return File(files.OpenRead(evidence.DuongDan),previewType);}catch(FileNotFoundException){return NotFound();}
    }
}
