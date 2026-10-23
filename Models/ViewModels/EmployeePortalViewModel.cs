namespace QLNS.Models.ViewModels;

public class EmployeePortalViewModel
{
    public Entities.NhanVien Employee { get; set; } = null!;
    public IReadOnlyList<Entities.ChamCong> Attendance { get; set; } = [];
    public IReadOnlyList<Entities.NghiPhep> Leaves { get; set; } = [];
    public IReadOnlyList<Entities.Luong> Payrolls { get; set; } = [];
    public IReadOnlyList<Entities.SoDuNghiPhep> LeaveBalances { get; set; } = [];
    public IReadOnlyDictionary<int, decimal> UsedLeaveDaysByType { get; set; } = new Dictionary<int, decimal>();
    public IReadOnlyList<Entities.HopDong> Contracts { get; set; } = [];
    public IReadOnlyList<Entities.TrinhDoChungChi> Qualifications { get; set; } = [];
    public IReadOnlyList<Entities.PhuCap> Allowances { get; set; } = [];
    public IReadOnlyList<Entities.ChinhSachPhuCap> ScopedAllowances { get; set; } = [];
    public IReadOnlyList<Entities.BaoHiem> Insurances { get; set; } = [];
    public IReadOnlyList<Entities.ChinhSachBhxh> ScopedInsurances { get; set; } = [];
    public IReadOnlyList<Entities.DieuChinhChamCong> AttendanceAdjustments { get; set; } = [];
    public IReadOnlyList<Entities.DangKyTangCa> OvertimeRequests { get; set; } = [];
    public IReadOnlyList<Entities.CongTac> BusinessTrips { get; set; } = [];
    public Entities.ChamCong? CurrentAttendance { get; set; }
    public IReadOnlyList<Entities.NhanVien> Colleagues { get; set; } = [];
    public Entities.NhanVien? DirectManager { get; set; }
}

public class EmployeeMonthlyAttendanceViewModel
{
    public Entities.NhanVien Employee { get; set; } = null!;
    public int Month { get; set; }
    public int Year { get; set; }
    public IReadOnlyList<Entities.ChamCong> Records { get; set; } = [];
    public Entities.ChamCong? CurrentAttendance { get; set; }
    public Entities.Luong? Payroll { get; set; }
    public int WorkingDays => Records.Count(x =>
        (x.TrangThai is Entities.AttendanceStatuses.Present or Entities.AttendanceStatuses.Remote or Entities.AttendanceStatuses.BusinessTrip) &&
        x.Giovaolam.HasValue &&
        x.Giotanlam.HasValue);
    public int LeaveDays => Records.Count(x => x.TrangThai is Entities.AttendanceStatuses.PaidLeave or Entities.AttendanceStatuses.UnpaidLeave);
    public int AbsentDays => Records.Count(x => x.TrangThai == Entities.AttendanceStatuses.Absent);
    public int LateMinutes => Records.Sum(x => x.PhutDiMuon);
    public double TotalHours => Records.Sum(x => x.SoGioLam ?? 0);
}
