using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.Data;
using QLNS.Models.Entities;
using QLNS.Models.ViewModels;
using QLNS.Services;

namespace QLNS.Areas.Employee.Controllers;

[Area("Employee")]
[Authorize(Roles="NhanVien,HR,QuanLy")]
public class HomeController : Controller
{
    private readonly AppDbContext _context;private readonly AuditService _audit;private readonly LeaveService _leave;private readonly FileStorageService _files;
    public HomeController(AppDbContext context,AuditService audit,LeaveService leave,FileStorageService files){_context=context;_audit=audit;_leave=leave;_files=files;}
    public async Task<IActionResult> Index()
    {
        var id=EmployeeId();if(!id.HasValue)return View("NoProfile");
        var employee=await _context.NhanViens.AsNoTracking().Include(n=>n.PhongBan).Include(n=>n.ChucVu).FirstOrDefaultAsync(n=>n.ID==id);if(employee is null)return View("NoProfile");

        var approvedLeavesThisYear = await _context.NghiPheps.AsNoTracking()
            .Where(x => x.NhanVienID == id.Value && x.LoaiNghiPhepID.HasValue &&
                x.Trangthai == LeaveStatuses.Approved && x.Ngaybatdau.Year == DateTime.Today.Year)
            .ToListAsync();
        var usedLeaveDays = approvedLeavesThisYear
            .GroupBy(x => x.LoaiNghiPhepID!.Value)
            .ToDictionary(x => x.Key, x => (decimal)x.Sum(y => LeaveService.BusinessDays(y.Ngaybatdau, y.NgayKetThuc)));
        var currentAllowancePolicies = (await _context.ChinhSachPhuCaps.AsNoTracking()
            .Where(x => x.HoatDong && x.NgayBatDau <= DateTime.Today).ToListAsync())
            .Where(x => PayrollFormula.IsAllowanceApplicable(x, employee, DateTime.Today)).ToList();
        var currentInsurancePolicies = (await _context.ChinhSachBhxhs.AsNoTracking()
            .Where(x => x.HoatDong && x.NgayBatDau <= DateTime.Today).ToListAsync())
            .Where(x => PayrollFormula.IsSocialInsuranceApplicable(x, employee, DateTime.Today)).ToList();
        var quotaTypes=await _context.LoaiNghiPheps.AsNoTracking().Where(x=>x.HoatDong&&x.TruQuyPhep).OrderBy(x=>x.TenLoai).ToListAsync();
        var savedBalances=await _context.SoDuNghiPheps.AsNoTracking().Where(x=>x.NhanVienID==id&&x.Nam==DateTime.Today.Year).ToListAsync();
        var effectiveLeaveBalances=quotaTypes.Select(type=>
        {
            var saved=savedBalances.FirstOrDefault(x=>x.LoaiNghiPhepID==type.ID);
            return new SoDuNghiPhep{ID=saved?.ID??0,NhanVienID=id.Value,LoaiNghiPhepID=type.ID,Nam=DateTime.Today.Year,
                SoNgayDuocHuong=employee.ChucVu?.SoNgayPhepNam??16m,DieuChinh=saved?.DieuChinh??0m,GhiChu=saved?.GhiChu,LoaiNghiPhep=type,NhanVien=employee};
        }).ToList();
        
        return View(new EmployeePortalViewModel{
            Employee=employee,
            Payrolls=await _context.Luongs.AsNoTracking().Where(l=>l.NhanVienID==id&&l.TrangThai!=PayrollStatuses.Draft).OrderByDescending(l=>l.Nam).ThenByDescending(l=>l.Thang).Take(12).ToListAsync(),
            LeaveBalances=effectiveLeaveBalances,
            UsedLeaveDaysByType=usedLeaveDays,
            Contracts=await _context.HopDongs.AsNoTracking().Where(x=>x.NhanVienID==id.Value).OrderByDescending(x=>x.Ngaybatdau).ToListAsync(),
            Qualifications=await _context.TrinhDoChungChis.AsNoTracking().Where(x=>x.NhanVienID==id.Value).OrderByDescending(x=>x.NgayCap).ToListAsync(),
            Allowances=await _context.PhuCaps.AsNoTracking().Where(x=>x.NhanVienID==id.Value).OrderByDescending(x=>x.NgayApDung).ToListAsync(),
            ScopedAllowances=currentAllowancePolicies,
            ScopedInsurances=currentInsurancePolicies,
            Insurances=await _context.BaoHiems.AsNoTracking().Include(x=>x.LoaiBaoHiem).Where(x=>x.NhanVienID==id.Value).OrderByDescending(x=>x.NgayBatDau).ToListAsync()
        });
    }
    [HttpGet]
    public async Task<IActionResult> Attendance(int? thang,int? nam)
    {
        var id=EmployeeId();if(!id.HasValue)return View("NoProfile");
        var today=DateTime.Today;
        var month=thang is >=1 and <=12?thang.Value:today.Month;
        var year=nam is >=2000 and <=2200?nam.Value:today.Year;
        var employee=await _context.NhanViens.AsNoTracking().Include(x=>x.PhongBan).Include(x=>x.ChucVu).FirstOrDefaultAsync(x=>x.ID==id.Value);
        if(employee is null)return View("NoProfile");
        var from=new DateTime(year,month,1);var to=from.AddMonths(1);
        var records=await _context.ChamCongs.AsNoTracking().Where(x=>x.NhanVienID==id.Value&&x.Ngay>=from&&x.Ngay<to).OrderBy(x=>x.Ngay).ToListAsync();
        var payroll=await _context.Luongs.AsNoTracking()
            .FirstOrDefaultAsync(x=>x.NhanVienID==id.Value&&x.Thang==month&&x.Nam==year&&x.TrangThai!=PayrollStatuses.Draft);
        return View(new EmployeeMonthlyAttendanceViewModel{Employee=employee,Month=month,Year=year,Records=records,CurrentAttendance=await CurrentAttendanceAsync(id.Value),Payroll=payroll});
    }
    [HttpGet]public IActionResult RequestLeave()=>View(new LeaveFormViewModel{NhanVienID=EmployeeId()??0});
    [HttpPost]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> UpdateAvatar(IFormFile? avatar)
    {
        var id=EmployeeId();if(!id.HasValue)return Challenge();
        if(avatar is null){TempData["Error"]="Hãy chọn ảnh đại diện.";return RedirectToAction(nameof(Index));}
        var employee=await _context.NhanViens.FindAsync(id.Value);if(employee is null)return View("NoProfile");
        try
        {
            var stored=await _files.SaveAvatarAsync(avatar);
            var previous=employee.AnhDaiDien;
            employee.AnhDaiDien=stored.RelativePath;employee.Ngaycapnhat=DateTime.UtcNow;
            _audit.Add("UpdateAvatar","NhanVien",employee.ID.ToString(),"Employee self-service");
            await _context.SaveChangesAsync();
            if(previous!=employee.AnhDaiDien)_files.Delete(previous);
            TempData["Success"]="Đã cập nhật ảnh đại diện.";
        }
        catch(InvalidOperationException exception){TempData["Error"]=exception.Message;}
        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    public async Task<IActionResult> RemoveAvatar()
    {
        var id=EmployeeId();if(!id.HasValue)return Challenge();
        var employee=await _context.NhanViens.FindAsync(id.Value);if(employee is null)return View("NoProfile");
        var previous=employee.AnhDaiDien;employee.AnhDaiDien=null;employee.Ngaycapnhat=DateTime.UtcNow;
        _audit.Add("RemoveAvatar","NhanVien",employee.ID.ToString(),"Employee self-service");
        await _context.SaveChangesAsync();_files.Delete(previous);
        TempData["Success"]="Đã xóa ảnh đại diện.";
        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    public async Task<IActionResult> RequestLeave(LeaveFormViewModel model)
    {
        var id=EmployeeId();if(!id.HasValue)return Challenge();model.NhanVienID=id.Value;
        var type=await _context.LoaiNghiPheps.AsNoTracking().SingleOrDefaultAsync(x=>x.MaLoai=="PHEP_NAM"&&x.HoatDong);
        model.LoaiNghiPhepID=type?.ID;
        if(type is null)ModelState.AddModelError(string.Empty,"Hệ thống chưa cấu hình loại nghỉ phép năm.");
        var error=await _leave.ValidateRequestAsync(id.Value,model.LoaiNghiPhepID,model.Ngaybatdau,model.NgayKetThuc,model.LaDotXuat);
        if(error is not null)ModelState.AddModelError(string.Empty,error);
        var route=await _leave.GetApprovalRouteAsync(id.Value);
        if(route.Error is not null)ModelState.AddModelError(string.Empty,route.Error);
        if(!ModelState.IsValid)return View(model);

        var allocation=await _leave.CalculatePayAllocationAsync(id.Value,model.LoaiNghiPhepID!.Value,model.Ngaybatdau,model.NgayKetThuc);
        var storedFiles=new List<StoredFile>();
        try{foreach(var file in model.FilesMinhChung)storedFiles.Add(await _files.SaveAsync(file,"leave-proofs"));}
        catch(InvalidOperationException exception){foreach(var file in storedFiles)_files.Delete(file.RelativePath);ModelState.AddModelError(nameof(model.FilesMinhChung),exception.Message);return View(model);}
        var firstProof=storedFiles[0];

        var autoApproved=route.Route==LeaveApprovalRoute.AutoApprove;
        var item=new NghiPhep{
            NhanVienID=id.Value,LoaiNghiPhepID=model.LoaiNghiPhepID,LoaiNghi=type!.TenLoai,
            Ngaybatdau=model.Ngaybatdau.Date,NgayKetThuc=model.NgayKetThuc.Date,Lydo=model.Lydo.Trim(),
            LaDotXuat=model.LaDotXuat,Trangthai=autoApproved?LeaveStatuses.Approved:LeaveStatuses.Pending,
            SoNgayHuongLuong=allocation.PaidDays,SoNgayKhongLuong=allocation.UnpaidDays,
            TenFileMinhChung=firstProof.OriginalName,DuongDanMinhChung=firstProof.RelativePath,ContentTypeMinhChung=firstProof.ContentType,
            MinhChungs=storedFiles.Select(x=>new NghiPhepMinhChung{TenFileGoc=x.OriginalName,DuongDan=x.RelativePath,ContentType=x.ContentType,KichThuoc=x.Length}).ToList(),
            NguoiDuyet=autoApproved?(User.FindFirstValue(ClaimTypes.Email)??User.Identity?.Name):null,
            NgayDuyet=autoApproved?DateTime.UtcNow:null,NgayTao=DateTime.UtcNow};
        _context.NghiPheps.Add(item);
        if(autoApproved)await _leave.ApplyApprovedAttendanceAsync(item);
        try{await _context.SaveChangesAsync();}
        catch{foreach(var file in storedFiles)_files.Delete(file.RelativePath);throw;}
        _audit.Add(autoApproved?"AutoApprove":"Create","NghiPhep",item.ID.ToString(),"Employee self-service");
        await _context.SaveChangesAsync();
        TempData["Success"]=autoApproved?"Đơn nghỉ đã được tự động duyệt.":
            allocation.UnpaidDays>0?$"Đã gửi đơn: {allocation.PaidDays} ngày có lương, {allocation.UnpaidDays} ngày không lương.":"Đơn nghỉ đã được gửi.";
        return RedirectToAction(nameof(Index));
    }
    [HttpPost]public async Task<IActionResult> CancelLeave(int id){var employeeId=EmployeeId();var item=await _context.NghiPheps.FirstOrDefaultAsync(n=>n.ID==id&&n.NhanVienID==employeeId);if(item is null)return NotFound();if(item.Trangthai is not(LeaveStatuses.Pending or ApprovalStatuses.ManagerApproved)){TempData["Error"]="Chỉ có thể hủy đơn chưa được duyệt hoàn tất.";return RedirectToAction(nameof(Index));}item.Trangthai=LeaveStatuses.Cancelled;_audit.Add("Cancel","NghiPhep",id.ToString(),"Employee self-service");await _context.SaveChangesAsync();TempData["Success"]="Đã hủy đơn nghỉ.";return RedirectToAction(nameof(Index));}
    [HttpGet]
    public async Task<IActionResult> DownloadLeaveProof(int id)
    {
        var employeeId=EmployeeId();if(!employeeId.HasValue)return Challenge();
        var item=await _context.NghiPheps.AsNoTracking().FirstOrDefaultAsync(x=>x.ID==id&&x.NhanVienID==employeeId.Value);
        if(item?.DuongDanMinhChung is null)return NotFound();
        try{return File(_files.OpenRead(item.DuongDanMinhChung),item.ContentTypeMinhChung??"application/octet-stream",item.TenFileMinhChung??"minh-chung");}
        catch(FileNotFoundException){return NotFound();}
    }
    [HttpGet]
    public async Task<IActionResult> DownloadLeaveEvidence(int id)
    {
        var employeeId=EmployeeId();if(!employeeId.HasValue)return Challenge();
        var proof=await _context.NghiPhepMinhChungs.AsNoTracking().Include(x=>x.NghiPhep).FirstOrDefaultAsync(x=>x.ID==id&&x.NghiPhep!.NhanVienID==employeeId.Value);
        if(proof is null)return NotFound();
        try{return File(_files.OpenRead(proof.DuongDan),proof.ContentType,proof.TenFileGoc);}catch(FileNotFoundException){return NotFound();}
    }
    [HttpGet]
    public async Task<IActionResult> RequestAttendanceAdjustment(int? attendanceId)
    {
        var employeeId=EmployeeId();if(!employeeId.HasValue)return Challenge();
        if(!attendanceId.HasValue){TempData["Error"]="Hãy chọn nút Chỉnh sửa công tại dòng cần điều chỉnh.";return RedirectToAction(nameof(Attendance));}
        var attendance=await _context.ChamCongs.AsNoTracking().FirstOrDefaultAsync(x=>x.ID==attendanceId&&x.NhanVienID==employeeId.Value);
        if(attendance is null)return NotFound();
        return View(new AttendanceAdjustmentFormViewModel{ChamCongID=attendance.ID,Ngay=attendance.Ngay,GioVao=attendance.Giovaolam?.TimeOfDay,GioRa=attendance.Giotanlam?.TimeOfDay});
    }
    [HttpPost,DisableRequestSizeLimit]
    public async Task<IActionResult> RequestAttendanceAdjustment(AttendanceAdjustmentFormViewModel model,CancellationToken cancellationToken)
    {
        var id=EmployeeId();if(!id.HasValue)return Challenge();
        var attendance=await _context.ChamCongs.AsNoTracking().FirstOrDefaultAsync(x=>x.ID==model.ChamCongID&&x.NhanVienID==id.Value);
        if(attendance is null)ModelState.AddModelError(string.Empty,"Không tìm thấy dòng chấm công của bạn.");else model.Ngay=attendance.Ngay;
        if(await _context.Luongs.AnyAsync(x=>x.Thang==model.Ngay.Month&&x.Nam==model.Ngay.Year&&(x.TrangThai==PayrollStatuses.Locked||x.TrangThai==PayrollStatuses.Paid)))ModelState.AddModelError(string.Empty,"Kỳ lương đã chốt, không thể yêu cầu sửa công.");
        if(await _context.DieuChinhChamCongs.AnyAsync(x=>x.ChamCongID==model.ChamCongID&&x.TrangThai==ApprovalStatuses.Pending))ModelState.AddModelError(string.Empty,"Dòng này đã có yêu cầu đang chờ xử lý.");
        if(!ModelState.IsValid)return View(model);
        var storedFiles=new List<StoredFile>();
        try{foreach(var file in model.AnhMinhChung)storedFiles.Add(await _files.SaveEvidenceImageAsync(file,cancellationToken));}
        catch(InvalidOperationException exception){foreach(var file in storedFiles)_files.Delete(file.RelativePath);ModelState.AddModelError(nameof(model.AnhMinhChung),exception.Message);return View(model);}
        var item=new DieuChinhChamCong{ChamCongID=model.ChamCongID,NhanVienID=id.Value,Ngay=model.Ngay.Date,GioVaoDeXuat=model.GioVao,GioRaDeXuat=model.GioRa,LyDo=model.LyDo.Trim(),MinhChungs=storedFiles.Select(x=>new DieuChinhChamCongMinhChung{TenFileGoc=x.OriginalName,DuongDan=x.RelativePath,ContentType=x.ContentType,KichThuoc=x.Length}).ToList()};
        _context.Add(item);try{await _context.SaveChangesAsync(cancellationToken);}catch{foreach(var file in storedFiles)_files.Delete(file.RelativePath);throw;}
        _audit.Add("Create","DieuChinhChamCong",item.ID.ToString(),"Employee self-service");await _context.SaveChangesAsync(cancellationToken);TempData["Success"]="Đã gửi yêu cầu điều chỉnh công kèm ảnh minh chứng.";return RedirectToAction(nameof(Attendance),new{thang=model.Ngay.Month,nam=model.Ngay.Year});
    }
    [HttpGet]public IActionResult RequestOvertime()=>View(new OvertimeRequestViewModel());
    [HttpPost,DisableRequestSizeLimit]
    public async Task<IActionResult> RequestOvertime(OvertimeRequestViewModel model,CancellationToken cancellationToken)
    {
        var id=EmployeeId();if(!id.HasValue)return Challenge();
        if(await _context.DangKyTangCas.AnyAsync(x=>x.NhanVienID==id&&x.Ngay==model.Ngay.Date&&x.TrangThai!=ApprovalStatuses.Rejected))ModelState.AddModelError(string.Empty,"Ngày này đã có đăng ký OT.");
        if(await IsPayrollLocked(model.Ngay))ModelState.AddModelError(string.Empty,"Kỳ lương đã khóa.");
        var route=await _leave.GetApprovalRouteAsync(id.Value);if(route.Error is not null)ModelState.AddModelError(string.Empty,route.Error);
        if(!ModelState.IsValid)return View(model);
        var storedFiles=new List<StoredFile>();try{foreach(var file in model.FilesMinhChung)storedFiles.Add(await _files.SaveAsync(file,"overtime-proofs",cancellationToken));}catch(InvalidOperationException exception){foreach(var file in storedFiles)_files.Delete(file.RelativePath);ModelState.AddModelError(nameof(model.FilesMinhChung),exception.Message);return View(model);}
        var auto=route.Route==LeaveApprovalRoute.AutoApprove;
        var firstProof=storedFiles.FirstOrDefault();
        var item=new DangKyTangCa{NhanVienID=id.Value,Ngay=model.Ngay.Date,GioBatDau=model.GioBatDau,GioKetThuc=model.GioKetThuc,LyDo=model.LyDo.Trim(),TenFileMinhChung=firstProof?.OriginalName,DuongDanMinhChung=firstProof?.RelativePath,ContentTypeMinhChung=firstProof?.ContentType,MinhChungs=storedFiles.Select(x=>new DangKyTangCaMinhChung{TenFileGoc=x.OriginalName,DuongDan=x.RelativePath,ContentType=x.ContentType,KichThuoc=x.Length}).ToList(),TrangThai=auto?ApprovalStatuses.Approved:ApprovalStatuses.Pending,HeSo=2m,SoGioDuyet=auto?(decimal)(model.GioKetThuc-model.GioBatDau).TotalHours:0,NguoiDuyet=auto?User.FindFirstValue(ClaimTypes.Email):null,NgayDuyet=auto?DateTime.UtcNow:null};
        _context.Add(item);try{await _context.SaveChangesAsync(cancellationToken);}catch{foreach(var file in storedFiles)_files.Delete(file.RelativePath);throw;}_audit.Add(auto?"AutoApprove":"Create","DangKyTangCa",item.ID.ToString(),"Employee self-service");await _context.SaveChangesAsync(cancellationToken);TempData["Success"]=auto?"Đăng ký tăng ca của Trưởng phòng HR đã tự động duyệt.":"Đã gửi đăng ký tăng ca.";return RedirectToAction(nameof(Index));
    }
    [HttpGet]
    public async Task<IActionResult> DownloadOvertimeProof(int id){var employeeId=EmployeeId();if(!employeeId.HasValue)return Challenge();var item=await _context.DangKyTangCas.AsNoTracking().FirstOrDefaultAsync(x=>x.ID==id&&x.NhanVienID==employeeId.Value);if(item?.DuongDanMinhChung is null)return NotFound();try{return File(_files.OpenRead(item.DuongDanMinhChung),item.ContentTypeMinhChung??"application/octet-stream",item.TenFileMinhChung??"minh-chung");}catch(FileNotFoundException){return NotFound();}}
    [HttpGet]public async Task<IActionResult> DownloadOvertimeEvidence(int id){var employeeId=EmployeeId();if(!employeeId.HasValue)return Challenge();var proof=await _context.DangKyTangCaMinhChungs.AsNoTracking().Include(x=>x.DangKyTangCa).FirstOrDefaultAsync(x=>x.ID==id&&x.DangKyTangCa!.NhanVienID==employeeId.Value);if(proof is null)return NotFound();try{return File(_files.OpenRead(proof.DuongDan),proof.ContentType,proof.TenFileGoc);}catch(FileNotFoundException){return NotFound();}}

    [HttpGet]
    public async Task<IActionResult> RequestPersonalInformationChange()
    {
        var id=EmployeeId();if(!id.HasValue)return Challenge();var employee=await _context.NhanViens.AsNoTracking().FirstOrDefaultAsync(x=>x.ID==id.Value);if(employee is null)return View("NoProfile");
        ViewBag.Requests=await _context.YeuCauCapNhatThongTinCaNhans.AsNoTracking().Where(x=>x.NhanVienID==id.Value).OrderByDescending(x=>x.NgayTao).Take(10).ToListAsync();
        return View(new PersonalInformationChangeViewModel{Email=employee.Email,SDT=employee.SDT,NgaySinh=employee.NgaySinh,GioiTinh=employee.Gioitinh,CanCuocCongDan=employee.CanCuocCongDan,DiaChi=employee.DiaChi,SoTaiKhoan=employee.SoTaiKhoan,MaSoThue=employee.MaSothue});
    }
    [HttpPost]
    public async Task<IActionResult> RequestPersonalInformationChange(PersonalInformationChangeViewModel model)
    {
        var id=EmployeeId();if(!id.HasValue)return Challenge();
        if(await _context.YeuCauCapNhatThongTinCaNhans.AnyAsync(x=>x.NhanVienID==id.Value&&x.TrangThai==ApprovalStatuses.Pending))ModelState.AddModelError(string.Empty,"Bạn đang có một yêu cầu chờ HR xử lý.");
        if(!ModelState.IsValid){ViewBag.Requests=await _context.YeuCauCapNhatThongTinCaNhans.AsNoTracking().Where(x=>x.NhanVienID==id.Value).OrderByDescending(x=>x.NgayTao).Take(10).ToListAsync();return View(model);}
        var item=new YeuCauCapNhatThongTinCaNhan{NhanVienID=id.Value,Email=Clean(model.Email),SDT=Clean(model.SDT),NgaySinh=model.NgaySinh?.Date,GioiTinh=model.GioiTinh,CanCuocCongDan=Clean(model.CanCuocCongDan),DiaChi=Clean(model.DiaChi),SoTaiKhoan=Clean(model.SoTaiKhoan),MaSoThue=Clean(model.MaSoThue),LyDo=model.LyDo.Trim()};_context.Add(item);await _context.SaveChangesAsync();_audit.Add("Create","YeuCauCapNhatThongTinCaNhan",item.ID.ToString(),"Employee self-service");await _context.SaveChangesAsync();TempData["Success"]="Đã gửi yêu cầu. Dữ liệu hồ sơ chỉ thay đổi sau khi HR duyệt.";return RedirectToAction(nameof(RequestPersonalInformationChange));
    }
    private Task<bool> IsPayrollLocked(DateTime date)=>_context.Luongs.AnyAsync(x=>x.Thang==date.Month&&x.Nam==date.Year&&(x.TrangThai==PayrollStatuses.Locked||x.TrangThai==PayrollStatuses.Paid));
    private Task<ChamCong?> CurrentAttendanceAsync(int employeeId){var today=DateTime.Today;var since=today.AddDays(-1);return _context.ChamCongs.AsNoTracking().Where(x=>x.NhanVienID==employeeId&&(x.Ngay==today||(x.Ngay>=since&&x.Giovaolam!=null&&x.Giotanlam==null))).OrderByDescending(x=>x.Giotanlam==null).ThenByDescending(x=>x.Ngay).FirstOrDefaultAsync();}
    private int? EmployeeId()=>int.TryParse(User.FindFirstValue("NhanVienID"),out var id)?id:null;
    private static string? Clean(string? value)=>string.IsNullOrWhiteSpace(value)?null:value.Trim();
}
