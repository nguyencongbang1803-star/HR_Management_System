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
[Authorize(Roles="Admin,HR,QuanLy")]
public class VanHanhController(AppDbContext context,AuditService audit,LeaveService leave,FileStorageService files) : Controller
{
    public async Task<IActionResult> Index()
    {
        var overtimeQuery=context.DangKyTangCas.AsNoTracking().Include(x=>x.NhanVien).Include(x=>x.MinhChungs).AsQueryable();
        var currentEmployeeId=CurrentEmployeeId();
        var isHrManager=currentEmployeeId.HasValue&&await IsHrManagerAsync(currentEmployeeId.Value);
        if(User.IsInRole("QuanLy")&&!isHrManager)
        {
            var employeeId=int.TryParse(User.FindFirstValue("NhanVienID"),out var id)?id:0;
            var departmentId=await context.NhanViens.Where(x=>x.ID==employeeId).Select(x=>x.PhongBanID).FirstOrDefaultAsync();
            overtimeQuery=overtimeQuery.Where(x=>departmentId.HasValue&&x.NhanVien!=null&&x.NhanVien.PhongBanID==departmentId);
        }
        ViewBag.TangCas=await overtimeQuery.OrderByDescending(x=>x.NgayTao).Take(50).ToListAsync();
        var canDecide=new HashSet<int>();
        foreach(var overtime in (IReadOnlyList<DangKyTangCa>)ViewBag.TangCas)if(await CanDecideAsync(overtime,currentEmployeeId))canDecide.Add(overtime.ID);
        ViewBag.CanDecideOvertime=canDecide;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> DecideOvertime(int id,string decision,decimal? approvedHours,decimal? multiplier,string? response,bool returnToRequests=false,string? returnTab=null)
    {
        RedirectToActionResult ErrorBack(string message){TempData["Error"]=message;return returnToRequests?RedirectToAction("Index","YeuCau",new{area="HR",tab=returnTab}):RedirectToAction(nameof(Index));}
        RedirectToActionResult SuccessBack(string message){TempData["Success"]=message;return returnToRequests?RedirectToAction("Index","YeuCau",new{area="HR",tab=returnTab}):RedirectToAction(nameof(Index));}
        var item=await context.DangKyTangCas.Include(x=>x.NhanVien).FirstOrDefaultAsync(x=>x.ID==id);if(item is null)return NotFound();
        if(decision is not (ApprovalStatuses.Approved or ApprovalStatuses.Rejected))return BadRequest();
        if(string.IsNullOrWhiteSpace(response))return ErrorBack("Cần nhập phản hồi trước khi hoàn thành duyệt đơn.");
        if(await IsPayrollLocked(item.Ngay))return ErrorBack("Kỳ lương đã khóa, không thể duyệt OT.");

        var currentEmployeeId=CurrentEmployeeId();
        var route=await leave.GetApprovalRouteAsync(item.NhanVienID);if(route.Error is not null)return ErrorBack(route.Error);
        var isDepartmentStep=route.Route==LeaveApprovalRoute.DepartmentManagerThenHr&&item.TrangThai==ApprovalStatuses.Pending&&currentEmployeeId==route.DepartmentManagerId;
        var isHrFinal=(route.Route==LeaveApprovalRoute.DepartmentManagerThenHr&&item.TrangThai==ApprovalStatuses.ManagerApproved&&(User.IsInRole("HR")||User.IsInRole("Admin")))||(route.Route==LeaveApprovalRoute.HrManagerDirect&&item.TrangThai==ApprovalStatuses.Pending&&currentEmployeeId==route.HrManagerId);
        if(!isDepartmentStep&&!isHrFinal)return Forbid();

        if(isDepartmentStep)
        {
            item.TrangThai=decision==ApprovalStatuses.Approved?ApprovalStatuses.ManagerApproved:ApprovalStatuses.Rejected;
            item.PhanHoiQuanLy=response?.Trim();item.NguoiDuyetQuanLy=User.FindFirstValue(ClaimTypes.Email);item.NgayDuyetQuanLy=DateTime.UtcNow;
            audit.Add(item.TrangThai,"DangKyTangCa",id.ToString(),response);await context.SaveChangesAsync();
            return SuccessBack(item.TrangThai==ApprovalStatuses.ManagerApproved?"Quản lý đã duyệt đề nghị; HR sẽ đối chiếu giờ thực tế.":"Đã từ chối đề nghị tăng ca.");
        }

        if(decision==ApprovalStatuses.Approved)
        {
            item.SoGioDuyet=Math.Min(item.SoGioDangKy,approvedHours??item.SoGioDangKy);
            item.HeSo=2m;
            if(item.SoGioDuyet<=0)return ErrorBack("Số giờ tăng ca duyệt phải lớn hơn 0.");
        }
        item.TrangThai=decision;item.PhanHoi=response?.Trim();item.NguoiDuyet=User.FindFirstValue(ClaimTypes.Email);item.NgayDuyet=DateTime.UtcNow;
        audit.Add(decision,"DangKyTangCa",id.ToString(),response);await context.SaveChangesAsync();return SuccessBack("Đã xử lý yêu cầu tăng ca. Chỉ số giờ đã duyệt mới được tính lương hệ số 2.");
    }

    [HttpGet]
    public async Task<IActionResult> DownloadOvertimeProof(int id)
    {
        var item=await context.DangKyTangCas.AsNoTracking().Include(x=>x.NhanVien).FirstOrDefaultAsync(x=>x.ID==id);if(item?.DuongDanMinhChung is null)return NotFound();
        if(!await CanViewAsync(item,CurrentEmployeeId()))return Forbid();
        try{return File(files.OpenRead(item.DuongDanMinhChung),item.ContentTypeMinhChung??"application/octet-stream",item.TenFileMinhChung??"minh-chung");}catch(FileNotFoundException){return NotFound();}
    }

    [HttpGet]
    public async Task<IActionResult> DownloadOvertimeEvidence(int id)
    {
        var evidence=await context.DangKyTangCaMinhChungs.AsNoTracking()
            .Include(x=>x.DangKyTangCa).ThenInclude(x=>x!.NhanVien)
            .FirstOrDefaultAsync(x=>x.ID==id);
        if(evidence?.DangKyTangCa is null)return NotFound();
        if(!await CanViewAsync(evidence.DangKyTangCa,CurrentEmployeeId()))return Forbid();
        try{return File(files.OpenRead(evidence.DuongDan),evidence.ContentType,evidence.TenFileGoc);}catch(FileNotFoundException){return NotFound();}
    }

    [HttpGet]
    public async Task<IActionResult> PreviewOvertimeEvidence(int id)
    {
        var evidence=await context.DangKyTangCaMinhChungs.AsNoTracking()
            .Include(x=>x.DangKyTangCa).ThenInclude(x=>x!.NhanVien)
            .FirstOrDefaultAsync(x=>x.ID==id);
        if(evidence?.DangKyTangCa is null)return NotFound();
        if(!await CanViewAsync(evidence.DangKyTangCa,CurrentEmployeeId()))return Forbid();
        var previewType=FileStorageService.InlinePreviewContentType(evidence.TenFileGoc);if(previewType is null)return StatusCode(StatusCodes.Status415UnsupportedMediaType);
        try{return File(files.OpenRead(evidence.DuongDan),previewType);}catch(FileNotFoundException){return NotFound();}
    }

    private async Task<bool> CanDecideAsync(DangKyTangCa item,int? currentEmployeeId)
    {
        if(item.TrangThai is ApprovalStatuses.Approved or ApprovalStatuses.Rejected)return false;
        var route=await leave.GetApprovalRouteAsync(item.NhanVienID);if(route.Error is not null)return false;
        return route.Route switch{LeaveApprovalRoute.DepartmentManagerThenHr=>item.TrangThai==ApprovalStatuses.Pending&&currentEmployeeId==route.DepartmentManagerId||item.TrangThai==ApprovalStatuses.ManagerApproved&&(User.IsInRole("HR")||User.IsInRole("Admin")),LeaveApprovalRoute.HrManagerDirect=>item.TrangThai==ApprovalStatuses.Pending&&currentEmployeeId==route.HrManagerId,_=>false};
    }
    private async Task<bool> CanViewAsync(DangKyTangCa item,int? currentEmployeeId){if(User.IsInRole("HR")||User.IsInRole("Admin"))return true;if(!currentEmployeeId.HasValue)return false;var ownDepartment=await context.NhanViens.Where(x=>x.ID==currentEmployeeId).Select(x=>x.PhongBanID).FirstOrDefaultAsync();return ownDepartment.HasValue&&item.NhanVien?.PhongBanID==ownDepartment;}
    private async Task<bool> IsHrManagerAsync(int employeeId){var route=await leave.GetApprovalRouteAsync(employeeId);return route.Error is null&&route.Route==LeaveApprovalRoute.AutoApprove;}
    private int? CurrentEmployeeId()=>int.TryParse(User.FindFirstValue("NhanVienID"),out var id)?id:null;

    private Task<bool> IsPayrollLocked(DateTime date)=>context.Luongs.AnyAsync(x=>x.Thang==date.Month&&x.Nam==date.Year&&(x.TrangThai==PayrollStatuses.Locked||x.TrangThai==PayrollStatuses.Paid));
    private RedirectToActionResult Error(string message){TempData["Error"]=message;return RedirectToAction(nameof(Index));}
    private RedirectToActionResult Success(string message){TempData["Success"]=message;return RedirectToAction(nameof(Index));}
}
