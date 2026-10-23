using Microsoft.EntityFrameworkCore;
using QLNS.Data;
using QLNS.Models.Entities;

namespace QLNS.Services;

public sealed class LeaveService(AppDbContext context)
{
    public async Task<string?> ValidateRequestAsync(int employeeId, int? leaveTypeId, DateTime from, DateTime to, bool isEmergency = false, int? excludeLeaveId = null)
    {
        if (!leaveTypeId.HasValue) return "Vui lòng chọn loại nghỉ.";
        var type=await context.LoaiNghiPheps.AsNoTracking().FirstOrDefaultAsync(x=>x.ID==leaveTypeId&&x.HoatDong);
        if(type is null)return "Loại nghỉ không tồn tại hoặc đã ngừng sử dụng.";
        if (!excludeLeaveId.HasValue && from.Date < DateTime.Today)
            return "Không thể tạo đơn nghỉ cho ngày đã qua.";
        var earliestNormalLeaveDate = DateTime.Today.AddDays(3);
        if (!excludeLeaveId.HasValue && !isEmergency && from.Date < earliestNormalLeaveDate)
            return $"Ngày bắt đầu phải từ {earliestNormalLeaveDate:dd/MM/yyyy} trở đi (báo trước đủ 3 ngày tính từ hôm nay {DateTime.Today:dd/MM/yyyy}). Nếu cần nghỉ sớm hơn, hãy chọn Nghỉ khẩn cấp.";
        var overlap=await context.NghiPheps.AnyAsync(n=>n.ID!=excludeLeaveId&&n.NhanVienID==employeeId&&
            (n.Trangthai==LeaveStatuses.Pending||n.Trangthai==ApprovalStatuses.ManagerApproved||n.Trangthai==LeaveStatuses.Approved)&&
            n.Ngaybatdau<=to&&n.NgayKetThuc>=from);
        if(overlap)return "Đã có đơn nghỉ chồng lấn thời gian.";
        if(type.TruQuyPhep&&from.Year!=to.Year)return "Đơn trừ quỹ phép phải nằm trong cùng một năm.";
        return null;
    }

    public async Task<LeavePayAllocation> CalculatePayAllocationAsync(
        int employeeId, int leaveTypeId, DateTime from, DateTime to, int? excludeLeaveId = null)
    {
        var type = await context.LoaiNghiPheps.AsNoTracking().SingleAsync(x => x.ID == leaveTypeId);
        var requested = BusinessDays(from, to);
        if (!type.HuongLuong) return new(0, requested);
        if (!type.TruQuyPhep) return new(requested, 0);

        var balance = await context.SoDuNghiPheps.AsNoTracking().FirstOrDefaultAsync(x =>
            x.NhanVienID == employeeId && x.LoaiNghiPhepID == leaveTypeId && x.Nam == from.Year);
        var positionQuota = await context.NhanViens.AsNoTracking().Where(x=>x.ID==employeeId)
            .Select(x=>x.ChucVu==null?(decimal?)null:x.ChucVu.SoNgayPhepNam).SingleOrDefaultAsync()??16m;
        var usedLeaves = await context.NghiPheps.AsNoTracking().Where(x =>
            x.ID != excludeLeaveId && x.NhanVienID == employeeId && x.LoaiNghiPhepID == leaveTypeId &&
            x.Trangthai == LeaveStatuses.Approved && x.Ngaybatdau.Year == from.Year).ToListAsync();
        var used = usedLeaves.Sum(x => x.SoNgayHuongLuong ?? BusinessDays(x.Ngaybatdau, x.NgayKetThuc));
        var available = Math.Max(0, (int)Math.Floor(positionQuota +
            (balance?.DieuChinh ?? 0) - used));
        var paid = Math.Min(requested, available);
        return new(paid, requested - paid);
    }

    public async Task<LeaveApprovalRouteResult> GetApprovalRouteAsync(int employeeId)
    {
        var employee = await context.NhanViens.AsNoTracking()
            .Where(x => x.ID == employeeId)
            .Select(x => new
            {
                x.PhongBanID,
                PositionRole = x.ChucVu == null ? null : x.ChucVu.PhanQuyen
            }).SingleOrDefaultAsync();
        if (employee is null) return LeaveApprovalRouteResult.Invalid("Không tìm thấy nhân viên.");

        var hrDepartment = await context.PhongBans.AsNoTracking()
            .Where(x => x.Matruongphong.HasValue && x.NhanViens.Any(n =>
                n.ChucVu != null && ("," + n.ChucVu.PhanQuyen + ",").Contains(",HR,")))
            .OrderBy(x => x.ID)
            .Select(x => new { x.ID, ManagerId = x.Matruongphong!.Value })
            .FirstOrDefaultAsync();
        if (hrDepartment is null)
            return LeaveApprovalRouteResult.Invalid("Chưa cấu hình Trưởng phòng HR để duyệt nghỉ phép.");

        if (hrDepartment.ManagerId == employeeId)
            return new(LeaveApprovalRoute.AutoApprove, null, hrDepartment.ManagerId, null);

        var roles = AuthorizationRoles.Parse(employee.PositionRole);
        if (roles.Contains(AuthorizationRoles.Manager) || roles.Contains(AuthorizationRoles.HumanResources))
            return new(LeaveApprovalRoute.HrManagerDirect, null, hrDepartment.ManagerId, null);

        var departmentManagerId = employee.PhongBanID.HasValue
            ? await context.PhongBans.AsNoTracking().Where(x => x.ID == employee.PhongBanID.Value)
                .Select(x => x.Matruongphong).SingleOrDefaultAsync()
            : null;
        if (!departmentManagerId.HasValue)
            return LeaveApprovalRouteResult.Invalid("Phòng ban của nhân viên chưa có Trưởng phòng để duyệt đơn.");
        return new(LeaveApprovalRoute.DepartmentManagerThenHr, departmentManagerId, hrDepartment.ManagerId, null);
    }

    public async Task ApplyApprovedAttendanceAsync(NghiPhep leave)
    {
        var businessDates = BusinessDateRange(leave.Ngaybatdau, leave.NgayKetThuc).ToList();
        if (businessDates.Count == 0) return;
        var paidDays = Math.Clamp(leave.SoNgayHuongLuong ?? businessDates.Count, 0, businessDates.Count);
        var firstDate = businessDates[0];
        var lastDate = businessDates[businessDates.Count - 1];
        var existingByDate = (await context.ChamCongs.Where(x =>
                x.NhanVienID == leave.NhanVienID && x.Ngay >= firstDate && x.Ngay <= lastDate)
            .ToListAsync()).ToDictionary(x => x.Ngay.Date);
        for (var index = 0; index < businessDates.Count; index++)
        {
            var date = businessDates[index];
            var status = index < paidDays ? AttendanceStatuses.PaidLeave : AttendanceStatuses.UnpaidLeave;
            if (!existingByDate.TryGetValue(date, out var attendance))
            {
                context.ChamCongs.Add(new ChamCong
                {
                    NhanVienID = leave.NhanVienID,
                    Ngay = date,
                    TrangThai = status,
                    GhiChu = $"Nghỉ phép: {leave.Lydo}"
                });
                continue;
            }
            if (attendance.Giovaolam.HasValue || attendance.Giotanlam.HasValue ||
                attendance.TrangThai is AttendanceStatuses.Present or AttendanceStatuses.Remote or AttendanceStatuses.BusinessTrip)
                continue;
            attendance.TrangThai = status;
            attendance.GhiChu = $"Nghỉ phép: {leave.Lydo}";
        }
    }

    public static int BusinessDays(DateTime from,DateTime to)
    {
        var count=0;for(var d=from.Date;d<=to.Date;d=d.AddDays(1))if(d.DayOfWeek is not(DayOfWeek.Saturday or DayOfWeek.Sunday))count++;return count;
    }

    private static IEnumerable<DateTime> BusinessDateRange(DateTime from, DateTime to)
    {
        for (var date = from.Date; date <= to.Date; date = date.AddDays(1))
            if (date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)) yield return date;
    }
}

public enum LeaveApprovalRoute { DepartmentManagerThenHr, HrManagerDirect, AutoApprove }

public sealed record LeaveApprovalRouteResult(
    LeaveApprovalRoute Route, int? DepartmentManagerId, int? HrManagerId, string? Error)
{
    public static LeaveApprovalRouteResult Invalid(string error) =>
        new(LeaveApprovalRoute.DepartmentManagerThenHr, null, null, error);
}

public sealed record LeavePayAllocation(int PaidDays, int UnpaidDays);
