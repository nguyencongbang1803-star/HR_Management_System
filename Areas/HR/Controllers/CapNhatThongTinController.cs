using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.Data;
using QLNS.Models.Entities;
using QLNS.Services;

namespace QLNS.Areas.HR.Controllers;

[Area("HR"),Authorize(Roles="Admin,HR")]
public class CapNhatThongTinController(AppDbContext context,AuditService audit) : Controller
{
    public async Task<IActionResult> Index(string? status=null)
    {
        var query=context.YeuCauCapNhatThongTinCaNhans.AsNoTracking().Include(x=>x.NhanVien).AsQueryable();
        if(!string.IsNullOrWhiteSpace(status))query=query.Where(x=>x.TrangThai==status);
        return View(await query.OrderByDescending(x=>x.NgayTao).ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Decide(int id,string decision,string? response,bool returnToRequests=false,string? returnTab=null)
    {
        IActionResult Back()=>returnToRequests?RedirectToAction("Index","YeuCau",new{area="HR",tab=returnTab}):RedirectToAction(nameof(Index));
        var item=await context.YeuCauCapNhatThongTinCaNhans.Include(x=>x.NhanVien).FirstOrDefaultAsync(x=>x.ID==id);if(item is null)return NotFound();
        if(item.TrangThai!=ApprovalStatuses.Pending){TempData["Error"]="Yêu cầu đã được xử lý.";return Back();}
        if(decision is not(ApprovalStatuses.Approved or ApprovalStatuses.Rejected))return BadRequest();
        if(string.IsNullOrWhiteSpace(response)){TempData["Error"]="Cần nhập phản hồi trước khi hoàn thành duyệt đơn.";return Back();}
        if(decision==ApprovalStatuses.Approved)
        {
            if(item.NhanVien is null)return NotFound();
            if(!string.IsNullOrWhiteSpace(item.Email)&&await context.NhanViens.AnyAsync(x=>x.ID!=item.NhanVienID&&x.Email==item.Email)){TempData["Error"]="Email đề xuất đã thuộc nhân viên khác.";return Back();}
            var normalizedEmail=item.Email?.ToUpperInvariant();
            if(normalizedEmail is not null&&await context.TaiKhoans.AnyAsync(x=>x.NhanVienID!=item.NhanVienID&&x.NormalizedEmail==normalizedEmail)){TempData["Error"]="Email đề xuất đã thuộc tài khoản khác.";return Back();}
            if(!string.IsNullOrWhiteSpace(item.CanCuocCongDan)&&await context.NhanViens.AnyAsync(x=>x.ID!=item.NhanVienID&&x.CanCuocCongDan==item.CanCuocCongDan)){TempData["Error"]="CCCD đề xuất đã thuộc nhân viên khác.";return Back();}
            item.NhanVien.Email=item.Email;item.NhanVien.SDT=item.SDT;item.NhanVien.NgaySinh=item.NgaySinh;item.NhanVien.Gioitinh=item.GioiTinh;item.NhanVien.CanCuocCongDan=item.CanCuocCongDan;item.NhanVien.DiaChi=item.DiaChi;item.NhanVien.SoTaiKhoan=item.SoTaiKhoan;item.NhanVien.MaSothue=item.MaSoThue;item.NhanVien.Ngaycapnhat=DateTime.UtcNow;
            var account=await context.TaiKhoans.FirstOrDefaultAsync(x=>x.NhanVienID==item.NhanVienID);if(account is not null){account.Email=item.Email;account.NormalizedEmail=normalizedEmail;}
        }
        item.TrangThai=decision;item.PhanHoi=response?.Trim();item.NguoiDuyet=User.FindFirstValue(ClaimTypes.Email);item.NgayDuyet=DateTime.UtcNow;audit.Add(decision,"YeuCauCapNhatThongTinCaNhan",id.ToString(),response);await context.SaveChangesAsync();TempData["Success"]=decision==ApprovalStatuses.Approved?"Đã duyệt và cập nhật hồ sơ nhân viên.":"Đã từ chối yêu cầu.";return Back();
    }
}
