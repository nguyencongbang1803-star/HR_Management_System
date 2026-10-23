using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QLNS.Models.Entities;
using QLNS.Services;

namespace QLNS.Data;

/// <summary>
/// Chuẩn hóa bộ dữ liệu kỹ thuật thành dữ liệu dùng để trình diễn sản phẩm:
/// tên gọi tự nhiên, không ghi công ở tương lai và mọi nhân viên có lịch sử lương/công nhất quán.
/// </summary>
public static class CompanyPresentationSeeder
{
    private const string Marker = "COMPANY-PRESENTATION-2026-V2";
    private const int Year = 2026;

    public static async Task SeedAsync(AppDbContext db, UserManager<TaiKhoan> users, ILogger logger)
    {
        if (await db.NhatKyHeThongs.AnyAsync(x => x.DoiTuongID == Marker)) return;

        await NormalizeExistingEmployeeAsync(db, users);
        await NormalizeEmployeeCodesAsync(db, users);
        await NormalizeVisibleContentAsync(db);
        await NormalizeRequestsAsync(db);
        await NormalizeAugustAttendanceAsync(db);
        await EnsureEveryEmployeePayrollAsync(db);
        await EnsureEveryEmployeeSupportingDataAsync(db);

        var activeEmployees = await db.NhanViens.Where(x => x.Trangthai).ToListAsync();
        foreach (var employee in activeEmployees)
        {
            var payrollCount = await db.Luongs.CountAsync(x => x.NhanVienID == employee.ID && x.Nam == Year && new[] { 5, 6, 7 }.Contains(x.Thang));
            if (payrollCount != 3)
                throw new InvalidOperationException($"{employee.MaNhanVien} chưa đủ bảng lương tháng 05-07/2026.");
            var attendanceCount = await db.ChamCongs.CountAsync(x => x.NhanVienID == employee.ID && x.Ngay >= new DateTime(Year, 8, 1) && x.Ngay <= new DateTime(Year, 8, 7));
            if (attendanceCount < 5)
                throw new InvalidOperationException($"{employee.MaNhanVien} chưa đủ dữ liệu công đầu tháng 08/2026.");
        }

        db.NhatKyHeThongs.Add(new NhatKyHeThong
        {
            HanhDong = "SeedComplete", DoiTuong = "PresentationData", DoiTuongID = Marker,
            ChiTiet = $"Đã chuẩn hóa dữ liệu trình diễn cho {activeEmployees.Count} nhân viên.",
            NguoiThucHien = "system"
        });
        await db.SaveChangesAsync();
        logger.LogInformation("Đã chuẩn hóa dữ liệu trình diễn công ty cho {Count} nhân viên.", activeEmployees.Count);
    }

    private static async Task NormalizeExistingEmployeeAsync(AppDbContext db, UserManager<TaiKhoan> users)
    {
        var employee = await db.NhanViens.Include(x => x.HopDongs).FirstOrDefaultAsync(x => x.MaNhanVien == "NVTEST012");
        if (employee is null) return;

        employee.MaNhanVien = "NV-CN-002";
        employee.Email = "khanh.truong@tic.local";
        employee.SDT ??= "0902000013";
        employee.CanCuocCongDan ??= "079096100013";
        employee.MaSothue ??= "DEMO100013";
        employee.SoTaiKhoan ??= "001196100013";
        employee.Ngayvaolam = new DateTime(2022, 5, 2);
        employee.DiaChi = "Quận Bình Thạnh, TP. Hồ Chí Minh";
        employee.Trangthai = true;
        foreach (var contract in employee.HopDongs)
        {
            contract.SoHopDong = contract.SoHopDong == "HOPDONG1/12" ? "HD-2022-NV-CN-002" : contract.SoHopDong;
            contract.Ngayky = new DateTime(2022, 4, 25);
            contract.Ngaybatdau = new DateTime(2022, 5, 2);
            contract.Luongcoban = contract.Luongcoban <= 0 ? 17_000_000 : contract.Luongcoban;
        }
        await db.SaveChangesAsync();

        var account = await users.Users.FirstOrDefaultAsync(x => x.NhanVienID == employee.ID);
        if (account is null) return;
        account.MaNhanVien = employee.MaNhanVien;
        account.Email = employee.Email;
        account.UserName = employee.Email;
        account.EmailConfirmed = true;
        var result = await users.UpdateAsync(account);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
    }

    private static async Task NormalizeEmployeeCodesAsync(AppDbContext db, UserManager<TaiKhoan> users)
    {
        var mappings = new Dictionary<string, string>
        {
            ["GD-DEMO-001"] = "GD001", ["GD-DEMO-002"] = "PGD001",
            ["HR-DEMO-001"] = "TPNS001", ["HR-DEMO-002"] = "NS001",
            ["QL-DEMO-001"] = "TPCN001", ["NV-DEMO-001"] = "CN001",
            ["QL-KD-DEMO-001"] = "TPKD001", ["NV-DEMO-002"] = "KD001",
            ["QL-TC-001"] = "TPKT001", ["NV-TC-001"] = "KT001",
            ["QL-MKT-001"] = "TPMKT001", ["NV-MKT-001"] = "MKT001",
            ["QL-CSKH-001"] = "TPCSKH001", ["NV-CSKH-001"] = "CSKH001",
            ["QL-VH-001"] = "TPVH001", ["NV-VH-001"] = "VH001",
            ["QL-QA-001"] = "TPQA001", ["NV-QA-001"] = "QA001",
            ["QL-SP-001"] = "TPSP001", ["NV-SP-001"] = "SP001"
        };
        var oldCodes = mappings.Keys.ToArray();
        var employees = await db.NhanViens.Include(x => x.HopDongs).ThenInclude(x => x.TaiLieuHopDongs)
            .Include(x => x.TaiLieuNhanViens).Where(x => oldCodes.Contains(x.MaNhanVien)).ToListAsync();
        foreach (var employee in employees)
        {
            var newCode = mappings[employee.MaNhanVien];
            employee.MaNhanVien = newCode;
            foreach (var contract in employee.HopDongs)
            {
                contract.SoHopDong = $"HD-{contract.Ngaybatdau.Year}-{newCode}-{contract.ID:000}";
                foreach (var attachment in contract.TaiLieuHopDongs)
                    attachment.TenFileGoc = $"hop-dong-{newCode.ToLowerInvariant()}.png";
            }
            foreach (var document in employee.TaiLieuNhanViens)
                document.TenFileGoc = $"ho-so-{newCode.ToLowerInvariant()}.png";
            var account = await users.Users.FirstOrDefaultAsync(x => x.NhanVienID == employee.ID);
            if (account is not null)
            {
                account.MaNhanVien = newCode;
                var result = await users.UpdateAsync(account);
                if (!result.Succeeded)
                    throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
            }
        }
        await db.SaveChangesAsync();
    }

    private static async Task NormalizeVisibleContentAsync(AppDbContext db)
    {
        var allowanceNames = new[]
        {
            "Phụ cấp ăn trưa toàn công ty", "Phụ cấp trách nhiệm quản lý", "Phụ cấp điện thoại phòng Kinh doanh",
            "Phụ cấp chứng chỉ chuyên môn", "Hỗ trợ đi lại", "Hỗ trợ làm việc từ xa", "Phụ cấp độc hại",
            "Hỗ trợ gửi xe", "Phụ cấp dự án trọng điểm", "Hỗ trợ nhà ở có thời hạn"
        };
        var allowances = await db.ChinhSachPhuCaps.Where(x => x.TenPhuCap.StartsWith("Chính sách phụ cấp demo")).OrderBy(x => x.ID).ToListAsync();
        for (var i = 0; i < allowances.Count && i < allowanceNames.Length; i++)
        {
            allowances[i].TenPhuCap = allowanceNames[i];
            allowances[i].HoatDong = i < 4;
        }

        var insuranceNames = new[]
        {
            "BHXH bắt buộc toàn công ty", "BHYT bắt buộc", "BHTN bắt buộc", "Bảo hiểm tai nạn phòng Vận hành",
            "Bảo hiểm sức khỏe quản lý", "Bảo hiểm sức khỏe nhân viên", "Gói nha khoa", "Bảo hiểm thai sản bổ sung",
            "Bảo hiểm công tác", "Chương trình hưu trí bổ sung"
        };
        var insurances = await db.ChinhSachBhxhs.Where(x => x.TenKhoan.StartsWith("Chính sách BHXH demo")).OrderBy(x => x.ID).ToListAsync();
        for (var i = 0; i < insurances.Count && i < insuranceNames.Length; i++)
        {
            insurances[i].TenKhoan = insuranceNames[i];
            insurances[i].HoatDong = i < 4;
        }

        var documentNames = new[]
        {
            "Sổ tay nhân viên TIC 2026", "Quy định chấm công và nghỉ phép", "Quy trình bảo mật dữ liệu",
            "Quy chế lương, thưởng và phúc lợi", "Hướng dẫn an toàn lao động", "Quy trình tiếp nhận nhân viên mới",
            "Bộ quy tắc ứng xử nội bộ", "Hướng dẫn sử dụng tài sản công ty", "Chính sách đào tạo và phát triển",
            "Dự thảo quy trình đánh giá hiệu suất"
        };
        var documents = await db.TaiLieuNoiBos.Where(x => x.TieuDe.StartsWith("Tài liệu nội bộ demo")).OrderBy(x => x.ID).ToListAsync();
        for (var i = 0; i < documents.Count && i < documentNames.Length; i++)
        {
            documents[i].TieuDe = documentNames[i];
            documents[i].TomTat = $"Nội dung áp dụng trong hoạt động nội bộ của công ty, ban hành năm {Year}.";
        }

        var announcementNames = new[]
        {
            "Lịch họp toàn công ty tháng 8", "Thông báo nghỉ lễ Quốc khánh", "Kế hoạch khám sức khỏe định kỳ",
            "Chương trình đào tạo an toàn thông tin", "Cập nhật chính sách làm việc linh hoạt", "Thông báo bảo trì hệ thống",
            "Chúc mừng nhân viên xuất sắc tháng 7", "Kế hoạch hoạt động gắn kết quý III", "Cập nhật đầu mối hỗ trợ nhân sự",
            "Thông báo đã hết hiệu lực"
        };
        var announcements = await db.ThongBaoNoiBos.Where(x => x.TieuDe.StartsWith("Thông báo nội bộ demo")).OrderBy(x => x.ID).ToListAsync();
        for (var i = 0; i < announcements.Count && i < announcementNames.Length; i++)
        {
            announcements[i].TieuDe = announcementNames[i];
            announcements[i].NoiDung = $"Kính gửi toàn thể nhân viên, vui lòng theo dõi và thực hiện nội dung thông báo số {i + 1}.";
        }

        var surveyNames = new[]
        {
            "Khảo sát mức độ hài lòng", "Khảo sát nhu cầu đào tạo", "Đánh giá chất lượng bữa trưa",
            "Khảo sát hình thức làm việc", "Đánh giá công cụ nội bộ", "Khảo sát hoạt động gắn kết",
            "Nhu cầu chăm sóc sức khỏe", "Khảo sát môi trường làm việc", "Đề xuất chủ đề hội thảo",
            "Khảo sát quý II đã kết thúc"
        };
        var surveys = await db.KhaoSatNoiBos.Where(x => x.TieuDe.StartsWith("Khảo sát nội bộ demo")).OrderBy(x => x.ID).ToListAsync();
        for (var i = 0; i < surveys.Count && i < surveyNames.Length; i++)
        {
            surveys[i].TieuDe = surveyNames[i];
            surveys[i].CauHoi = i % 2 == 0 ? "Bạn đánh giá nội dung này ở mức nào?" : "Bạn mong muốn công ty ưu tiên phương án nào?";
        }

        var files = await db.TaiLieuNhanViens.Where(x => x.TenTaiLieu == "Hồ sơ nhân viên demo").ToListAsync();
        foreach (var file in files) file.TenTaiLieu = "Hồ sơ nhân sự";
        var histories = await db.QuaTrinhCongTacs.Where(x => x.DonVi == "Công ty trước đây (demo)").ToListAsync();
        foreach (var history in histories)
        {
            history.DonVi = "Công ty TNHH Giải pháp Việt";
            history.MoTa = "Kinh nghiệm chuyên môn trước khi gia nhập TIC.";
        }
        var individualAllowances = await db.PhuCaps.Where(x => x.TenPhuCap == "Phụ cấp cá nhân demo").ToListAsync();
        foreach (var allowance in individualAllowances)
        {
            allowance.TenPhuCap = "Hỗ trợ theo thỏa thuận";
            allowance.GhiChu = "Khoản hỗ trợ theo chính sách nhân sự hiện hành";
        }
        await db.SaveChangesAsync();
    }

    private static async Task NormalizeRequestsAsync(AppDbContext db)
    {
        var leaveReasons = new[] { "Giải quyết việc gia đình", "Khám sức khỏe định kỳ", "Tham dự lễ cưới người thân", "Chăm con ốm", "Nghỉ phép cá nhân" };
        var leaves = await db.NghiPheps.Where(x => x.Lydo.StartsWith("Tình huống nghỉ phép demo")).OrderBy(x => x.ID).ToListAsync();
        for (var i = 0; i < leaves.Count; i++) leaves[i].Lydo = leaveReasons[i % leaveReasons.Length];

        var overtimeReasons = new[] { "Hoàn thiện báo cáo cuối tháng", "Xử lý sự cố hệ thống", "Chuẩn bị hồ sơ khách hàng", "Đóng gói phiên bản phát hành", "Kiểm kê và đối soát dữ liệu" };
        var overtime = await db.DangKyTangCas.Where(x => x.LyDo.StartsWith("OT demo")).OrderBy(x => x.ID).ToListAsync();
        for (var i = 0; i < overtime.Count; i++) overtime[i].LyDo = overtimeReasons[i % overtimeReasons.Length];

        var adjustments = await db.DieuChinhChamCongs.Where(x => x.LyDo.StartsWith("Yêu cầu sửa công demo")).OrderBy(x => x.ID).ToListAsync();
        for (var i = 0; i < adjustments.Count; i++) adjustments[i].LyDo = i % 2 == 0 ? "Máy chấm công không ghi nhận giờ ra" : "Quên chấm công đầu ngày";

        var profiles = await db.YeuCauCapNhatThongTinCaNhans.Where(x => x.LyDo.StartsWith("Cập nhật hồ sơ demo")).OrderBy(x => x.ID).ToListAsync();
        for (var i = 0; i < profiles.Count; i++) profiles[i].LyDo = i % 2 == 0 ? "Cập nhật nơi cư trú hiện tại" : "Cập nhật số điện thoại liên hệ";

        var trips = await db.CongTacs.Where(x => x.MucDich.StartsWith("Công tác demo")).OrderBy(x => x.ID).ToListAsync();
        var tripPurposes = new[] { "Gặp gỡ khách hàng", "Khảo sát địa điểm triển khai", "Đào tạo chi nhánh", "Đối soát vận hành", "Tham dự hội thảo chuyên ngành" };
        for (var i = 0; i < trips.Count; i++) trips[i].MucDich = tripPurposes[i % tripPurposes.Length];

        var workforce = await db.YeuCauNhanLuc.Where(x => x.ViTriCanBoSung.StartsWith("Vị trí tuyển dụng demo")).OrderBy(x => x.ID).ToListAsync();
        var positions = new[] { "Lập trình viên .NET", "Nhân viên kinh doanh", "Chuyên viên tuyển dụng", "Kiểm thử viên", "Kế toán tổng hợp" };
        for (var i = 0; i < workforce.Count; i++)
        {
            workforce[i].ViTriCanBoSung = positions[i % positions.Length];
            workforce[i].MoTaCongViec = "Thực hiện công việc chuyên môn theo kế hoạch của phòng ban.";
        }

        var notifications = await db.ThongBaoCaNhans.Where(x => x.MaThamChieu.StartsWith("DEMO-NOTIFY-")).ToListAsync();
        foreach (var notification in notifications)
            notification.NoiDung = notification.Loai == "Payroll" ? "Bảng lương tháng 07/2026 đã được phát hành." : "Yêu cầu của bạn vừa được cập nhật trạng thái.";
        await db.SaveChangesAsync();
    }

    private static async Task NormalizeAugustAttendanceAsync(AppDbContext db)
    {
        var employees = await db.NhanViens.Where(x => x.Trangthai).OrderBy(x => x.ID).ToListAsync();
        var employeeIds = employees.Select(x => x.ID).ToArray();
        var artificialFuture = await db.ChamCongs.Where(x => employeeIds.Contains(x.NhanVienID) && x.Ngay > new DateTime(Year, 8, 9) && x.Ngay < new DateTime(Year, 9, 1)).ToListAsync();
        db.ChamCongs.RemoveRange(artificialFuture);
        await db.SaveChangesAsync();

        var workDays = Enumerable.Range(1, 9).Select(x => new DateTime(Year, 8, x))
            .Where(x => x.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday).ToArray();
        var existing = await db.ChamCongs.Where(x => employeeIds.Contains(x.NhanVienID) && x.Ngay >= new DateTime(Year, 8, 1) && x.Ngay <= new DateTime(Year, 8, 9)).ToListAsync();
        for (var i = 0; i < employees.Count; i++)
        for (var d = 0; d < workDays.Length; d++)
        {
            var employee = employees[i];
            var date = workDays[d];
            var attendance = existing.FirstOrDefault(x => x.NhanVienID == employee.ID && x.Ngay == date);
            if (attendance is null)
            {
                attendance = new ChamCong { NhanVienID = employee.ID, Ngay = date };
                db.ChamCongs.Add(attendance);
                existing.Add(attendance);
            }
            attendance.GhiChu = null;
            attendance.TrangThai = AttendanceStatuses.Present;
            attendance.Giovaolam = date.AddHours(8).AddMinutes((i + d) % 8 == 0 ? 20 : (i % 3) * 5);
            attendance.Giotanlam = date.AddHours(17).AddMinutes(i % 2 * 10);
            if (i == 3 && d == 2)
            {
                attendance.TrangThai = AttendanceStatuses.PaidLeave;
                attendance.Giovaolam = attendance.Giotanlam = null;
            }
            else if (i == 7 && d == 3)
            {
                attendance.TrangThai = AttendanceStatuses.UnpaidLeave;
                attendance.Giovaolam = attendance.Giotanlam = null;
            }
            else if (i == 11 && d == 4)
            {
                attendance.TrangThai = AttendanceStatuses.Absent;
                attendance.Giovaolam = attendance.Giotanlam = null;
            }
            else if (i == 1 && d == 4)
            {
                attendance.Giotanlam = null;
            }
            AttendancePolicy.ApplyLateRule(attendance);
        }
        await db.SaveChangesAsync();
    }

    private static async Task EnsureEveryEmployeePayrollAsync(AppDbContext db)
    {
        var employees = await db.NhanViens.Include(x => x.ChucVu).Include(x => x.HopDongs).Where(x => x.Trangthai).ToListAsync();
        foreach (var employee in employees)
        {
            var contract = employee.HopDongs.OrderByDescending(x => x.Ngaybatdau).FirstOrDefault();
            if (contract is null) continue;
            foreach (var month in new[] { 5, 6, 7 })
            {
                var payroll = await db.Luongs.FirstOrDefaultAsync(x => x.NhanVienID == employee.ID && x.Nam == Year && x.Thang == month);
                var coefficient = employee.ChucVu?.Hesoluong ?? 1m;
                var workDays = 21m + (employee.ID + month) % 2;
                var salaryBasis = contract.Luongcoban * coefficient;
                var allowance = 800_000m + employee.ID % 4 * 100_000m;
                var bonus = month == 7 ? 1_000_000m : 0m;
                var otherIncome = month == 6 ? 500_000m : 0m;
                var overtimeHours = (employee.ID + month) % 4 * 2m;
                var overtimePay = Math.Round(overtimeHours * salaryBasis / 22m / 8m * 2m, 0);
                var penalty = employee.ID % 6 == 0 ? Math.Round(salaryBasis / 22m / 8m, 0) : 0m;
                const decimal insurance = 1_890_000m;
                var net = Math.Round(salaryBasis / 22m * workDays + allowance + bonus + otherIncome + overtimePay - penalty - insurance, 0);
                if (payroll is null)
                {
                    payroll = new Luong { NhanVienID = employee.ID, Thang = month, Nam = Year };
                    db.Luongs.Add(payroll);
                }
                payroll.Ten = $"Lương tháng {month:00}/{Year}";
                payroll.Luongcoban = contract.Luongcoban; payroll.HeSoLuong = coefficient;
                payroll.SoNgayCong = workDays; payroll.SoNgayChuan = 22; payroll.Phucap = allowance;
                payroll.Thuong = bonus; payroll.ThuNhapKhac = otherIncome; payroll.SoGioTangCa = overtimeHours;
                payroll.TienTangCa = overtimePay; payroll.Khautru = penalty; payroll.BaoHiem = insurance;
                payroll.ThueTNCN = 0; payroll.Tongluong = net; payroll.TrangThai = PayrollStatuses.Paid;
                payroll.NgayTinh = new DateTime(Year, month, 28); payroll.NgayDoiSoat = new DateTime(Year, month, 29);
                payroll.NgayKhoa = new DateTime(Year, month, Math.Min(30, DateTime.DaysInMonth(Year, month)));
                payroll.NguoiDoiSoat = "hr@tic.local"; payroll.NguoiKhoa = "admin@tic.local";
            }
        }
        await db.SaveChangesAsync();
    }

    private static async Task EnsureEveryEmployeeSupportingDataAsync(AppDbContext db)
    {
        var employees = await db.NhanViens.Where(x => x.Trangthai).ToListAsync();
        var annual = await db.LoaiNghiPheps.FirstAsync(x => x.MaLoai == "PHEP_NAM");
        foreach (var employee in employees)
        {
            if (!await db.SoDuNghiPheps.AnyAsync(x => x.NhanVienID == employee.ID && x.LoaiNghiPhepID == annual.ID && x.Nam == Year))
                db.SoDuNghiPheps.Add(new SoDuNghiPhep { NhanVienID = employee.ID, LoaiNghiPhepID = annual.ID, Nam = Year, SoNgayDuocHuong = 12, GhiChu = "Quỹ phép năm 2026" });
            var reference = $"PAYROLL-2026-07-{employee.MaNhanVien}";
            if (!await db.ThongBaoCaNhans.AnyAsync(x => x.NhanVienID == employee.ID && x.MaThamChieu == reference))
                db.ThongBaoCaNhans.Add(new ThongBaoCaNhan { NhanVienID = employee.ID, Loai = "Payroll", TieuDe = "Bảng lương tháng 07/2026", NoiDung = "Bảng lương tháng 07/2026 đã được phát hành.", MaThamChieu = reference, DuongDan = "/Employee/Home" });
        }
        await db.SaveChangesAsync();
    }
}
