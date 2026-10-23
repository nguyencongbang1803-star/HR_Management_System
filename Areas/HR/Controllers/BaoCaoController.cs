using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.Data;
using QLNS.Models.ViewModels;

namespace QLNS.Areas.HR.Controllers;

[Area("HR")]
[Authorize(Roles="Admin,HR")]
public class BaoCaoController : Controller
{
    private readonly AppDbContext _context;
    public BaoCaoController(AppDbContext context)=>_context=context;

    public async Task<IActionResult> Index(int thang=0,int nam=0)
    {
        if(thang is<1 or>12)thang=DateTime.Today.Month;if(nam is<2000 or>2200)nam=DateTime.Today.Year;
        ViewBag.Thang=thang;ViewBag.Nam=nam;
        ViewBag.EmployeeByDepartment=await _context.PhongBans.AsNoTracking().Select(p=>new DepartmentSummaryViewModel { Name=p.Tenphongban, Count=p.NhanViens.Count(n=>n.Trangthai)}).OrderByDescending(x=>x.Count).ToListAsync();
        ViewBag.AttendanceCount=await _context.ChamCongs.CountAsync(c=>c.Ngay.Month==thang&&c.Ngay.Year==nam);
        ViewBag.LeaveCount=await _context.NghiPheps.CountAsync(n=>n.Ngaybatdau.Month==thang&&n.Ngaybatdau.Year==nam);
        ViewBag.PayrollTotal=await _context.Luongs.Where(l=>l.Thang==thang&&l.Nam==nam).SumAsync(l=>(decimal?)l.Tongluong)??0;
        ViewBag.PayrollCount=await _context.Luongs.CountAsync(l=>l.Thang==thang&&l.Nam==nam);
        return View();
    }

    [Authorize(Roles="Admin,HR")]
    [HttpGet]
    public async Task<IActionResult> ExportPayroll(int thang,int nam)
    {
        if(thang is<1 or>12||nam is<2000 or>2200)return BadRequest();
        var data=await _context.Luongs.AsNoTracking().Include(l=>l.NhanVien).Where(l=>l.Thang==thang&&l.Nam==nam).OrderBy(l=>l.NhanVien!.MaNhanVien).ToListAsync();
        static string Csv(string? value)=>$"\"{(value??string.Empty).Replace("\"","\"\"")}\"";
        var sb=new StringBuilder();sb.Append('\uFEFF');sb.AppendLine("Mã nhân viên,Họ tên,Ngày công,Lương cơ bản,Hệ số chức vụ,Lương sau hệ số,Phụ cấp,Thưởng,Thu nhập khác,Giờ OT,Tiền OT,Khấu trừ,Bảo hiểm,Thực lĩnh,Trạng thái");
        foreach(var l in data)sb.AppendLine(string.Join(',',Csv(l.NhanVien?.MaNhanVien),Csv(l.NhanVien?.Ten),l.SoNgayCong,l.Luongcoban,l.HeSoLuong,l.Luongcoban*l.HeSoLuong,l.Phucap,l.Thuong,l.ThuNhapKhac,l.SoGioTangCa,l.TienTangCa,l.Khautru,l.BaoHiem,l.Tongluong,Csv(l.TrangThai)));
        return File(Encoding.UTF8.GetBytes(sb.ToString()),"text/csv; charset=utf-8",$"bang-luong-{nam}-{thang:00}.csv");
    }
}
