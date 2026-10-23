using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QLNS.Models.Entities;

namespace QLNS.Data;

public static class DemoDataSeeder
{
    private const string Marker = "NV-DEMO-001";
    private const string ComprehensiveMarker = "COMPREHENSIVE-COMPANY-2026-V1";
    private const string PresentationMarker = "COMPANY-PRESENTATION-2026-V2";

    public static async Task SeedAsync(AppDbContext db, UserManager<TaiKhoan> users, ILogger logger)
    {
        // Các bước chuẩn hóa trình diễn có thể đổi mã nhân viên. Dùng nhật ký hoàn tất
        // làm dấu ổn định để seeder gốc không tạo lại cùng email/CCCD ở lần khởi động sau.
        if (await db.NhatKyHeThongs.AnyAsync(x =>
                x.DoiTuongID == ComprehensiveMarker || x.DoiTuongID == PresentationMarker))
        {
            logger.LogInformation("Bộ dữ liệu nền đã tồn tại; bỏ qua seeder khởi tạo cũ.");
            return;
        }

        if (await db.NhanViens.AnyAsync(x => x.MaNhanVien == Marker))
        {
            await EnsureExpandedDemoDataAsync(db, users, logger);
            return;
        }

        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
        if (await db.NhanViens.AnyAsync(x => x.MaNhanVien == Marker)) return;
        await using var transaction = await db.Database.BeginTransactionAsync();
        var today = DateTime.Today;

        var departments = new[]
        {
            new PhongBan { Tenphongban = "Ban Giám đốc" },
            new PhongBan { Tenphongban = "Phòng Nhân sự" },
            new PhongBan { Tenphongban = "Phòng Công nghệ" },
            new PhongBan { Tenphongban = "Phòng Kinh doanh" }
        };
        var positions = new[]
        {
            new ChucVu { Chucvu = "Trưởng phòng", Hesoluong = 2.20m, PhanQuyen = AuthorizationRoles.Manager },
            new ChucVu { Chucvu = "Chuyên viên nhân sự", Hesoluong = 1.40m, PhanQuyen = AuthorizationRoles.HumanResources },
            new ChucVu { Chucvu = "Lập trình viên", Hesoluong = 1.60m, PhanQuyen = AuthorizationRoles.Employee },
            new ChucVu { Chucvu = "Nhân viên kinh doanh", Hesoluong = 1.30m, PhanQuyen = AuthorizationRoles.Employee },
            new ChucVu { Chucvu = "Giám đốc", Hesoluong = 3.00m, PhanQuyen = AuthorizationRoles.Administrator },
            new ChucVu { Chucvu = "Phó giám đốc", Hesoluong = 2.60m, PhanQuyen = AuthorizationRoles.Manager }
        };
        db.AddRange(departments);
        db.AddRange(positions);
        await db.SaveChangesAsync();

        var employees = new[]
        {
            Employee("QL-DEMO-001", "Trần Minh Quân", "quanly@tic.local", "0901000001", "079088000001", "DEMO000001", true, new(1988,4,12), today.AddYears(-5), departments[2], positions[0]),
            Employee(Marker, "Nguyễn Hoàng An", "nhanvien@tic.local", "0901000002", "079097000002", "DEMO000002", true, new(1997,9,21), today.AddYears(-2), departments[2], positions[2]),
            Employee("HR-DEMO-001", "Lê Thu Hà", "hr@tic.local", "0901000003", "079194000003", "DEMO000003", false, new(1994,2,15), today.AddYears(-3), departments[1], positions[1]),
            Employee("NV-DEMO-002", "Phạm Gia Bình", "binh.pham@tic.local", "0901000004", "079099000004", "DEMO000004", true, new(1999,11,8), today.AddMonths(-10), departments[3], positions[3]),
            Employee("GD-DEMO-001", "Nguyễn Đức Minh", "minh.nguyen@tic.local", "0901000005", "079085000005", "DEMO000005", true, new(1985,6,10), today.AddYears(-8), departments[0], positions[4]),
            Employee("GD-DEMO-002", "Vũ Thanh Mai", "mai.vu@tic.local", "0901000006", "079190000006", "DEMO000006", false, new(1990,3,18), today.AddYears(-6), departments[0], positions[5]),
            Employee("QL-KD-DEMO-001", "Đỗ Quốc Khánh", "khanh.do@tic.local", "0901000007", "079089000007", "DEMO000007", true, new(1989,8,24), today.AddYears(-4), departments[3], positions[0]),
            Employee("HR-DEMO-002", "Trương Ngọc Lan", "lan.truong@tic.local", "0901000008", "079096000008", "DEMO000008", false, new(1996,12,2), today.AddYears(-2), departments[1], positions[1])
        };
        db.NhanViens.AddRange(employees);
        await db.SaveChangesAsync();
        departments[1].Matruongphong = employees[2].ID;
        departments[2].Matruongphong = employees[0].ID;
        departments[0].Matruongphong = employees[4].ID;
        departments[3].Matruongphong = employees[6].ID;
        employees[2].ChucVuID = positions[0].ID;

        var manager = await LinkAccount(users, "quanly@tic.local", "Quanly@123456", employees[0], "QuanLy");
        var staff = await LinkAccount(users, "nhanvien@tic.local", "Nhanvien@123456", employees[1], "NhanVien");
        var hr = await LinkAccount(users, "hr@tic.local", "HrDemo@123456", employees[2], "HR");
        var sales = await LinkAccount(users, "binh.pham@tic.local", "Nhanvien@123456", employees[3], "NhanVien");
        await LinkAccount(users, "lan.truong@tic.local", "HrStaff@123456", employees[7], "HR");

        db.HopDongs.AddRange(
            Contract(employees[0], "HD-DEMO-001", ContractTypes.FixedTerm, today.AddYears(-5), today.AddYears(1), 28_000_000),
            Contract(employees[1], "HD-DEMO-002", ContractTypes.FixedTerm, today.AddYears(-2), today.AddDays(35), 18_000_000),
            Contract(employees[2], "HD-DEMO-003", ContractTypes.FixedTerm, today.AddYears(-3), today.AddYears(1), 20_000_000),
            Contract(employees[3], "HD-DEMO-004", ContractTypes.FixedTerm, today.AddMonths(-10), today.AddMonths(2), 14_000_000),
            Contract(employees[4], "HD-DEMO-005", ContractTypes.FixedTerm, today.AddYears(-8), today.AddYears(2), 35_000_000),
            Contract(employees[5], "HD-DEMO-006", ContractTypes.FixedTerm, today.AddYears(-6), today.AddYears(1), 30_000_000),
            Contract(employees[6], "HD-DEMO-007", ContractTypes.FixedTerm, today.AddYears(-4), today.AddMonths(8), 24_000_000),
            Contract(employees[7], "HD-DEMO-008", ContractTypes.FixedTerm, today.AddYears(-2), today.AddMonths(4), 16_000_000));

        var annual = await db.LoaiNghiPheps.SingleAsync(x => x.MaLoai == "PHEP_NAM");
        var sick = await db.LoaiNghiPheps.SingleAsync(x => x.MaLoai == "NGHI_OM");
        foreach (var employee in employees)
            db.SoDuNghiPheps.Add(new SoDuNghiPhep { NhanVienID = employee.ID, LoaiNghiPhepID = annual.ID, Nam = today.Year, SoNgayDuocHuong = 16, DieuChinh = employee == employees[0] ? 1 : 0, GhiChu = "Số dư phép demo" });
        db.NghiPheps.AddRange(
            Leave(employees[1], annual, today.AddDays(-8), today.AddDays(-7), "Giải quyết việc gia đình", LeaveStatuses.Approved, manager.Email),
            Leave(employees[3], annual, today.AddDays(5), today.AddDays(5), "Nghỉ phép cá nhân", LeaveStatuses.Pending),
            Leave(employees[2], sick, today.AddDays(-18), today.AddDays(-18), "Khám sức khỏe", LeaveStatuses.Rejected, manager.Email, "Vui lòng bổ sung giấy xác nhận khám bệnh"));

        var shift = await db.CaLamViecs.SingleAsync(x => x.MaCa == "HC");
        foreach (var employee in employees)
            db.PhanCaNhanViens.Add(new PhanCaNhanVien { NhanVienID = employee.ID, CaLamViecID = shift.ID, TuNgay = new(today.Year, 1, 1) });
        SeedAttendance(db, employees, today);

        db.DangKyTangCas.AddRange(
            new DangKyTangCa { NhanVienID = employees[1].ID, Ngay = BusinessDay(today, -3), GioBatDau = new(18,0,0), GioKetThuc = new(20,0,0), HeSo = 2m, SoGioDuyet = 2, LyDo = "Hoàn thiện báo cáo tháng", TrangThai = ApprovalStatuses.Approved, NguoiDuyet = manager.Email, NgayDuyet = DateTime.UtcNow.AddDays(-2) },
            new DangKyTangCa { NhanVienID = employees[3].ID, Ngay = BusinessDay(today, 2), GioBatDau = new(18,0,0), GioKetThuc = new(21,0,0), HeSo = 2m, LyDo = "Chuẩn bị hồ sơ khách hàng", TrangThai = ApprovalStatuses.Pending });
        db.CongTacs.AddRange(
            new CongTac { NhanVienID = employees[0].ID, TuNgay = today.AddDays(-15), DenNgay = today.AddDays(-13), DiaDiem = "Đà Nẵng", MucDich = "Làm việc với đối tác triển khai", TamUng = 5_000_000, TrangThai = ApprovalStatuses.Approved, NguoiDuyet = "admin@tic.local", NgayDuyet = DateTime.UtcNow.AddDays(-18), PhanCongs = [new() { NhanVienID = employees[0].ID }, new() { NhanVienID = employees[1].ID }] },
            new CongTac { NhanVienID = employees[3].ID, TuNgay = today.AddDays(10), DenNgay = today.AddDays(12), DiaDiem = "Hà Nội", MucDich = "Gặp khách hàng tiềm năng", TamUng = 4_000_000, TrangThai = ApprovalStatuses.Approved, NguoiDuyet = hr.Email, NgayDuyet = DateTime.UtcNow, PhanCongs = [new() { NhanVienID = employees[3].ID }] });
        db.DieuChinhChamCongs.AddRange(
            new DieuChinhChamCong { NhanVienID = employees[1].ID, Ngay = BusinessDay(today, -1), GioVaoDeXuat = new(8,0,0), GioRaDeXuat = new(17,30,0), LyDo = "Máy chấm công không ghi nhận giờ ra", TrangThai = ApprovalStatuses.Pending },
            new DieuChinhChamCong { NhanVienID = employees[7].ID, Ngay = BusinessDay(today, -4), GioVaoDeXuat = new(8,15,0), GioRaDeXuat = new(17,0,0), LyDo = "Quên chấm công đầu ngày", TrangThai = ApprovalStatuses.Approved, NguoiDuyet = hr.Email, NgayDuyet = DateTime.UtcNow.AddDays(-2) });

        db.TrinhDoChungChis.AddRange(
            new TrinhDoChungChi { NhanVienID = employees[1].ID, Loai = "Bằng cấp", Ten = "Kỹ sư Công nghệ thông tin", DonViCap = "Đại học Bách khoa", NgayCap = new(2019,8,20), SoHieu = "CNTT-DEMO-001" },
            new TrinhDoChungChi { NhanVienID = employees[1].ID, Loai = "Chứng chỉ", Ten = "Microsoft Azure Fundamentals", DonViCap = "Microsoft", NgayCap = today.AddYears(-1), NgayHetHan = today.AddYears(2), SoHieu = "AZ900-DEMO" },
            new TrinhDoChungChi { NhanVienID = employees[2].ID, Loai = "Chứng chỉ", Ten = "Quản trị nhân sự chuyên nghiệp", DonViCap = "Viện Quản trị Nhân lực", NgayCap = today.AddYears(-2), SoHieu = "HR-DEMO-2024" });
        db.QuaTrinhCongTacs.AddRange(
            new QuaTrinhCongTac { NhanVienID = employees[1].ID, DonVi = "Công ty Phần mềm Sao Việt", ChucDanh = "Lập trình viên .NET", TuNgay = new(2019,9,1), DenNgay = today.AddYears(-2).AddDays(-1), MoTa = "Phát triển hệ thống quản trị doanh nghiệp" },
            new QuaTrinhCongTac { NhanVienID = employees[2].ID, DonVi = "Công ty Dịch vụ Thành Công", ChucDanh = "Chuyên viên tuyển dụng", TuNgay = new(2018,6,1), DenNgay = today.AddYears(-3).AddDays(-1), MoTa = "Tuyển dụng và quản lý hồ sơ nhân sự" });

        db.PhuCaps.AddRange(
            Allowance(employees[0], "Phụ cấp trách nhiệm", 3_000_000, today.AddMonths(-6)), Allowance(employees[1], "Phụ cấp ăn trưa", 730_000, today.AddMonths(-6)),
            Allowance(employees[1], "Phụ cấp điện thoại", 300_000, today.AddMonths(-3)), Allowance(employees[2], "Phụ cấp ăn trưa", 730_000, today.AddMonths(-6)),
            Allowance(employees[3], "Phụ cấp đi lại", 800_000, today.AddMonths(-3)));
        db.ThuongPhats.AddRange(
            Reward(employees[1], "Thuong", "Hoàn thành tốt sprint", 1_500_000, today.AddDays(-5)),
            Reward(employees[3], "Thuong", "Đạt chỉ tiêu doanh số", 2_000_000, today.AddDays(-4)),
            Reward(employees[1], "Phat", "Đi muộn quá thời gian cho phép", 200_000, today.AddDays(-10)));

        var insuranceTypes = await EnsureInsuranceTypes(db);
        foreach (var employee in employees)
            foreach (var (type, amount) in insuranceTypes.Zip(new decimal[] { 1_440_000, 270_000, 180_000 }))
                db.BaoHiems.Add(new BaoHiem { NhanVienID = employee.ID, LoaiBaoHiemID = type.MaLoaiBaoHiem, SoTienDong = amount, NgayBatDau = today.AddMonths(-6), TrangThai = "DangDong" });

        var month = today.AddMonths(-1);
        db.Luongs.AddRange(
            Payroll(employees[0], month, 28_000_000, 3_000_000, 0, 0, 2_940_000),
            Payroll(employees[1], month, 18_000_000, 1_030_000, 1_500_000, 200_000, 1_890_000),
            Payroll(employees[2], month, 20_000_000, 730_000, 0, 0, 2_100_000),
            Payroll(employees[3], month, 14_000_000, 800_000, 2_000_000, 0, 1_470_000),
            Payroll(employees[4], month, 35_000_000, 4_000_000, 0, 0, 3_675_000),
            Payroll(employees[5], month, 30_000_000, 3_000_000, 0, 0, 3_150_000),
            Payroll(employees[6], month, 24_000_000, 2_000_000, 2_500_000, 0, 2_520_000),
            Payroll(employees[7], month, 16_000_000, 730_000, 0, 0, 1_680_000));

        var document = new TaiLieuNoiBo { TieuDe = "Sổ tay nhân viên 2026", DanhMuc = "Chính sách", TomTat = "Quy định giờ làm việc, nghỉ phép, ứng xử và bảo mật thông tin.", PhamVi = "All", BatBuocXacNhan = true, DaPhatHanh = true, NgayPhatHanh = DateTime.UtcNow.AddDays(-7), NguoiTao = hr.Email! };
        var document2 = new TaiLieuNoiBo { TieuDe = "Quy trình bảo mật dữ liệu", DanhMuc = "Quy trình", TomTat = "Hướng dẫn lưu trữ, chia sẻ và bảo vệ dữ liệu nội bộ.", PhamVi = "All", BatBuocXacNhan = true, DaPhatHanh = true, NgayPhatHanh = DateTime.UtcNow.AddDays(-3), NguoiTao = hr.Email! };
        var announcement = new ThongBaoNoiBo { TieuDe = "Thông báo họp toàn công ty", NoiDung = "Họp cập nhật kết quả quý vào 09:00 thứ Sáu tại phòng họp lớn.", PhamVi = "All", Ghim = true, BatDau = DateTime.UtcNow.AddDays(-2), KetThuc = DateTime.UtcNow.AddDays(14), NguoiTao = hr.Email! };
        var announcement2 = new ThongBaoNoiBo { TieuDe = "Lịch đào tạo an toàn thông tin", NoiDung = "Buổi đào tạo bắt buộc diễn ra lúc 14:00 tại phòng đào tạo.", PhamVi = "All", BatDau = DateTime.UtcNow.AddDays(-1), KetThuc = DateTime.UtcNow.AddDays(20), NguoiTao = hr.Email! };
        var survey = new KhaoSatNoiBo { TieuDe = "Khảo sát trải nghiệm làm việc", CauHoi = "Bạn đánh giá môi trường làm việc hiện tại như thế nào?", BatDau = DateTime.UtcNow.AddDays(-5), KetThuc = DateTime.UtcNow.AddDays(20), HoatDong = true, NguoiTao = hr.Email!, LuaChons = [new() { NoiDung = "Rất tốt" }, new() { NoiDung = "Tốt" }, new() { NoiDung = "Cần cải thiện" }] };
        var survey2 = new KhaoSatNoiBo { TieuDe = "Khảo sát nhu cầu đào tạo", CauHoi = "Bạn muốn ưu tiên chủ đề đào tạo nào trong quý tới?", BatDau = DateTime.UtcNow.AddDays(-2), KetThuc = DateTime.UtcNow.AddDays(25), HoatDong = true, NguoiTao = hr.Email!, LuaChons = [new() { NoiDung = "Kỹ năng chuyên môn" }, new() { NoiDung = "Kỹ năng quản lý" }, new() { NoiDung = "Ngoại ngữ" }] };
        db.AddRange(document, document2, announcement, announcement2, survey, survey2);
        db.QuyTrinhPheDuyets.AddRange(
            Workflow("NghiPhep", "Duyệt đơn nghỉ phép", (1,"QuanLy","Quản lý trực tiếp"), (2,"HR","Nhân sự xác nhận")),
            Workflow("TangCa", "Duyệt đăng ký tăng ca", (1,"QuanLy","Quản lý trực tiếp")),
            Workflow("DieuChinhChamCong", "Duyệt điều chỉnh chấm công", (1,"QuanLy","Quản lý trực tiếp"), (2,"HR","Nhân sự xác nhận")));
        await db.SaveChangesAsync();

        db.XacNhanTaiLieus.Add(new XacNhanTaiLieu { TaiLieuNoiBoID = document.ID, TaiKhoanID = manager.Id, ThoiGian = DateTime.UtcNow.AddDays(-1) });
        db.XacNhanTaiLieus.Add(new XacNhanTaiLieu { TaiLieuNoiBoID = document2.ID, TaiKhoanID = staff.Id, ThoiGian = DateTime.UtcNow });
        db.BinhLuanThongBaos.Add(new BinhLuanThongBao { ThongBaoNoiBoID = announcement.ID, TaiKhoanID = sales.Id, NoiDung = "Đã nhận thông tin, cảm ơn phòng Nhân sự.", NgayTao = DateTime.UtcNow.AddHours(-6) });
        db.BinhLuanThongBaos.Add(new BinhLuanThongBao { ThongBaoNoiBoID = announcement2.ID, TaiKhoanID = manager.Id, NoiDung = "Bộ phận đã sắp xếp nhân sự tham gia.", NgayTao = DateTime.UtcNow.AddHours(-2) });
        db.PhieuKhaoSats.Add(new PhieuKhaoSat { KhaoSatNoiBoID = survey.ID, LuaChonKhaoSatID = survey.LuaChons.First().ID, TaiKhoanID = staff.Id, NgayTraLoi = DateTime.UtcNow.AddDays(-1) });
        db.PhieuKhaoSats.Add(new PhieuKhaoSat { KhaoSatNoiBoID = survey2.ID, LuaChonKhaoSatID = survey2.LuaChons.First().ID, TaiKhoanID = manager.Id, NgayTraLoi = DateTime.UtcNow });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        logger.LogInformation("Da tao du lieu demo HRM cho {Count} nhan vien.", employees.Length);
        });
    }

    private static async Task EnsureExpandedDemoDataAsync(AppDbContext db, UserManager<TaiKhoan> users, ILogger logger)
    {
        const string upgradeMarker = "GD-DEMO-001";
        if (await db.NhanViens.AnyAsync(x => x.MaNhanVien == upgradeMarker))
        {
            var existingHrStaff = await db.NhanViens.FirstOrDefaultAsync(x => x.MaNhanVien == "HR-DEMO-002");
            if (existingHrStaff is not null)
                await LinkAccount(users, "lan.truong@tic.local", "HrStaff@123456", existingHrStaff, "HR");
            logger.LogInformation("Bo du lieu demo day du da ton tai.");
            return;
        }

        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            if (await db.NhanViens.AnyAsync(x => x.MaNhanVien == upgradeMarker)) return;
            await using var transaction = await db.Database.BeginTransactionAsync();
            var today = DateTime.Today;

            var departmentNames = new[] { "Ban Giám đốc", "Phòng Nhân sự", "Phòng Công nghệ", "Phòng Kinh doanh" };
            var departments = await db.PhongBans.Where(x => departmentNames.Contains(x.Tenphongban)).ToListAsync();
            foreach (var name in departmentNames)
                if (departments.All(x => x.Tenphongban != name))
                {
                    var department = new PhongBan { Tenphongban = name };
                    db.PhongBans.Add(department);
                    departments.Add(department);
                }

            var positionDefinitions = new (string Name, decimal Factor, string Role)[]
            {
                ("Trưởng phòng", 2.20m, AuthorizationRoles.Manager),
                ("Chuyên viên nhân sự", 1.40m, AuthorizationRoles.HumanResources),
                ("Lập trình viên", 1.60m, AuthorizationRoles.Employee),
                ("Nhân viên kinh doanh", 1.30m, AuthorizationRoles.Employee),
                ("Giám đốc", 3.00m, AuthorizationRoles.Administrator),
                ("Phó giám đốc", 2.60m, AuthorizationRoles.Manager)
            };
            var positionNames = positionDefinitions.Select(x => x.Name).ToArray();
            var positions = await db.ChucVus.Where(x => positionNames.Contains(x.Chucvu)).ToListAsync();
            foreach (var definition in positionDefinitions)
                if (positions.All(x => x.Chucvu != definition.Name))
                {
                    var position = new ChucVu
                    {
                        Chucvu = definition.Name,
                        Hesoluong = definition.Factor,
                        PhanQuyen = definition.Role
                    };
                    db.ChucVus.Add(position);
                    positions.Add(position);
                }
            await db.SaveChangesAsync();

            PhongBan Department(string name) => departments.First(x => x.Tenphongban == name);
            ChucVu Position(string name) => positions.First(x => x.Chucvu == name);

            var addedEmployees = new List<NhanVien>();
            async Task<NhanVien> EnsureEmployee(
                string code, string name, string email, string phone, string citizenId, string taxId,
                bool gender, DateTime birthday, DateTime joined, string department, string position)
            {
                var existing = await db.NhanViens.FirstOrDefaultAsync(x => x.MaNhanVien == code);
                if (existing is not null) return existing;
                var employee = Employee(code,name,email,phone,citizenId,taxId,gender,birthday,joined,Department(department),Position(position));
                db.NhanViens.Add(employee);
                addedEmployees.Add(employee);
                return employee;
            }

            var director = await EnsureEmployee("GD-DEMO-001","Nguyễn Đức Minh","minh.nguyen@tic.local","0901000005","079085000005","DEMO000005",true,new(1985,6,10),today.AddYears(-8),"Ban Giám đốc","Giám đốc");
            var deputy = await EnsureEmployee("GD-DEMO-002","Vũ Thanh Mai","mai.vu@tic.local","0901000006","079190000006","DEMO000006",false,new(1990,3,18),today.AddYears(-6),"Ban Giám đốc","Phó giám đốc");
            var salesManager = await EnsureEmployee("QL-KD-DEMO-001","Đỗ Quốc Khánh","khanh.do@tic.local","0901000007","079089000007","DEMO000007",true,new(1989,8,24),today.AddYears(-4),"Phòng Kinh doanh","Trưởng phòng");
            var hrStaff = await EnsureEmployee("HR-DEMO-002","Trương Ngọc Lan","lan.truong@tic.local","0901000008","079096000008","DEMO000008",false,new(1996,12,2),today.AddYears(-2),"Phòng Nhân sự","Chuyên viên nhân sự");
            await db.SaveChangesAsync();
            await LinkAccount(users, "lan.truong@tic.local", "HrStaff@123456", hrStaff, AuthorizationRoles.HumanResources);

            var techManager = await db.NhanViens.FirstAsync(x => x.MaNhanVien == "QL-DEMO-001");
            var hrManager = await db.NhanViens.FirstAsync(x => x.MaNhanVien == "HR-DEMO-001");
            hrManager.ChucVuID = Position("Trưởng phòng").ID;
            Department("Ban Giám đốc").Matruongphong = director.ID;
            Department("Phòng Nhân sự").Matruongphong = hrManager.ID;
            Department("Phòng Công nghệ").Matruongphong = techManager.ID;
            Department("Phòng Kinh doanh").Matruongphong = salesManager.ID;

            var contractData = new[]
            {
                (director,"HD-DEMO-005",ContractTypes.FixedTerm,today.AddYears(-8),today.AddYears(2),35_000_000m),
                (deputy,"HD-DEMO-006",ContractTypes.FixedTerm,today.AddYears(-6),today.AddYears(1),30_000_000m),
                (salesManager,"HD-DEMO-007",ContractTypes.FixedTerm,today.AddYears(-4),today.AddMonths(8),24_000_000m),
                (hrStaff,"HD-DEMO-008",ContractTypes.FixedTerm,today.AddYears(-2),today.AddMonths(4),16_000_000m)
            };
            foreach (var (employee,number,type,start,end,salary) in contractData)
                if (!await db.HopDongs.AnyAsync(x => x.SoHopDong == number))
                    db.HopDongs.Add(Contract(employee,number,type,start,end,salary));

            var annual = await db.LoaiNghiPheps.SingleAsync(x => x.MaLoai == "PHEP_NAM");
            var shift = await db.CaLamViecs.SingleAsync(x => x.MaCa == "HC");
            var insuranceTypes = await EnsureInsuranceTypes(db);
            foreach (var employee in addedEmployees)
            {
                db.SoDuNghiPheps.Add(new SoDuNghiPhep { NhanVienID=employee.ID,LoaiNghiPhepID=annual.ID,Nam=today.Year,SoNgayDuocHuong=16,GhiChu="Số dư phép demo" });
                db.PhanCaNhanViens.Add(new PhanCaNhanVien { NhanVienID=employee.ID,CaLamViecID=shift.ID,TuNgay=new(today.Year,1,1) });
                foreach (var (type,amount) in insuranceTypes.Zip(new decimal[] { 1_440_000,270_000,180_000 }))
                    db.BaoHiems.Add(new BaoHiem { NhanVienID=employee.ID,LoaiBaoHiemID=type.MaLoaiBaoHiem,SoTienDong=amount,NgayBatDau=today.AddMonths(-6),TrangThai="DangDong" });
            }
            SeedAttendance(db,addedEmployees,today);

            var month=today.AddMonths(-1);
            var payrollData = new[]
            {
                (director,35_000_000m,4_000_000m,0m,0m,3_675_000m),
                (deputy,30_000_000m,3_000_000m,0m,0m,3_150_000m),
                (salesManager,24_000_000m,2_000_000m,2_500_000m,0m,2_520_000m),
                (hrStaff,16_000_000m,730_000m,0m,0m,1_680_000m)
            };
            foreach (var (employee,salary,allowance,reward,deduction,insurance) in payrollData)
                if (!await db.Luongs.AnyAsync(x => x.NhanVienID==employee.ID&&x.Thang==month.Month&&x.Nam==month.Year))
                    db.Luongs.Add(Payroll(employee,month,salary,allowance,reward,deduction,insurance));

            if (!await db.DieuChinhChamCongs.AnyAsync(x => x.LyDo == "Quên chấm công đầu ngày"))
                db.DieuChinhChamCongs.Add(new DieuChinhChamCong { NhanVienID=hrStaff.ID,Ngay=BusinessDay(today,-4),GioVaoDeXuat=new(8,15,0),GioRaDeXuat=new(17,0,0),LyDo="Quên chấm công đầu ngày",TrangThai=ApprovalStatuses.Approved,NguoiDuyet="hr@tic.local",NgayDuyet=DateTime.UtcNow.AddDays(-2) });
            if (!await db.TaiLieuNoiBos.AnyAsync(x => x.TieuDe == "Quy trình bảo mật dữ liệu"))
                db.TaiLieuNoiBos.Add(new TaiLieuNoiBo { TieuDe="Quy trình bảo mật dữ liệu",DanhMuc="Quy trình",TomTat="Hướng dẫn lưu trữ, chia sẻ và bảo vệ dữ liệu nội bộ.",PhamVi="All",BatBuocXacNhan=true,DaPhatHanh=true,NgayPhatHanh=DateTime.UtcNow.AddDays(-3),NguoiTao="hr@tic.local" });
            if (!await db.ThongBaoNoiBos.AnyAsync(x => x.TieuDe == "Lịch đào tạo an toàn thông tin"))
                db.ThongBaoNoiBos.Add(new ThongBaoNoiBo { TieuDe="Lịch đào tạo an toàn thông tin",NoiDung="Buổi đào tạo bắt buộc diễn ra lúc 14:00 tại phòng đào tạo.",PhamVi="All",BatDau=DateTime.UtcNow.AddDays(-1),KetThuc=DateTime.UtcNow.AddDays(20),NguoiTao="hr@tic.local" });
            if (!await db.KhaoSatNoiBos.AnyAsync(x => x.TieuDe == "Khảo sát nhu cầu đào tạo"))
                db.KhaoSatNoiBos.Add(new KhaoSatNoiBo { TieuDe="Khảo sát nhu cầu đào tạo",CauHoi="Bạn muốn ưu tiên chủ đề đào tạo nào trong quý tới?",BatDau=DateTime.UtcNow.AddDays(-2),KetThuc=DateTime.UtcNow.AddDays(25),HoatDong=true,NguoiTao="hr@tic.local",LuaChons=[new(){NoiDung="Kỹ năng chuyên môn"},new(){NoiDung="Kỹ năng quản lý"},new(){NoiDung="Ngoại ngữ"}] });

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            logger.LogInformation("Da nang cap bo du lieu demo day du: moi phong ban co toi thieu hai nhan vien.");
        });
    }

    private static NhanVien Employee(string code, string name, string email, string phone, string citizenId, string taxId, bool gender, DateTime birthday, DateTime joined, PhongBan department, ChucVu position) =>
        new() { MaNhanVien = code, Ten = name, Email = email, SDT = phone, SoTaiKhoan = $"0011{citizenId[^8..]}", Gioitinh = gender, NgaySinh = birthday, CanCuocCongDan = citizenId, DiaChi = "TP. Hồ Chí Minh", MaSothue = taxId, Ngayvaolam = joined, PhongBanID = department.ID, ChucVuID = position.ID };

    private static HopDong Contract(NhanVien e, string number, string type, DateTime start, DateTime? end, decimal salary) =>
        new() { NhanVienID = e.ID, SoHopDong = number, Loaihopdong = type, Ngayky = start.AddDays(-5), Ngaybatdau = start, Ngayketthuc = end, Luongcoban = salary, Trangthai = true };

    private static NghiPhep Leave(NhanVien e, LoaiNghiPhep type, DateTime start, DateTime end, string reason, string status, string? approver = null, string? feedback = null) =>
        new() { NhanVienID = e.ID, LoaiNghiPhepID = type.ID, LoaiNghi = type.TenLoai, Ngaybatdau = start, NgayKetThuc = end, Lydo = reason, Trangthai = status, NguoiDuyet = approver, PhanHoi = feedback, NgayDuyet = approver is null ? null : DateTime.UtcNow.AddDays(-1), NgayTao = DateTime.UtcNow.AddDays(-3) };

    private static PhuCap Allowance(NhanVien e, string name, decimal amount, DateTime date) => new() { NhanVienID = e.ID, TenPhuCap = name, SoTien = amount, NgayApDung = date, GhiChu = "Dữ liệu demo" };
    private static ThuongPhat Reward(NhanVien e, string type, string reason, decimal amount, DateTime date) => new() { NhanVienID = e.ID, Loai = type, LyDo = reason, SoTien = amount, NgayApDung = date };

    private static Luong Payroll(NhanVien e, DateTime month, decimal salary, decimal allowance, decimal reward, decimal deduction, decimal insurance) =>
        new() { NhanVienID = e.ID, Ten = e.Ten, Thang = month.Month, Nam = month.Year, Luongcoban = salary, SoNgayCong = 22, SoNgayChuan = 22, Phucap = allowance, Thuong = reward, Khautru = deduction, BaoHiem = insurance, Tongluong = salary + allowance + reward - deduction - insurance, TrangThai = PayrollStatuses.Paid, NgayTinh = DateTime.UtcNow.AddMonths(-1), NgayDoiSoat = DateTime.UtcNow.AddDays(-15), NgayKhoa = DateTime.UtcNow.AddDays(-12), NguoiDoiSoat = "hr@tic.local", NguoiKhoa = "admin@tic.local" };

    private static QuyTrinhPheDuyet Workflow(string type, string name, params (int Order, string Role, string Name)[] steps) =>
        new() { LoaiYeuCau = type, TenQuyTrinh = name, CacBuoc = steps.Select(x => new BuocPheDuyet { ThuTu = x.Order, VaiTroDuyet = x.Role, TenBuoc = x.Name }).ToList() };

    private static async Task<LoaiBaoHiem[]> EnsureInsuranceTypes(AppDbContext db)
    {
        var names = new[] { "Bảo hiểm xã hội", "Bảo hiểm y tế", "Bảo hiểm thất nghiệp" };
        var existing = await db.LoaiBaoHiems.ToListAsync();
        foreach (var name in names)
            if (existing.All(x => x.TenLoaiBaoHiem != name)) db.LoaiBaoHiems.Add(new LoaiBaoHiem { TenLoaiBaoHiem = name });
        await db.SaveChangesAsync();
        var result = await db.LoaiBaoHiems.Where(x => names.Contains(x.TenLoaiBaoHiem)).ToListAsync();
        return names.Select(name => result.Single(x => x.TenLoaiBaoHiem == name)).ToArray();
    }

    private static void SeedAttendance(AppDbContext db, IReadOnlyList<NhanVien> employees, DateTime today)
    {
        var days = Enumerable.Range(0, today.Day).Select(x => today.AddDays(-x))
            .Where(x => x.Month == today.Month && x.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
            .Take(12).OrderBy(x => x).ToArray();
        for (var e = 0; e < employees.Count; e++)
            for (var d = 0; d < days.Length; d++)
            {
                var late = (e + d) % 7 == 0 ? 12 : 0;
                var status = d == 2 && e == 1 ? AttendanceStatuses.Remote : AttendanceStatuses.Present;
                db.ChamCongs.Add(new ChamCong { NhanVienID = employees[e].ID, Ngay = days[d], Giovaolam = days[d].AddHours(8).AddMinutes(late), Giotanlam = days[d].AddHours(17).AddMinutes(e % 2 == 0 ? 15 : 0), TrangThai = status, PhutDiMuon = late, GhiChu = status == AttendanceStatuses.Remote ? "Làm việc từ xa đã đăng ký" : "Dữ liệu chấm công demo" });
            }
    }

    private static DateTime BusinessDay(DateTime date, int offset)
    {
        var direction = Math.Sign(offset);
        for (var remaining = Math.Abs(offset); remaining > 0;)
        {
            date = date.AddDays(direction);
            if (date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday) remaining--;
        }
        return date;
    }

    private static async Task<TaiKhoan> LinkAccount(UserManager<TaiKhoan> users, string email, string password, NhanVien employee, string role)
    {
        var account = await users.FindByEmailAsync(email);
        if (account is null)
        {
            account = new TaiKhoan { UserName = email, Email = email, EmailConfirmed = true, MaNhanVien = employee.MaNhanVien, NhanVienID = employee.ID, Role = role };
            Ensure(await users.CreateAsync(account, password), $"tạo tài khoản {email}");
        }
        else
        {
            account.MaNhanVien = employee.MaNhanVien;
            account.NhanVienID = employee.ID;
            account.Role = role;
            account.EmailConfirmed = true;
            Ensure(await users.UpdateAsync(account), $"liên kết tài khoản {email}");
        }
        var currentRoles = await users.GetRolesAsync(account);
        if (currentRoles.Count > 0 && (currentRoles.Count != 1 || currentRoles[0] != role)) Ensure(await users.RemoveFromRolesAsync(account, currentRoles), $"xóa vai trò cũ của {email}");
        if (!await users.IsInRoleAsync(account, role)) Ensure(await users.AddToRoleAsync(account, role), $"gán vai trò {role} cho {email}");
        return account;
    }

    private static void Ensure(IdentityResult result, string action)
    {
        if (!result.Succeeded) throw new InvalidOperationException($"Không thể {action}: {string.Join("; ", result.Errors.Select(x => x.Description))}");
    }
}
