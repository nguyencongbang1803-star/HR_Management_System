using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.Data;
using QLNS.Models.Entities;
using QLNS.Models.ViewModels;
using QLNS.Services;
using System.Globalization;

namespace QLNS.Areas.HR.Controllers;

[Area("HR")]
[Authorize(Roles = "Admin,HR,QuanLy")]
public class ChamCongController : Controller
{
    private readonly AppDbContext _context; private readonly AuditService _audit;
    public ChamCongController(AppDbContext context, AuditService audit) { _context=context; _audit=audit; }

    public async Task<IActionResult> Index(
        int thang = 0,
        int nam = 0,
        DateTime? ngay = null,
        string? trangThai = null,
        string? employeeSearch = null,
        bool incompleteOnly = false)
    {
        thang = thang is >= 1 and <= 12 ? thang : DateTime.Today.Month;
        nam = nam is >= 2000 and <= 2200 ? nam : DateTime.Today.Year;
        ngay = ngay?.Date;
        if (incompleteOnly && string.IsNullOrWhiteSpace(trangThai)) trangThai = "Incomplete";

        var query = _context.ChamCongs.AsNoTracking().Include(c => c.NhanVien).AsQueryable();
        query = ngay.HasValue
            ? query.Where(c => c.Ngay == ngay.Value)
            : query.Where(c => c.Ngay.Month == thang && c.Ngay.Year == nam);
        if (IsManagerOnly())
        {
            var managerId = int.TryParse(User.FindFirst("NhanVienID")?.Value, out var id) ? id : 0;
            var departmentId = await _context.NhanViens.Where(x => x.ID == managerId).Select(x => x.PhongBanID).FirstOrDefaultAsync();
            query = query.Where(x => departmentId.HasValue && x.NhanVien != null && x.NhanVien.PhongBanID == departmentId);
        }
        if (!string.IsNullOrWhiteSpace(employeeSearch))
        {
            var term = employeeSearch.Trim();
            query = query.Where(c => c.NhanVien != null &&
                (c.NhanVien.MaNhanVien.Contains(term) || c.NhanVien.Ten.Contains(term)));
        }
        query = trangThai switch
        {
            "OnTime" => query.Where(c =>
                c.TrangThai == AttendanceStatuses.Present && c.PhutDiMuon == 0 &&
                c.Giovaolam.HasValue && c.Giotanlam.HasValue),
            "Late" => query.Where(c => c.PhutDiMuon > 0),
            "Incomplete" => query.Where(c =>
                (c.TrangThai == AttendanceStatuses.Present ||
                 c.TrangThai == AttendanceStatuses.Remote ||
                 c.TrangThai == AttendanceStatuses.BusinessTrip) &&
                (!c.Giovaolam.HasValue || !c.Giotanlam.HasValue)),
            AttendanceStatuses.PaidLeave => query.Where(c => c.TrangThai == AttendanceStatuses.PaidLeave),
            AttendanceStatuses.UnpaidLeave => query.Where(c => c.TrangThai == AttendanceStatuses.UnpaidLeave),
            AttendanceStatuses.Absent => query.Where(c => c.TrangThai == AttendanceStatuses.Absent),
            AttendanceStatuses.Remote => query.Where(c => c.TrangThai == AttendanceStatuses.Remote),
            _ => query
        };
        ViewBag.Thang = thang;
        ViewBag.Nam = nam;
        ViewBag.Ngay = ngay?.ToString("yyyy-MM-dd");
        ViewBag.TrangThai = trangThai;
        ViewBag.EmployeeSearch = employeeSearch;
        ViewBag.IncompleteOnly = trangThai == "Incomplete";
        return View(await query.OrderByDescending(c => c.Ngay).ThenBy(c => c.NhanVien!.Ten).ToListAsync());
    }

    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> Create(DateTime? date = null)
    {
        await LoadEmployeesAsync(); return View(new AttendanceFormViewModel { Ngay = date?.Date ?? DateTime.Today });
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPost]
    public async Task<IActionResult> Create(AttendanceFormViewModel model)
    {
        await ValidateAsync(model);
        if (!ModelState.IsValid) { await LoadEmployeesAsync(); return View(model); }
        var item = new ChamCong(); Map(model,item);await ApplyShiftMetricsAsync(item); _context.ChamCongs.Add(item); await _context.SaveChangesAsync();
        _audit.Add("Create","ChamCong",item.ID.ToString(),$"{item.NhanVienID} - {item.Ngay:yyyy-MM-dd}"); await _context.SaveChangesAsync();
        TempData["Success"]="Đã ghi nhận chấm công."; return RedirectToAction(nameof(Index),new { thang=item.Ngay.Month, nam=item.Ngay.Year });
    }

    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> Edit(int id)
    {
        var item=await _context.ChamCongs.FindAsync(id); if(item is null) return NotFound();
        if(await IsLockedAsync(item.Ngay)) { TempData["Error"]="Kỳ lương đã khóa, không thể sửa chấm công."; return RedirectToAction(nameof(Index)); }
        await LoadEmployeesAsync(); return View("Create", ToForm(item));
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPost]
    public async Task<IActionResult> Edit(int id, AttendanceFormViewModel model)
    {
        if(id!=model.ID) return BadRequest(); var item=await _context.ChamCongs.FindAsync(id); if(item is null) return NotFound();
        if(await IsLockedAsync(item.Ngay)) { TempData["Error"]="Kỳ lương đã khóa, không thể sửa chấm công."; return RedirectToAction(nameof(Index)); }
        await ValidateAsync(model,id); if(!ModelState.IsValid){await LoadEmployeesAsync(); return View("Create",model);}
        Map(model,item);await ApplyShiftMetricsAsync(item); _audit.Add("Update","ChamCong",id.ToString()); await _context.SaveChangesAsync(); TempData["Success"]="Đã cập nhật chấm công.";
        return RedirectToAction(nameof(Index),new{thang=item.Ngay.Month,nam=item.Ngay.Year});
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item=await _context.ChamCongs.FindAsync(id); if(item is null) return NotFound();
        if(await IsLockedAsync(item.Ngay)){TempData["Error"]="Kỳ lương đã khóa, không thể xóa chấm công.";return RedirectToAction(nameof(Index));}
        var month=item.Ngay.Month;var year=item.Ngay.Year;_context.ChamCongs.Remove(item);_audit.Add("Delete","ChamCong",id.ToString());await _context.SaveChangesAsync();TempData["Success"]="Đã xóa chấm công.";
        return RedirectToAction(nameof(Index),new{thang=month,nam=year});
    }

    [Authorize(Roles="Admin,HR")]
    [HttpPost]
    [RequestSizeLimit(2*1024*1024)]
    public async Task<IActionResult> ImportCsv(IFormFile? file)
    {
        var extension=file is null?string.Empty:Path.GetExtension(file.FileName);
        if(file is null||file.Length==0||file.Length>2*1024*1024||!(extension.Equals(".csv",StringComparison.OrdinalIgnoreCase)||extension.Equals(".txt",StringComparison.OrdinalIgnoreCase))){TempData["Error"]="Chọn file CSV hoặc TXT dung lượng không quá 2 MB.";return RedirectToAction(nameof(Index));}
        var employees=await _context.NhanViens.ToDictionaryAsync(x=>x.MaNhanVien,StringComparer.OrdinalIgnoreCase);
        var errors=new List<string>();var imported=0;var lineNumber=0;
        await using var transaction=await _context.Database.BeginTransactionAsync();
        using var reader=new StreamReader(file.OpenReadStream());
        while(await reader.ReadLineAsync() is { } line)
        {
            lineNumber++;if(string.IsNullOrWhiteSpace(line))continue;var values=ParseCsv(line);
            if(lineNumber==1&&values.Count>0&&values[0].Equals("MaNhanVien",StringComparison.OrdinalIgnoreCase))continue;
            if(values.Count<4){errors.Add($"Dòng {lineNumber}: thiếu cột.");continue;}
            if(!employees.TryGetValue(values[0].Trim(),out var employee)){errors.Add($"Dòng {lineNumber}: mã nhân viên không tồn tại.");continue;}
            if(!DateTime.TryParseExact(values[1].Trim(),"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var date)){errors.Add($"Dòng {lineNumber}: ngày phải theo yyyy-MM-dd.");continue;}
            if(await IsLockedAsync(date)){errors.Add($"Dòng {lineNumber}: kỳ lương đã khóa.");continue;}
            if(!TimeSpan.TryParseExact(values[2].Trim(),@"hh\:mm",CultureInfo.InvariantCulture,out var timeIn)||!TimeSpan.TryParseExact(values[3].Trim(),@"hh\:mm",CultureInfo.InvariantCulture,out var timeOut)||timeOut<=timeIn){errors.Add($"Dòng {lineNumber}: giờ vào/ra không hợp lệ.");continue;}
            var status=values.Count>4&&!string.IsNullOrWhiteSpace(values[4])?values[4].Trim():AttendanceStatuses.Present;
            if(status is not (AttendanceStatuses.Present or AttendanceStatuses.Remote or AttendanceStatuses.BusinessTrip)){errors.Add($"Dòng {lineNumber}: trạng thái không hợp lệ.");continue;}
            var item=await _context.ChamCongs.FirstOrDefaultAsync(x=>x.NhanVienID==employee.ID&&x.Ngay==date.Date);
            if(item is null){item=new ChamCong{NhanVienID=employee.ID,Ngay=date.Date};_context.Add(item);}
            item.Giovaolam=date.Date+timeIn;item.Giotanlam=date.Date+timeOut;item.TrangThai=status;item.GhiChu=null;await ApplyShiftMetricsAsync(item);imported++;
        }
        if(errors.Count>0){await transaction.RollbackAsync();TempData["Error"]=$"Không nhập dữ liệu vì có {errors.Count} lỗi: {string.Join(" | ",errors.Take(5))}";return RedirectToAction(nameof(Index));}
        _audit.Add("Import","ChamCong",file.FileName,$"{imported} dòng");await _context.SaveChangesAsync();await transaction.CommitAsync();TempData["Success"]=$"Đã nhập/cập nhật {imported} dòng chấm công.";return RedirectToAction(nameof(Index));
    }

    private async Task ValidateAsync(AttendanceFormViewModel model,int? exceptId=null)
    {
        if(await _context.ChamCongs.AnyAsync(c=>c.NhanVienID==model.NhanVienID&&c.Ngay==model.Ngay.Date&&c.ID!=exceptId)) ModelState.AddModelError(nameof(model.Ngay),"Nhân viên đã có chấm công trong ngày này.");
        if(await IsLockedAsync(model.Ngay)) ModelState.AddModelError(string.Empty,"Kỳ lương của tháng này đã khóa.");
    }
    private Task<bool> IsLockedAsync(DateTime date)=>_context.Luongs.AnyAsync(l=>l.Thang==date.Month&&l.Nam==date.Year&&(l.TrangThai==PayrollStatuses.Locked||l.TrangThai==PayrollStatuses.Paid));
    private bool IsManagerOnly()=>User.IsInRole(AuthorizationRoles.Manager)&&!User.IsInRole(AuthorizationRoles.HumanResources)&&!User.IsInRole(AuthorizationRoles.Administrator);
    private async Task LoadEmployeesAsync()=>ViewBag.NhanViens=await _context.NhanViens.AsNoTracking().Where(n=>n.Trangthai).OrderBy(n=>n.Ten).ToListAsync();
    private static void Map(AttendanceFormViewModel s,ChamCong t){t.NhanVienID=s.NhanVienID;t.Ngay=s.Ngay.Date;t.Giovaolam=s.GioVao.HasValue?s.Ngay.Date+s.GioVao.Value:null;t.Giotanlam=s.GioRa.HasValue?s.Ngay.Date+s.GioRa.Value:null;t.TrangThai=s.TrangThai;t.GhiChu=s.GhiChu?.Trim();}
    private static AttendanceFormViewModel ToForm(ChamCong c)=>new(){ID=c.ID,NhanVienID=c.NhanVienID,Ngay=c.Ngay,GioVao=c.Giovaolam?.TimeOfDay,GioRa=c.Giotanlam?.TimeOfDay,TrangThai=c.TrangThai,GhiChu=c.GhiChu};
    private static List<string> ParseCsv(string line){var result=new List<string>();var current=new System.Text.StringBuilder();var quoted=false;for(var i=0;i<line.Length;i++){var c=line[i];if(c=='"'){if(quoted&&i+1<line.Length&&line[i+1]=='"'){current.Append('"');i++;}else quoted=!quoted;}else if((c==','||c==';')&&!quoted){result.Add(current.ToString());current.Clear();}else current.Append(c);}result.Add(current.ToString());return result;}
    private async Task ApplyShiftMetricsAsync(ChamCong item){AttendancePolicy.ApplyLateRule(item);item.PhutVeSom=0;if(item.TrangThai is not(AttendanceStatuses.Present or AttendanceStatuses.Remote)||!item.Giotanlam.HasValue)return;var assignment=await _context.PhanCaNhanViens.AsNoTracking().Include(x=>x.CaLamViec).Where(x=>x.NhanVienID==item.NhanVienID&&x.TuNgay<=item.Ngay&&(x.DenNgay==null||x.DenNgay>=item.Ngay)).OrderByDescending(x=>x.TuNgay).FirstOrDefaultAsync();var shift=assignment?.CaLamViec;if(shift is null)return;var expectedOut=item.Ngay.Date+shift.GioKetThuc;if(shift.GioKetThuc<=shift.GioBatDau)expectedOut=expectedOut.AddDays(1);item.PhutVeSom=Math.Max(0,(int)Math.Ceiling((expectedOut-item.Giotanlam.Value).TotalMinutes));}
}
