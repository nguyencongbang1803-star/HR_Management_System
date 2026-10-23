using Microsoft.EntityFrameworkCore;
using QLNS.Data;
using QLNS.Models.Entities;

namespace QLNS.Services;

public sealed class PayrollCalculationService(AppDbContext context)
{
    public const int StandardWorkingDays = 22;
    public const decimal HoursPerDay = 8m;

    public async Task<PayrollReadinessReport> GetReadinessAsync(
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(month, year);
        var from = new DateTime(year, month, 1);
        var to = from.AddMonths(1).AddTicks(-1);

        var pendingLeaves = await context.NghiPheps.CountAsync(
            x => (x.Trangthai == LeaveStatuses.Pending || x.Trangthai == ApprovalStatuses.ManagerApproved) &&
                 x.Ngaybatdau <= to && x.NgayKetThuc >= from,
            cancellationToken);
        var pendingOvertime = await context.DangKyTangCas.CountAsync(
            x => (x.TrangThai == ApprovalStatuses.Pending ||
                  x.TrangThai == ApprovalStatuses.ManagerApproved) &&
                 x.Ngay >= from && x.Ngay <= to,
            cancellationToken);
        var pendingAdjustments = await context.DieuChinhChamCongs.CountAsync(
            x => x.TrangThai == ApprovalStatuses.Pending &&
                 x.Ngay >= from && x.Ngay <= to,
            cancellationToken);
        var incompleteAttendance = await context.ChamCongs.CountAsync(
            x => x.Ngay >= from && x.Ngay <= to &&
                 (x.TrangThai == AttendanceStatuses.Present ||
                  x.TrangThai == AttendanceStatuses.Remote ||
                  x.TrangThai == AttendanceStatuses.BusinessTrip) &&
                 (!x.Giovaolam.HasValue || !x.Giotanlam.HasValue),
            cancellationToken);

        var eligibleEmployeeIds = await context.NhanViens
            .Where(x => x.Ngayvaolam <= to && (!x.Ngayxoa.HasValue || x.Ngayxoa >= from))
            .Select(x => x.ID)
            .ToListAsync(cancellationToken);
        var employeesWithContracts = await context.HopDongs
            .Where(x => eligibleEmployeeIds.Contains(x.NhanVienID) &&
                        x.Ngaybatdau <= to &&
                        (!x.Ngayketthuc.HasValue || x.Ngayketthuc >= from))
            .Select(x => x.NhanVienID)
            .Distinct()
            .ToListAsync(cancellationToken);

        return new PayrollReadinessReport(
            pendingLeaves,
            pendingOvertime,
            pendingAdjustments,
            incompleteAttendance,
            eligibleEmployeeIds.Except(employeesWithContracts).Count());
    }

    public async Task<PayrollCalculationResult> CalculateAsync(
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(month, year);
        if (await context.Luongs.AnyAsync(
                x => x.Thang == month && x.Nam == year && x.TrangThai != PayrollStatuses.Draft,
                cancellationToken))
            return PayrollCalculationResult.Blocked("Kỳ lương đã phát hành, khóa hoặc thanh toán.");

        var readiness = await GetReadinessAsync(month, year, cancellationToken);
        if (!readiness.CanCalculate)
            return PayrollCalculationResult.Blocked(readiness.BlockingMessage);

        var from = new DateTime(year, month, 1);
        var to = from.AddMonths(1).AddTicks(-1);
        var employees = await context.NhanViens
            .Include(x => x.ChucVu)
            .Where(x => x.Ngayvaolam <= to && (!x.Ngayxoa.HasValue || x.Ngayxoa >= from))
            .OrderBy(x => x.Ten)
            .ToListAsync(cancellationToken);
        var employeeIds = employees.Select(x => x.ID).ToList();

        var contracts = await context.HopDongs
            .Where(x => employeeIds.Contains(x.NhanVienID) &&
                        x.Ngaybatdau <= to &&
                        (!x.Ngayketthuc.HasValue || x.Ngayketthuc >= from))
            .OrderByDescending(x => x.Ngaybatdau)
            .ToListAsync(cancellationToken);
        var attendance = await context.ChamCongs
            .Where(x => employeeIds.Contains(x.NhanVienID) && x.Ngay >= from && x.Ngay <= to)
            .ToListAsync(cancellationToken);
        var leaves = await context.NghiPheps
            .Include(x => x.LoaiNghiPhep)
            .Where(x => employeeIds.Contains(x.NhanVienID) &&
                        x.Trangthai == LeaveStatuses.Approved &&
                        x.Ngaybatdau <= to && x.NgayKetThuc >= from)
            .ToListAsync(cancellationToken);
        var allowances = await context.PhuCaps
            .Where(x => employeeIds.Contains(x.NhanVienID) && x.NgayApDung <= to)
            .ToListAsync(cancellationToken);
        var allowancePolicies = await context.ChinhSachPhuCaps
            .Where(x => x.HoatDong && x.NgayBatDau <= to)
            .ToListAsync(cancellationToken);
        var individualInsurances = await context.BaoHiems
            .Where(x => employeeIds.Contains(x.NhanVienID) &&
                        x.TrangThai == "DangDong" &&
                        x.NgayBatDau <= to &&
                        (!x.NgayKetThuc.HasValue || x.NgayKetThuc >= from))
            .ToListAsync(cancellationToken);
        var insurancePolicies = await context.ChinhSachBhxhs
            .Where(x => x.HoatDong && x.NgayBatDau <= to)
            .ToListAsync(cancellationToken);
        var adjustments = await context.ThuongPhats
            .Where(x => employeeIds.Contains(x.NhanVienID) &&
                        x.NgayApDung >= from && x.NgayApDung <= to)
            .ToListAsync(cancellationToken);
        var overtime = await context.DangKyTangCas
            .Where(x => employeeIds.Contains(x.NhanVienID) &&
                        x.TrangThai == ApprovalStatuses.Approved &&
                        x.Ngay >= from && x.Ngay <= to)
            .ToListAsync(cancellationToken);
        var existing = await context.Luongs
            .Where(x => x.Thang == month && x.Nam == year)
            .ToDictionaryAsync(x => x.NhanVienID, cancellationToken);

        var warnings = new List<string>();
        var calculatedEmployeeIds = new HashSet<int>();
        var created = 0;
        var updated = 0;

        foreach (var employee in employees)
        {
            var contract = contracts.FirstOrDefault(x => x.NhanVienID == employee.ID);
            if (contract is null)
            {
                warnings.Add($"{employee.MaNhanVien} - {employee.Ten}: không có hợp đồng trong kỳ.");
                continue;
            }

            var employeeAttendance = attendance
                .Where(x => x.NhanVienID == employee.ID &&
                            x.TrangThai is AttendanceStatuses.Present
                                or AttendanceStatuses.Remote
                                or AttendanceStatuses.BusinessTrip)
                .ToList();
            var workDates = employeeAttendance.Select(x => x.Ngay.Date).ToHashSet();
            var workedDays = employeeAttendance.Sum(PayrollFormula.AttendanceDayValue);
            var paidLeaveDates = leaves
                .Where(x => x.NhanVienID == employee.ID && x.LoaiNghiPhep?.HuongLuong == true)
                .SelectMany(x => PaidBusinessDays(x, from, to))
                .ToHashSet();
            paidLeaveDates.ExceptWith(workDates);

            if (employeeAttendance.Count == 0 && paidLeaveDates.Count == 0)
            {
                warnings.Add($"{employee.MaNhanVien} - {employee.Ten}: chưa có dữ liệu công hoặc phép có lương.");
                continue;
            }

            var paidDays = Math.Min(StandardWorkingDays, workedDays + paidLeaveDates.Count);
            var allowance = allowances.Where(x => x.NhanVienID == employee.ID).Sum(x => x.SoTien) +
                allowancePolicies.Where(x => PayrollFormula.IsAllowanceApplicable(x, employee, from)).Sum(x => x.SoTien);
            var insurance = individualInsurances.Where(x => x.NhanVienID == employee.ID).Sum(x => x.SoTienDong) +
                insurancePolicies.Where(x => PayrollFormula.IsSocialInsuranceApplicable(x, employee, from)).Sum(x => x.SoTienDong);
            var bonus = SumAdjustment(adjustments, employee.ID, "Thuong");
            var penalty = SumAdjustment(adjustments, employee.ID, "Phat");
            var otherIncome = SumAdjustment(adjustments, employee.ID, "ThuNhapKhac");
            var salaryCoefficient = employee.ChucVu?.Hesoluong ?? 1m;
            var salaryBasis = PayrollFormula.RoundMoney(contract.Luongcoban * salaryCoefficient);
            var employeeOvertime = overtime.Where(x => x.NhanVienID == employee.ID).ToList();
            var overtimeHours = employeeOvertime.Sum(x => x.SoGioDuyet);
            var hourlySalary = salaryBasis / (StandardWorkingDays * HoursPerDay);
            penalty += PayrollFormula.RoundMoney(employeeAttendance.Sum(x => x.PhutDiMuon) / 60m * hourlySalary);
            var overtimePay = PayrollFormula.RoundMoney(employeeOvertime.Sum(
                x => x.SoGioDuyet * hourlySalary * x.HeSo));
            var amounts = PayrollFormula.Calculate(
                contract.Luongcoban,
                salaryCoefficient,
                paidDays,
                allowance,
                bonus,
                otherIncome,
                overtimePay,
                penalty,
                insurance);

            if (!existing.TryGetValue(employee.ID, out var payroll))
            {
                payroll = new Luong { NhanVienID = employee.ID, Thang = month, Nam = year };
                context.Luongs.Add(payroll);
                created++;
            }
            else
            {
                updated++;
            }

            payroll.Ten = $"Lương tháng {month:00}/{year}";
            payroll.Luongcoban = contract.Luongcoban;
            payroll.HeSoLuong = salaryCoefficient;
            payroll.SoNgayCong = paidDays;
            payroll.SoNgayChuan = StandardWorkingDays;
            payroll.Phucap = allowance;
            payroll.Thuong = bonus;
            payroll.ThuNhapKhac = otherIncome;
            payroll.SoGioTangCa = overtimeHours;
            payroll.TienTangCa = overtimePay;
            payroll.Khautru = penalty;
            payroll.BaoHiem = amounts.Insurance;
            // Tạm thời chưa áp dụng thuế TNCN. Giữ cột dữ liệu để có thể bật lại sau.
            payroll.ThueTNCN = 0m;
            payroll.Tongluong = amounts.NetSalary;
            payroll.TrangThai = PayrollStatuses.Draft;
            payroll.NgayTinh = DateTime.UtcNow;
            calculatedEmployeeIds.Add(employee.ID);
        }

        var staleDrafts = existing.Values
            .Where(x => x.TrangThai == PayrollStatuses.Draft &&
                        !calculatedEmployeeIds.Contains(x.NhanVienID))
            .ToList();
        context.Luongs.RemoveRange(staleDrafts);
        await context.SaveChangesAsync(cancellationToken);

        return new PayrollCalculationResult(
            false,
            created,
            updated,
            staleDrafts.Count,
            warnings,
            null);
    }

    private static decimal SumAdjustment(
        IEnumerable<ThuongPhat> adjustments,
        int employeeId,
        string type) =>
        adjustments.Where(x => x.NhanVienID == employeeId && x.Loai == type).Sum(x => x.SoTien);

    private static IEnumerable<DateTime> BusinessDays(DateTime start, DateTime end)
    {
        for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
            if (date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
                yield return date;
    }

    private static IEnumerable<DateTime> PaidBusinessDays(NghiPhep leave, DateTime periodStart, DateTime periodEnd)
    {
        var paidDayLimit = leave.SoNgayHuongLuong ?? int.MaxValue;
        return BusinessDays(leave.Ngaybatdau, leave.NgayKetThuc)
            .Take(paidDayLimit)
            .Where(x => x >= periodStart.Date && x <= periodEnd.Date);
    }

    private static void ValidatePeriod(int month, int year)
    {
        if (month is < 1 or > 12 || year is < 2000 or > 2200)
            throw new ArgumentOutOfRangeException(nameof(month), "Kỳ lương không hợp lệ.");
    }
}

public static class PayrollFormula
{
    public static bool IsAllowanceApplicable(ChinhSachPhuCap policy, NhanVien employee, DateTime periodStart)
    {
        var month = new DateTime(periodStart.Year, periodStart.Month, 1);
        var startMonth = new DateTime(policy.NgayBatDau.Year, policy.NgayBatDau.Month, 1);
        if (!policy.HoatDong || month < startMonth) return false;
        if (policy.KieuThoiHan == AllowanceDurations.FixedMonths &&
            month >= startMonth.AddMonths(policy.SoThang ?? 0)) return false;
        return policy.PhamVi switch
        {
            AllowanceScopes.Company => true,
            AllowanceScopes.Department => policy.PhongBanID == employee.PhongBanID,
            AllowanceScopes.Position => policy.ChucVuID == employee.ChucVuID,
            AllowanceScopes.Employee => policy.NhanVienID == employee.ID,
            _ => false
        };
    }

    public static bool IsSocialInsuranceApplicable(ChinhSachBhxh policy, NhanVien employee, DateTime periodStart)
    {
        var month = new DateTime(periodStart.Year, periodStart.Month, 1);
        var startMonth = new DateTime(policy.NgayBatDau.Year, policy.NgayBatDau.Month, 1);
        if (!policy.HoatDong || month < startMonth) return false;
        if (policy.KieuThoiHan == AllowanceDurations.FixedMonths &&
            month >= startMonth.AddMonths(policy.SoThang ?? 0)) return false;
        return policy.PhamVi switch
        {
            AllowanceScopes.Company => true,
            AllowanceScopes.Department => policy.PhongBanID == employee.PhongBanID,
            AllowanceScopes.Position => policy.ChucVuID == employee.ChucVuID,
            AllowanceScopes.Employee => policy.NhanVienID == employee.ID,
            _ => false
        };
    }

    public static PayrollAmounts Calculate(
        decimal baseSalary,
        decimal salaryCoefficient,
        decimal paidDays,
        decimal allowance,
        decimal bonus,
        decimal otherIncome,
        decimal overtimePay,
        decimal penalty,
        decimal insurance)
    {
        if (baseSalary < 0 || salaryCoefficient < 0 || insurance < 0 || paidDays < 0 || paidDays > PayrollCalculationService.StandardWorkingDays)
            throw new ArgumentOutOfRangeException(nameof(paidDays), "Dữ liệu lương hoặc ngày công không hợp lệ.");

        var salaryBasis = RoundMoney(baseSalary * salaryCoefficient);
        var salaryByAttendance = RoundMoney(
            salaryBasis / PayrollCalculationService.StandardWorkingDays * paidDays);
        insurance = RoundMoney(insurance);
        var grossIncome = RoundMoney(
            salaryByAttendance + allowance + bonus + otherIncome + overtimePay);
        var totalDeductions = RoundMoney(penalty + insurance);
        var netSalary = RoundMoney(Math.Max(0, grossIncome - totalDeductions));
        return new PayrollAmounts(salaryByAttendance, insurance, grossIncome, totalDeductions, netSalary);
    }

    public static decimal AttendanceDayValue(ChamCong item)
    {
        if (!item.Giovaolam.HasValue || !item.Giotanlam.HasValue) return 0m;
        var hours = (decimal)Math.Max(0, (item.Giotanlam.Value - item.Giovaolam.Value).TotalHours);
        if (hours > 4m) hours = Math.Max(0, hours - 1m);
        return Math.Round(
            Math.Min(1m, hours / PayrollCalculationService.HoursPerDay),
            2,
            MidpointRounding.AwayFromZero);
    }

    public static decimal RoundMoney(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

public sealed record PayrollAmounts(
    decimal SalaryByAttendance,
    decimal Insurance,
    decimal GrossIncome,
    decimal TotalDeductions,
    decimal NetSalary);

public sealed record PayrollReadinessReport(
    int PendingLeaves,
    int PendingOvertime,
    int PendingAttendanceAdjustments,
    int IncompleteAttendance,
    int EmployeesWithoutContract)
{
    public bool CanCalculate =>
        PendingLeaves == 0 &&
        PendingOvertime == 0 &&
        PendingAttendanceAdjustments == 0;

    public string BlockingMessage =>
        $"Chưa thể tính tự động: {PendingLeaves} đơn nghỉ, {PendingOvertime} đề nghị OT, " +
        $"{PendingAttendanceAdjustments} yêu cầu sửa công đang chờ. " +
        $"Có {IncompleteAttendance} bản ghi thiếu giờ vào/ra và các ngày này sẽ không được tính công.";
}

public sealed record PayrollCalculationResult(
    bool IsBlocked,
    int Created,
    int Updated,
    int Removed,
    IReadOnlyList<string> Warnings,
    string? Error)
{
    public int Calculated => Created + Updated;

    public static PayrollCalculationResult Blocked(string message) =>
        new(true, 0, 0, 0, [], message);
}
