using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QLNS.Models.Entities;
using QLNS.Services;

namespace QLNS.Data;

/// <summary>
/// Bộ dữ liệu kiểm thử cố định cho một công ty nhỏ trong năm 2026.
/// Mọi khóa nghiệp vụ đều có tiền tố DEMO để có thể chạy lặp lại mà không sinh bản ghi trùng.
/// </summary>
public static class ComprehensiveCompanySeeder
{
    private const int DemoYear = 2026;
    private const string DefaultPassword = "Demo@123456";
    private const string CompletionMarker = "COMPREHENSIVE-COMPANY-2026-V1";

    private sealed record EmployeeDefinition(
        string Code, string Name, string Email, string Phone, string CitizenId, string TaxId,
        bool Gender, DateTime Birthday, DateTime Joined, string Department, string Position,
        decimal BaseSalary);

    public static async Task SeedAsync(AppDbContext db, UserManager<TaiKhoan> users, ILogger logger)
    {
        if (await db.NhatKyHeThongs.AnyAsync(x => x.DoiTuongID == CompletionMarker))
        {
            logger.LogInformation("Bộ dữ liệu công ty demo 2026 đã tồn tại.");
            return;
        }

        var departments = await EnsureDepartmentsAsync(db);
        var positions = await EnsurePositionsAsync(db);
        var employees = await EnsureEmployeesAsync(db, departments, positions);
        await AssignManagersAsync(db, departments, employees);
        var accounts = await EnsureAccountsAsync(users, employees);

        var demoFile = EnsureDemoImage();
        var contracts = await EnsureContractsAsync(db, employees, demoFile);
        var leaveTypes = await EnsureLeaveTypesAsync(db);
        await EnsureCoreEmployeeDataAsync(db, employees, leaveTypes, demoFile);
        await EnsureAugustAttendanceAsync(db, employees);
        await EnsureRequestsAsync(db, employees, leaveTypes, demoFile);
        await EnsurePoliciesAsync(db, departments, positions, employees);
        await EnsureInternalPortalAsync(db, departments, positions, accounts);
        await EnsureLegacyAndWorkflowCoverageAsync(db, departments, employees);
        await EnsurePayrollsAsync(db, employees, contracts);
        await EnsureAuditRowsAsync(db, employees);

        var warnings = await ValidateCoverageAsync(db);
        if (warnings.Count > 0)
            throw new InvalidOperationException($"Dữ liệu demo chưa đạt yêu cầu: {string.Join("; ", warnings)}");

        db.NhatKyHeThongs.Add(new NhatKyHeThong
        {
            HanhDong = "SeedComplete", DoiTuong = "DemoData", DoiTuongID = CompletionMarker,
            ChiTiet = $"Đã tạo dữ liệu đầy đủ cho {employees.Count} nhân viên; lương 05-07/2026 và công 08/2026.",
            NguoiThucHien = "system"
        });
        await db.SaveChangesAsync();
        logger.LogInformation("Đã bảo đảm bộ dữ liệu công ty demo đầy đủ cho {Count} nhân viên.", employees.Count);
    }

    private static async Task<Dictionary<string, PhongBan>> EnsureDepartmentsAsync(AppDbContext db)
    {
        var definitions = new (string Name, string? Parent)[]
        {
            ("Ban Giám đốc", null),
            ("Phòng Nhân sự", "Ban Giám đốc"),
            ("Phòng Công nghệ", "Ban Giám đốc"),
            ("Phòng Kinh doanh", "Ban Giám đốc"),
            ("Phòng Tài chính - Kế toán", "Ban Giám đốc"),
            ("Phòng Marketing", "Phòng Kinh doanh"),
            ("Phòng Chăm sóc khách hàng", "Phòng Kinh doanh"),
            ("Phòng Vận hành", "Ban Giám đốc"),
            ("Bộ phận Kiểm thử", "Phòng Công nghệ"),
            ("Bộ phận Sản phẩm", "Phòng Công nghệ")
        };

        var existing = await db.PhongBans.ToListAsync();
        foreach (var definition in definitions)
        {
            if (existing.Any(x => x.Tenphongban == definition.Name)) continue;
            var item = new PhongBan { Tenphongban = definition.Name };
            db.PhongBans.Add(item);
            existing.Add(item);
        }
        await db.SaveChangesAsync();

        var result = existing.Where(x => definitions.Any(d => d.Name == x.Tenphongban))
            .ToDictionary(x => x.Tenphongban);
        foreach (var (name, parent) in definitions.Where(x => x.Parent is not null))
            result[name].PhongBanChaID = result[parent!].ID;
        await db.SaveChangesAsync();
        return result;
    }

    private static async Task<Dictionary<string, ChucVu>> EnsurePositionsAsync(AppDbContext db)
    {
        var definitions = new (string Name, decimal Factor, string Roles)[]
        {
            ("Giám đốc", 3.00m, "NhanVien,Admin"),
            ("Phó giám đốc", 2.60m, "NhanVien,QuanLy"),
            ("Trưởng phòng", 2.20m, "NhanVien,QuanLy"),
            ("Trưởng phòng Nhân sự", 2.30m, "NhanVien,QuanLy,HR"),
            ("Chuyên viên nhân sự", 1.40m, "NhanVien,HR"),
            ("Lập trình viên", 1.60m, "NhanVien"),
            ("Nhân viên kinh doanh", 1.30m, "NhanVien"),
            ("Kế toán viên", 1.50m, "NhanVien"),
            ("Chuyên viên marketing", 1.40m, "NhanVien"),
            ("Chuyên viên chăm sóc khách hàng", 1.25m, "NhanVien"),
            ("Chuyên viên vận hành", 1.45m, "NhanVien"),
            ("Kiểm thử viên", 1.50m, "NhanVien"),
            ("Chuyên viên sản phẩm", 1.70m, "NhanVien")
        };

        var existing = await db.ChucVus.ToListAsync();
        foreach (var definition in definitions)
        {
            var item = existing.FirstOrDefault(x => x.Chucvu == definition.Name);
            if (item is null)
            {
                item = new ChucVu { Chucvu = definition.Name };
                db.ChucVus.Add(item);
                existing.Add(item);
            }
            item.Hesoluong = definition.Factor;
            item.PhanQuyen = definition.Roles;
        }
        await db.SaveChangesAsync();
        return existing.Where(x => definitions.Any(d => d.Name == x.Chucvu)).ToDictionary(x => x.Chucvu);
    }

    private static EmployeeDefinition[] EmployeeDefinitions() =>
    [
        new("GD-DEMO-001", "Nguyễn Đức Minh", "minh.nguyen@tic.local", "0901000005", "079085000005", "DEMO000005", true, new(1985,6,10), new(2018,1,2), "Ban Giám đốc", "Giám đốc", 35_000_000),
        new("GD-DEMO-002", "Vũ Thanh Mai", "mai.vu@tic.local", "0901000006", "079190000006", "DEMO000006", false, new(1990,3,18), new(2020,2,3), "Ban Giám đốc", "Phó giám đốc", 30_000_000),
        new("HR-DEMO-001", "Lê Thu Hà", "hr@tic.local", "0901000003", "079194000003", "DEMO000003", false, new(1994,2,15), new(2021,3,1), "Phòng Nhân sự", "Trưởng phòng Nhân sự", 22_000_000),
        new("HR-DEMO-002", "Trương Ngọc Lan", "lan.truong@tic.local", "0901000008", "079096000008", "DEMO000008", false, new(1996,12,2), new(2022,4,4), "Phòng Nhân sự", "Chuyên viên nhân sự", 16_000_000),
        new("QL-DEMO-001", "Trần Minh Quân", "quanly@tic.local", "0901000001", "079088000001", "DEMO000001", true, new(1988,4,12), new(2019,5,6), "Phòng Công nghệ", "Trưởng phòng", 28_000_000),
        new("NV-DEMO-001", "Nguyễn Hoàng An", "nhanvien@tic.local", "0901000002", "079097000002", "DEMO000002", true, new(1997,9,21), new(2023,6,5), "Phòng Công nghệ", "Lập trình viên", 18_000_000),
        new("QL-KD-DEMO-001", "Đỗ Quốc Khánh", "khanh.do@tic.local", "0901000007", "079089000007", "DEMO000007", true, new(1989,8,24), new(2020,7,6), "Phòng Kinh doanh", "Trưởng phòng", 24_000_000),
        new("NV-DEMO-002", "Phạm Gia Bình", "binh.pham@tic.local", "0901000004", "079099000004", "DEMO000004", true, new(1999,11,8), new(2025,8,1), "Phòng Kinh doanh", "Nhân viên kinh doanh", 14_000_000),
        new("QL-TC-001", "Nguyễn Thị Hương", "huong.nguyen@tic.local", "0902000001", "079188100001", "DEMO100001", false, new(1988,1,15), new(2020,1,6), "Phòng Tài chính - Kế toán", "Trưởng phòng", 25_000_000),
        new("NV-TC-001", "Trần Quốc Bảo", "bao.tran@tic.local", "0902000002", "079095100002", "DEMO100002", true, new(1995,5,20), new(2022,2,7), "Phòng Tài chính - Kế toán", "Kế toán viên", 15_500_000),
        new("QL-MKT-001", "Lê Ngọc Anh", "anh.le@tic.local", "0902000003", "079191100003", "DEMO100003", false, new(1991,7,11), new(2021,3,8), "Phòng Marketing", "Trưởng phòng", 23_000_000),
        new("NV-MKT-001", "Phan Minh Châu", "chau.phan@tic.local", "0902000004", "079098100004", "DEMO100004", false, new(1998,10,9), new(2023,4,3), "Phòng Marketing", "Chuyên viên marketing", 14_500_000),
        new("QL-CSKH-001", "Hoàng Thanh Tùng", "tung.hoang@tic.local", "0902000005", "079090100005", "DEMO100005", true, new(1990,9,3), new(2020,5,4), "Phòng Chăm sóc khách hàng", "Trưởng phòng", 21_000_000),
        new("NV-CSKH-001", "Đặng Mỹ Linh", "linh.dang@tic.local", "0902000006", "079099100006", "DEMO100006", false, new(1999,2,17), new(2024,6,3), "Phòng Chăm sóc khách hàng", "Chuyên viên chăm sóc khách hàng", 13_000_000),
        new("QL-VH-001", "Bùi Anh Tuấn", "tuan.bui@tic.local", "0902000007", "079087100007", "DEMO100007", true, new(1987,12,5), new(2019,7,1), "Phòng Vận hành", "Trưởng phòng", 24_500_000),
        new("NV-VH-001", "Võ Thu Trang", "trang.vo@tic.local", "0902000008", "079096100008", "DEMO100008", false, new(1996,6,14), new(2022,8,1), "Phòng Vận hành", "Chuyên viên vận hành", 15_000_000),
        new("QL-QA-001", "Đinh Quốc Việt", "viet.dinh@tic.local", "0902000009", "079089100009", "DEMO100009", true, new(1989,4,28), new(2020,9,7), "Bộ phận Kiểm thử", "Trưởng phòng", 22_500_000),
        new("NV-QA-001", "Ngô Hải Yến", "yen.ngo@tic.local", "0902000010", "079097100010", "DEMO100010", false, new(1997,8,19), new(2023,10,2), "Bộ phận Kiểm thử", "Kiểm thử viên", 16_000_000),
        new("QL-SP-001", "Phạm Đức Long", "long.pham@tic.local", "0902000011", "079088100011", "DEMO100011", true, new(1988,11,23), new(2020,11,2), "Bộ phận Sản phẩm", "Trưởng phòng", 26_000_000),
        new("NV-SP-001", "Trịnh Khánh Vy", "vy.trinh@tic.local", "0902000012", "079098100012", "DEMO100012", false, new(1998,3,30), new(2024,1,8), "Bộ phận Sản phẩm", "Chuyên viên sản phẩm", 17_500_000)
    ];

    private static async Task<List<NhanVien>> EnsureEmployeesAsync(
        AppDbContext db, IReadOnlyDictionary<string, PhongBan> departments,
        IReadOnlyDictionary<string, ChucVu> positions)
    {
        var definitions = EmployeeDefinitions();
        var codes = definitions.Select(x => x.Code).ToArray();
        var existing = await db.NhanViens.Where(x => codes.Contains(x.MaNhanVien)).ToListAsync();
        foreach (var definition in definitions)
        {
            var employee = existing.FirstOrDefault(x => x.MaNhanVien == definition.Code);
            if (employee is null)
            {
                employee = new NhanVien { MaNhanVien = definition.Code };
                db.NhanViens.Add(employee);
                existing.Add(employee);
            }
            employee.Ten = definition.Name;
            employee.Email = definition.Email;
            employee.SDT = definition.Phone;
            employee.CanCuocCongDan = definition.CitizenId;
            employee.MaSothue = definition.TaxId;
            employee.SoTaiKhoan = $"0011{definition.CitizenId[^8..]}";
            employee.Gioitinh = definition.Gender;
            employee.NgaySinh = definition.Birthday;
            employee.Ngayvaolam = definition.Joined;
            employee.DiaChi = $"Quận {(Array.IndexOf(definitions, definition) % 12) + 1}, TP. Hồ Chí Minh";
            employee.PhongBanID = departments[definition.Department].ID;
            employee.ChucVuID = positions[definition.Position].ID;
            employee.PhongBan = departments[definition.Department];
            employee.ChucVu = positions[definition.Position];
            employee.Trangthai = true;
            employee.Ngayxoa = null;
            employee.Ngaycapnhat = DateTime.UtcNow;
        }
        await db.SaveChangesAsync();
        return definitions.Select(d => existing.Single(x => x.MaNhanVien == d.Code)).ToList();
    }

    private static async Task AssignManagersAsync(
        AppDbContext db, IReadOnlyDictionary<string, PhongBan> departments, IReadOnlyList<NhanVien> employees)
    {
        var assignments = new Dictionary<string, string>
        {
            ["Ban Giám đốc"] = "GD-DEMO-001", ["Phòng Nhân sự"] = "HR-DEMO-001",
            ["Phòng Công nghệ"] = "QL-DEMO-001", ["Phòng Kinh doanh"] = "QL-KD-DEMO-001",
            ["Phòng Tài chính - Kế toán"] = "QL-TC-001", ["Phòng Marketing"] = "QL-MKT-001",
            ["Phòng Chăm sóc khách hàng"] = "QL-CSKH-001", ["Phòng Vận hành"] = "QL-VH-001",
            ["Bộ phận Kiểm thử"] = "QL-QA-001", ["Bộ phận Sản phẩm"] = "QL-SP-001"
        };
        foreach (var assignment in assignments)
            departments[assignment.Key].Matruongphong = employees.Single(x => x.MaNhanVien == assignment.Value).ID;
        await db.SaveChangesAsync();
    }

    private static async Task<List<TaiKhoan>> EnsureAccountsAsync(UserManager<TaiKhoan> users, IReadOnlyList<NhanVien> employees)
    {
        var result = new List<TaiKhoan>();
        foreach (var employee in employees)
        {
            var email = employee.Email!;
            var account = await users.FindByEmailAsync(email);
            if (account is null)
            {
                account = new TaiKhoan
                {
                    UserName = email, Email = email, EmailConfirmed = true,
                    MaNhanVien = employee.MaNhanVien, NhanVienID = employee.ID,
                    Role = PrimaryRole(employee)
                };
                Ensure(await users.CreateAsync(account, DefaultPassword), $"tạo tài khoản {email}");
            }
            else
            {
                account.UserName = email;
                account.EmailConfirmed = true;
                account.MaNhanVien = employee.MaNhanVien;
                account.NhanVienID = employee.ID;
                account.Role = PrimaryRole(employee);
                Ensure(await users.UpdateAsync(account), $"cập nhật tài khoản {email}");
            }

            var desiredRoles = AuthorizationRoles.Parse(employee.ChucVu?.PhanQuyen);
            if (desiredRoles.Count == 1 && employee.ChucVu is null)
                desiredRoles = [AuthorizationRoles.Employee];
            var currentRoles = await users.GetRolesAsync(account);
            if (currentRoles.Count > 0 && !currentRoles.ToHashSet(StringComparer.Ordinal).SetEquals(desiredRoles))
                Ensure(await users.RemoveFromRolesAsync(account, currentRoles), $"xóa quyền cũ {email}");
            currentRoles = await users.GetRolesAsync(account);
            var missing = desiredRoles.Except(currentRoles).ToArray();
            if (missing.Length > 0)
                Ensure(await users.AddToRolesAsync(account, missing), $"gán quyền {email}");
            result.Add(account);
        }
        return result;
    }

    private static string PrimaryRole(NhanVien employee) =>
        AuthorizationRoles.All.Reverse().First(AuthorizationRoles.Parse(employee.ChucVu?.PhanQuyen).Contains);

    private static async Task<Dictionary<int, HopDong>> EnsureContractsAsync(
        AppDbContext db, IReadOnlyList<NhanVien> employees, (string Path, long Size) demoFile)
    {
        var definitions = EmployeeDefinitions().ToDictionary(x => x.Code);
        var all = await db.HopDongs.Include(x => x.TaiLieuHopDongs).ToListAsync();
        var types = new[] { ContractTypes.IndefiniteTerm, ContractTypes.FixedTerm, ContractTypes.Probation, ContractTypes.Collaborator, ContractTypes.VocationalTraining };
        var result = new Dictionary<int, HopDong>();
        for (var index = 0; index < employees.Count; index++)
        {
            var employee = employees[index];
            var contract = all.Where(x => x.NhanVienID == employee.ID && x.Ngaybatdau <= new DateTime(DemoYear, 8, 31) && (!x.Ngayketthuc.HasValue || x.Ngayketthuc >= new DateTime(DemoYear, 5, 1)))
                .OrderByDescending(x => x.Ngaybatdau).FirstOrDefault();
            if (contract is null)
            {
                var type = types[index % types.Length];
                contract = new HopDong
                {
                    NhanVienID = employee.ID, SoHopDong = $"HD-2026-{employee.MaNhanVien}",
                    Loaihopdong = type, Ngayky = new(2026, 1, 2), Ngaybatdau = new(2026, 1, 5),
                    Ngayketthuc = type == ContractTypes.IndefiniteTerm ? null : new DateTime(2027, 12, 31),
                    Luongcoban = definitions[employee.MaNhanVien].BaseSalary, Trangthai = true
                };
                db.HopDongs.Add(contract);
                all.Add(contract);
                await db.SaveChangesAsync();
            }
            contract.Luongcoban = definitions[employee.MaNhanVien].BaseSalary;
            result[employee.ID] = contract;
            if (contract.TaiLieuHopDongs.Count == 0)
                db.TaiLieuHopDongs.Add(new TaiLieuHopDong
                {
                    HopDongID = contract.ID, TenFileGoc = $"hop-dong-{employee.MaNhanVien}.png",
                    DuongDanFile = demoFile.Path, ContentType = "image/png", KichThuoc = demoFile.Size
                });
        }
        await db.SaveChangesAsync();
        return result;
    }

    private static async Task<List<LoaiNghiPhep>> EnsureLeaveTypesAsync(AppDbContext db)
    {
        var definitions = new (string Code, string Name, bool Paid, bool Deduct)[]
        {
            ("PHEP_NAM", "Nghỉ phép năm", true, true), ("NGHI_OM", "Nghỉ ốm", true, false),
            ("VIEC_RIENG", "Nghỉ việc riêng", false, false), ("KHONG_LUONG", "Nghỉ không lương", false, false),
            ("THAI_SAN", "Nghỉ thai sản", true, false), ("KET_HON", "Nghỉ kết hôn", true, false),
            ("TANG_CHE", "Nghỉ tang chế", true, false), ("CHAM_CON", "Nghỉ chăm con ốm", true, false),
            ("TAI_NAN", "Nghỉ tai nạn lao động", true, false), ("KHAM_THAI", "Nghỉ khám thai", true, false)
        };
        var existing = await db.LoaiNghiPheps.ToListAsync();
        foreach (var definition in definitions)
        {
            var item = existing.FirstOrDefault(x => x.MaLoai == definition.Code);
            if (item is null)
            {
                item = new LoaiNghiPhep { MaLoai = definition.Code };
                db.LoaiNghiPheps.Add(item); existing.Add(item);
            }
            item.TenLoai = definition.Name; item.HuongLuong = definition.Paid;
            item.TruQuyPhep = definition.Deduct; item.HoatDong = true;
        }
        await db.SaveChangesAsync();
        return existing.Where(x => definitions.Any(d => d.Code == x.MaLoai)).ToList();
    }

    private static async Task EnsureCoreEmployeeDataAsync(
        AppDbContext db, IReadOnlyList<NhanVien> employees, IReadOnlyList<LoaiNghiPhep> leaveTypes,
        (string Path, long Size) demoFile)
    {
        var annual = leaveTypes.Single(x => x.MaLoai == "PHEP_NAM");
        var insuranceNames = new[]
        {
            "Bảo hiểm xã hội", "Bảo hiểm y tế", "Bảo hiểm thất nghiệp", "Bảo hiểm tai nạn",
            "Bảo hiểm sức khỏe", "Bảo hiểm nhân thọ", "Bảo hiểm thai sản", "Bảo hiểm nha khoa",
            "Bảo hiểm du lịch công tác", "Bảo hiểm hưu trí bổ sung"
        };
        var insuranceTypes = await db.LoaiBaoHiems.ToListAsync();
        foreach (var name in insuranceNames)
            if (insuranceTypes.All(x => x.TenLoaiBaoHiem != name))
            {
                var item = new LoaiBaoHiem { TenLoaiBaoHiem = name };
                db.LoaiBaoHiems.Add(item); insuranceTypes.Add(item);
            }
        await db.SaveChangesAsync();

        for (var i = 0; i < employees.Count; i++)
        {
            var employee = employees[i];
            if (!await db.SoDuNghiPheps.AnyAsync(x => x.NhanVienID == employee.ID && x.LoaiNghiPhepID == annual.ID && x.Nam == DemoYear))
                db.SoDuNghiPheps.Add(new SoDuNghiPhep { NhanVienID = employee.ID, LoaiNghiPhepID = annual.ID, Nam = DemoYear, SoNgayDuocHuong = 12 + i % 5, DieuChinh = i % 4 == 0 ? 1 : 0, GhiChu = "Quỹ phép năm 2026" });
            if (!await db.TrinhDoChungChis.AnyAsync(x => x.NhanVienID == employee.ID && x.SoHieu == $"CC-DEMO-{i + 1:000}"))
                db.TrinhDoChungChis.Add(new TrinhDoChungChi { NhanVienID = employee.ID, Loai = i % 2 == 0 ? "Bằng cấp" : "Chứng chỉ", Ten = i % 2 == 0 ? "Bằng đại học chuyên ngành" : "Chứng chỉ nghiệp vụ chuyên môn", DonViCap = i % 2 == 0 ? "Đại học Quốc gia" : "Viện Đào tạo TIC", NgayCap = new DateTime(2020 + i % 5, 6, 15), NgayHetHan = i % 3 == 0 ? new DateTime(2027, 6, 15) : null, SoHieu = $"CC-DEMO-{i + 1:000}" });
            if (!await db.QuaTrinhCongTacs.AnyAsync(x => x.NhanVienID == employee.ID && x.DonVi == "Công ty trước đây (demo)"))
                db.QuaTrinhCongTacs.Add(new QuaTrinhCongTac { NhanVienID = employee.ID, DonVi = "Công ty trước đây (demo)", ChucDanh = employee.ChucVu?.Chucvu ?? "Nhân viên", TuNgay = new DateTime(2017 + i % 4, 1, 1), DenNgay = employee.Ngayvaolam.AddDays(-1), MoTa = "Kinh nghiệm trước khi gia nhập TIC" });
            if (!await db.TaiLieuNhanViens.AnyAsync(x => x.NhanVienID == employee.ID && x.TenTaiLieu == "Hồ sơ nhân viên demo"))
                db.TaiLieuNhanViens.Add(new TaiLieuNhanVien { NhanVienID = employee.ID, TenTaiLieu = "Hồ sơ nhân viên demo", TenFileGoc = $"ho-so-{employee.MaNhanVien}.png", DuongDan = demoFile.Path, ContentType = "image/png", KichThuoc = demoFile.Size, NguoiTaiLen = "hr@tic.local" });
            if (!await db.PhuCaps.AnyAsync(x => x.NhanVienID == employee.ID && x.TenPhuCap == "Phụ cấp cá nhân demo"))
                db.PhuCaps.Add(new PhuCap { NhanVienID = employee.ID, TenPhuCap = "Phụ cấp cá nhân demo", SoTien = 300_000 + i % 5 * 100_000, NgayApDung = new DateTime(DemoYear, 1, 1), GhiChu = "Khoản cá nhân để kiểm thử" });

            foreach (var (type, index) in insuranceTypes.Take(3).Select((x, index) => (x, index)))
                if (!await db.BaoHiems.AnyAsync(x => x.NhanVienID == employee.ID && x.LoaiBaoHiemID == type.MaLoaiBaoHiem && x.NgayBatDau == new DateTime(DemoYear, 1, 1)))
                    db.BaoHiems.Add(new BaoHiem { NhanVienID = employee.ID, LoaiBaoHiemID = type.MaLoaiBaoHiem, SoTienDong = new[] { 1_440_000m, 270_000m, 180_000m }[index], NgayBatDau = new DateTime(DemoYear, 1, 1), TrangThai = i == 19 && index == 2 ? "NgungDong" : "DangDong" });

            var adjustmentTypes = new[] { "Thuong", "Phat", "ThuNhapKhac" };
            foreach (var (type, index) in adjustmentTypes.Select((x, index) => (x, index)))
                if (!await db.ThuongPhats.AnyAsync(x => x.NhanVienID == employee.ID && x.Loai == type && x.NgayApDung == new DateTime(DemoYear, 7, 20 + index)))
                    db.ThuongPhats.Add(new ThuongPhat { NhanVienID = employee.ID, Loai = type, LyDo = type == "Thuong" ? "Hoàn thành KPI" : type == "Phat" ? "Vi phạm nội quy demo" : "Hỗ trợ dự án", SoTien = type == "Thuong" ? 1_000_000 + i * 50_000 : type == "Phat" ? 100_000 + i * 10_000 : 500_000 + i * 20_000, NgayApDung = new DateTime(DemoYear, 7, 20 + index) });
        }
        await db.SaveChangesAsync();
    }

    private static async Task EnsureAugustAttendanceAsync(AppDbContext db, IReadOnlyList<NhanVien> employees)
    {
        var days = Enumerable.Range(1, 31).Select(day => new DateTime(DemoYear, 8, day))
            .Where(x => x.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday).ToArray();
        var employeeIds = employees.Select(x => x.ID).ToArray();
        var existing = await db.ChamCongs.Where(x => employeeIds.Contains(x.NhanVienID) && x.Ngay >= new DateTime(DemoYear, 8, 1) && x.Ngay < new DateTime(DemoYear, 9, 1)).ToListAsync();

        for (var employeeIndex = 0; employeeIndex < employees.Count; employeeIndex++)
        for (var dayIndex = 0; dayIndex < days.Length; dayIndex++)
        {
            var employee = employees[employeeIndex];
            var day = days[dayIndex];
            if (existing.Any(x => x.NhanVienID == employee.ID && x.Ngay == day)) continue;
            var item = new ChamCong { NhanVienID = employee.ID, Ngay = day, TrangThai = AttendanceStatuses.Present };
            if (dayIndex == 4 && employeeIndex % 7 == 3)
            {
                item.TrangThai = AttendanceStatuses.PaidLeave;
            }
            else if (dayIndex == 9 && employeeIndex % 7 == 4)
            {
                item.TrangThai = AttendanceStatuses.UnpaidLeave;
            }
            else if (dayIndex == 14 && employeeIndex % 7 == 5)
            {
                item.TrangThai = AttendanceStatuses.Absent;
            }
            else
            {
                item.TrangThai = dayIndex == 6 && employeeIndex % 5 == 0 ? AttendanceStatuses.Remote : AttendanceStatuses.Present;
                var late = dayIndex == 2 && employeeIndex % 4 == 0;
                item.Giovaolam = day.AddHours(8).AddMinutes(late ? 20 : employeeIndex % 3 * 5);
                item.Giotanlam = day.AddHours(17).AddMinutes(employeeIndex % 2 * 15);
                AttendancePolicy.ApplyLateRule(item);
            }
            db.ChamCongs.Add(item);
        }
        await db.SaveChangesAsync();
    }

    private static async Task EnsureRequestsAsync(
        AppDbContext db, IReadOnlyList<NhanVien> employees, IReadOnlyList<LoaiNghiPhep> leaveTypes,
        (string Path, long Size) demoFile)
    {
        var statuses = new[] { ApprovalStatuses.Pending, ApprovalStatuses.ManagerApproved, ApprovalStatuses.Approved, ApprovalStatuses.Rejected, LeaveStatuses.Cancelled };
        var augustAttendance = await db.ChamCongs.Where(x => x.Ngay >= new DateTime(DemoYear, 8, 1) && x.Ngay < new DateTime(DemoYear, 9, 1)).ToListAsync();
        for (var i = 0; i < employees.Count; i++)
        {
            var employee = employees[i];
            var leaveDate = new DateTime(DemoYear, 9, 7).AddDays(i);
            var leaveStatus = statuses[i % statuses.Length];
            if (!await db.NghiPheps.AnyAsync(x => x.NhanVienID == employee.ID && x.Ngaybatdau == leaveDate))
                db.NghiPheps.Add(new NghiPhep { NhanVienID = employee.ID, LoaiNghiPhepID = leaveTypes[i % leaveTypes.Count].ID, LoaiNghi = leaveTypes[i % leaveTypes.Count].TenLoai, Ngaybatdau = leaveDate, NgayKetThuc = leaveDate.AddDays(i % 3), Lydo = $"Tình huống nghỉ phép demo {i + 1}", Trangthai = leaveStatus, LaDotXuat = i % 6 == 0, SoNgayHuongLuong = i % 4 == 0 ? 1 : 0, SoNgayKhongLuong = i % 4 == 0 ? 1 : 0, TenFileMinhChung = "minh-chung-demo.png", DuongDanMinhChung = demoFile.Path, ContentTypeMinhChung = "image/png", MinhChungs = [new NghiPhepMinhChung { TenFileGoc = "minh-chung-demo.png", DuongDan = demoFile.Path, ContentType = "image/png", KichThuoc = demoFile.Size }], PhanHoi = leaveStatus == ApprovalStatuses.Rejected ? "Minh chứng chưa phù hợp" : leaveStatus == ApprovalStatuses.Approved ? "Đã xác nhận" : null, NguoiDuyet = leaveStatus is ApprovalStatuses.Approved or ApprovalStatuses.Rejected ? "hr@tic.local" : null, NgayDuyet = leaveStatus is ApprovalStatuses.Approved or ApprovalStatuses.Rejected ? DateTime.UtcNow : null });

            var overtimeDate = new DateTime(DemoYear, 8, 3).AddDays(i);
            var overtimeStatus = statuses[i % 4];
            if (!await db.DangKyTangCas.AnyAsync(x => x.NhanVienID == employee.ID && x.Ngay == overtimeDate && x.LyDo.StartsWith("OT demo")))
                db.DangKyTangCas.Add(new DangKyTangCa { NhanVienID = employee.ID, Ngay = overtimeDate, GioBatDau = new(18, 0, 0), GioKetThuc = new(20 + i % 3, 0, 0), HeSo = 2m, SoGioDuyet = overtimeStatus == ApprovalStatuses.Approved ? 2 + i % 3 : 0, LyDo = $"OT demo hoàn thiện công việc {i + 1}", TrangThai = overtimeStatus, TenFileMinhChung = "minh-chung-ot.png", DuongDanMinhChung = demoFile.Path, ContentTypeMinhChung = "image/png", MinhChungs = [new DangKyTangCaMinhChung { TenFileGoc = "minh-chung-ot.png", DuongDan = demoFile.Path, ContentType = "image/png", KichThuoc = demoFile.Size }], PhanHoi = overtimeStatus == ApprovalStatuses.Rejected ? "Không đúng kế hoạch" : null, NguoiDuyet = overtimeStatus is ApprovalStatuses.Approved or ApprovalStatuses.Rejected ? "hr@tic.local" : null, NgayDuyet = overtimeStatus is ApprovalStatuses.Approved or ApprovalStatuses.Rejected ? DateTime.UtcNow : null });

            if (i < 12)
            {
                var attendance = augustAttendance.Where(x => x.NhanVienID == employee.ID).OrderBy(x => x.Ngay).Skip(i % 10).First();
                if (!await db.DieuChinhChamCongs.AnyAsync(x => x.NhanVienID == employee.ID && x.LyDo == $"Yêu cầu sửa công demo {i + 1}"))
                    db.DieuChinhChamCongs.Add(new DieuChinhChamCong { ChamCongID = attendance.ID, NhanVienID = employee.ID, Ngay = attendance.Ngay, GioVaoDeXuat = new TimeSpan(8, 0, 0), GioRaDeXuat = new TimeSpan(17, 30, 0), LyDo = $"Yêu cầu sửa công demo {i + 1}", TrangThai = statuses[i % 4], PhanHoi = i % 4 == 3 ? "Ảnh chưa rõ" : i % 4 == 2 ? "Đã đối chiếu" : null, NguoiDuyet = i % 4 >= 2 ? "hr@tic.local" : null, NgayDuyet = i % 4 >= 2 ? DateTime.UtcNow : null, MinhChungs = [new DieuChinhChamCongMinhChung { TenFileGoc = $"sua-cong-{i + 1}.png", DuongDan = demoFile.Path, ContentType = "image/png", KichThuoc = demoFile.Size }] });
                if (!await db.YeuCauCapNhatThongTinCaNhans.AnyAsync(x => x.NhanVienID == employee.ID && x.LyDo == $"Cập nhật hồ sơ demo {i + 1}"))
                    db.YeuCauCapNhatThongTinCaNhans.Add(new YeuCauCapNhatThongTinCaNhan { NhanVienID = employee.ID, Email = employee.Email, SDT = $"0913{i + 1:000000}", NgaySinh = employee.NgaySinh, GioiTinh = employee.Gioitinh, CanCuocCongDan = employee.CanCuocCongDan, DiaChi = $"Địa chỉ đề xuất mới {i + 1}", SoTaiKhoan = employee.SoTaiKhoan, MaSoThue = employee.MaSothue, LyDo = $"Cập nhật hồ sơ demo {i + 1}", TrangThai = statuses[i % 4], PhanHoi = i % 4 == 3 ? "Thông tin chưa hợp lệ" : i % 4 == 2 ? "Đã cập nhật" : null, NguoiDuyet = i % 4 >= 2 ? "hr@tic.local" : null, NgayDuyet = i % 4 >= 2 ? DateTime.UtcNow : null });
            }

            foreach (var sequence in Enumerable.Range(1, 2))
            {
                var reference = $"DEMO-NOTIFY-{employee.MaNhanVien}-{sequence}";
                if (!await db.ThongBaoCaNhans.AnyAsync(x => x.NhanVienID == employee.ID && x.MaThamChieu == reference))
                    db.ThongBaoCaNhans.Add(new ThongBaoCaNhan { NhanVienID = employee.ID, Loai = sequence == 1 ? "RequestStatus" : "Payroll", TieuDe = sequence == 1 ? "Yêu cầu đã được cập nhật" : "Đã có bảng lương", NoiDung = sequence == 1 ? "Yêu cầu demo của bạn đã thay đổi trạng thái." : "Bảng lương tháng 07/2026 đã được phát hành.", MaThamChieu = reference, DuongDan = sequence == 1 ? "/Employee/Home" : "/Employee/Home", DaDoc = i % 3 == 0, NgayDoc = i % 3 == 0 ? DateTime.UtcNow : null });
            }
        }
        await db.SaveChangesAsync();
    }

    private static async Task EnsurePoliciesAsync(
        AppDbContext db, IReadOnlyDictionary<string, PhongBan> departments,
        IReadOnlyDictionary<string, ChucVu> positions, IReadOnlyList<NhanVien> employees)
    {
        for (var i = 1; i <= 10; i++)
        {
            var scope = (i % 4) switch { 0 => AllowanceScopes.Company, 1 => AllowanceScopes.Department, 2 => AllowanceScopes.Position, _ => AllowanceScopes.Employee };
            var allowanceName = $"Chính sách phụ cấp demo {i:00}";
            if (!await db.ChinhSachPhuCaps.AnyAsync(x => x.TenPhuCap == allowanceName))
                db.ChinhSachPhuCaps.Add(new ChinhSachPhuCap { TenPhuCap = allowanceName, SoTien = 200_000 + i * 50_000, PhamVi = scope, PhongBanID = scope == AllowanceScopes.Department ? departments.Values.ElementAt(i % departments.Count).ID : null, ChucVuID = scope == AllowanceScopes.Position ? positions.Values.ElementAt(i % positions.Count).ID : null, NhanVienID = scope == AllowanceScopes.Employee ? employees[i % employees.Count].ID : null, NgayBatDau = new DateTime(DemoYear, 1 + i % 6, 1), KieuThoiHan = i % 3 == 0 ? AllowanceDurations.FixedMonths : AllowanceDurations.Monthly, SoThang = i % 3 == 0 ? 3 : null, HoatDong = i != 10 });
            var insuranceName = $"Chính sách BHXH demo {i:00}";
            if (!await db.ChinhSachBhxhs.AnyAsync(x => x.TenKhoan == insuranceName))
                db.ChinhSachBhxhs.Add(new ChinhSachBhxh { TenKhoan = insuranceName, SoTienDong = 150_000 + i * 40_000, PhamVi = scope, PhongBanID = scope == AllowanceScopes.Department ? departments.Values.ElementAt(i % departments.Count).ID : null, ChucVuID = scope == AllowanceScopes.Position ? positions.Values.ElementAt(i % positions.Count).ID : null, NhanVienID = scope == AllowanceScopes.Employee ? employees[i % employees.Count].ID : null, NgayBatDau = new DateTime(DemoYear, 1 + i % 6, 1), KieuThoiHan = i % 2 == 0 ? AllowanceDurations.FixedMonths : AllowanceDurations.Monthly, SoThang = i % 2 == 0 ? 6 : null, HoatDong = i != 9 });
        }
        await db.SaveChangesAsync();
    }

    private static async Task EnsureInternalPortalAsync(
        AppDbContext db, IReadOnlyDictionary<string, PhongBan> departments,
        IReadOnlyDictionary<string, ChucVu> positions, IReadOnlyList<TaiKhoan> accounts)
    {
        var scopes = new[] { "All", "Department", "Position", "Role" };
        for (var i = 1; i <= 10; i++)
        {
            var scope = scopes[(i - 1) % scopes.Length];
            var documentTitle = $"Tài liệu nội bộ demo {i:00}";
            if (!await db.TaiLieuNoiBos.AnyAsync(x => x.TieuDe == documentTitle))
                db.TaiLieuNoiBos.Add(new TaiLieuNoiBo { TieuDe = documentTitle, DanhMuc = i % 2 == 0 ? "Quy trình" : "Chính sách", TomTat = $"Nội dung tài liệu kiểm thử số {i}.", PhamVi = scope, PhongBanID = scope == "Department" ? departments.Values.ElementAt(i % departments.Count).ID : null, ChucVuID = scope == "Position" ? positions.Values.ElementAt(i % positions.Count).ID : null, VaiTro = scope == "Role" ? AuthorizationRoles.Employee : null, BatBuocXacNhan = i % 2 == 0, DaPhatHanh = i != 10, NgayPhatHanh = DateTime.UtcNow.AddDays(-i), NguoiTao = "hr@tic.local" });
            var announcementTitle = $"Thông báo nội bộ demo {i:00}";
            if (!await db.ThongBaoNoiBos.AnyAsync(x => x.TieuDe == announcementTitle))
                db.ThongBaoNoiBos.Add(new ThongBaoNoiBo { TieuDe = announcementTitle, NoiDung = $"Thông báo kiểm thử tình huống số {i}.", PhamVi = scope, PhongBanID = scope == "Department" ? departments.Values.ElementAt(i % departments.Count).ID : null, ChucVuID = scope == "Position" ? positions.Values.ElementAt(i % positions.Count).ID : null, VaiTro = scope == "Role" ? AuthorizationRoles.Employee : null, Ghim = i <= 2, BatDau = DateTime.UtcNow.AddDays(-i), KetThuc = i == 10 ? DateTime.UtcNow.AddDays(-1) : DateTime.UtcNow.AddDays(30 - i), NguoiTao = "hr@tic.local" });
            var surveyTitle = $"Khảo sát nội bộ demo {i:00}";
            if (!await db.KhaoSatNoiBos.AnyAsync(x => x.TieuDe == surveyTitle))
                db.KhaoSatNoiBos.Add(new KhaoSatNoiBo { TieuDe = surveyTitle, CauHoi = $"Bạn chọn phương án nào cho tình huống {i}?", BatDau = DateTime.UtcNow.AddDays(-5), KetThuc = DateTime.UtcNow.AddDays(i == 10 ? -1 : 20), HoatDong = i != 10, NguoiTao = "hr@tic.local", LuaChons = [new() { NoiDung = "Rất tốt" }, new() { NoiDung = "Tốt" }, new() { NoiDung = "Cần cải thiện" }] });
        }
        await db.SaveChangesAsync();

        var documents = await db.TaiLieuNoiBos.Where(x => x.TieuDe.StartsWith("Tài liệu nội bộ demo")).OrderBy(x => x.ID).Take(10).ToListAsync();
        var announcements = await db.ThongBaoNoiBos.Where(x => x.TieuDe.StartsWith("Thông báo nội bộ demo")).OrderBy(x => x.ID).Take(10).ToListAsync();
        var surveys = await db.KhaoSatNoiBos.Include(x => x.LuaChons).Where(x => x.TieuDe.StartsWith("Khảo sát nội bộ demo")).OrderBy(x => x.ID).Take(10).ToListAsync();
        for (var i = 0; i < 10; i++)
        {
            var account = accounts[i % accounts.Count];
            if (!await db.XacNhanTaiLieus.AnyAsync(x => x.TaiLieuNoiBoID == documents[i].ID && x.TaiKhoanID == account.Id))
                db.XacNhanTaiLieus.Add(new XacNhanTaiLieu { TaiLieuNoiBoID = documents[i].ID, TaiKhoanID = account.Id, ThoiGian = DateTime.UtcNow.AddDays(-i) });
            if (!await db.BinhLuanThongBaos.AnyAsync(x => x.ThongBaoNoiBoID == announcements[i].ID && x.TaiKhoanID == account.Id))
                db.BinhLuanThongBaos.Add(new BinhLuanThongBao { ThongBaoNoiBoID = announcements[i].ID, TaiKhoanID = account.Id, NoiDung = $"Phản hồi thông báo demo {i + 1}.", NgayTao = DateTime.UtcNow.AddHours(-i) });
            if (!await db.PhieuKhaoSats.AnyAsync(x => x.KhaoSatNoiBoID == surveys[i].ID && x.TaiKhoanID == account.Id))
                db.PhieuKhaoSats.Add(new PhieuKhaoSat { KhaoSatNoiBoID = surveys[i].ID, LuaChonKhaoSatID = surveys[i].LuaChons.ElementAt(i % surveys[i].LuaChons.Count).ID, TaiKhoanID = account.Id, NgayTraLoi = DateTime.UtcNow.AddDays(-i) });
        }
        await db.SaveChangesAsync();
    }

    private static async Task EnsureLegacyAndWorkflowCoverageAsync(
        AppDbContext db, IReadOnlyDictionary<string, PhongBan> departments, IReadOnlyList<NhanVien> employees)
    {
        for (var i = 1; i <= 10; i++)
        {
            var shiftCode = $"DEMO-{i:00}";
            if (!await db.CaLamViecs.AnyAsync(x => x.MaCa == shiftCode))
                db.CaLamViecs.Add(new CaLamViec { MaCa = shiftCode, TenCa = $"Ca dữ liệu demo {i:00}", GioBatDau = new TimeSpan(7 + i % 3, 0, 0), GioKetThuc = new TimeSpan(16 + i % 3, 0, 0), SoPhutNghi = 60, ChoPhepDiMuonPhut = 15, HoatDong = i != 10 });

            var workflowType = $"DemoRequest{i:00}";
            if (!await db.QuyTrinhPheDuyets.AnyAsync(x => x.LoaiYeuCau == workflowType))
                db.QuyTrinhPheDuyets.Add(new QuyTrinhPheDuyet { LoaiYeuCau = workflowType, TenQuyTrinh = $"Quy trình kiểm thử {i:00}", HoatDong = i != 10, CacBuoc = [new() { ThuTu = 1, VaiTroDuyet = AuthorizationRoles.Manager, TenBuoc = "Quản lý duyệt" }, new() { ThuTu = 2, VaiTroDuyet = AuthorizationRoles.HumanResources, TenBuoc = "HR xác nhận" }] });

            var requestCode = $"YCNL-DEMO-{i:00}";
            if (!await db.YeuCauNhanLuc.AnyAsync(x => x.MaYeuCau == requestCode))
                db.YeuCauNhanLuc.Add(new YeuCauNhanLuc { MaYeuCau = requestCode, PhongBanID = departments.Values.ElementAt(i % departments.Count).ID, ViTriCanBoSung = $"Vị trí tuyển dụng demo {i}", SoLuong = 1 + i % 3, LyDo = "Mở rộng hoạt động kinh doanh", HinhThucBoSung = i % 2 == 0 ? WorkforceSupplyMethods.ExternalRecruitment : WorkforceSupplyMethods.InternalTransfer, MoTaCongViec = "Mô tả công việc dùng để kiểm thử.", NhanVienNoiBoID = i % 2 == 1 ? employees[i % employees.Count].ID : null, TenUngVien = i % 3 == 0 ? $"Ứng viên demo {i}" : null, NgayPhongVan = i % 3 == 0 ? new DateTime(DemoYear, 9, i + 1) : null, TrangThai = new[] { WorkforceRequestStatuses.Pending, WorkforceRequestStatuses.InternalSelection, WorkforceRequestStatuses.Recruiting, WorkforceRequestStatuses.Interviewing, WorkforceRequestStatuses.Onboarding, WorkforceRequestStatuses.Completed, WorkforceRequestStatuses.Rejected }[i % 7], NguoiDeXuat = "quanly@tic.local", NguoiXuLy = i > 2 ? "hr@tic.local" : null });

            var tripStart = new DateTime(DemoYear, 9, 1).AddDays(i * 2);
            if (!await db.CongTacs.AnyAsync(x => x.MucDich == $"Công tác demo {i:00}"))
                db.CongTacs.Add(new CongTac { NhanVienID = employees[i % employees.Count].ID, TuNgay = tripStart, DenNgay = tripStart.AddDays(2), DiaDiem = i % 2 == 0 ? "Hà Nội" : "Đà Nẵng", MucDich = $"Công tác demo {i:00}", TamUng = 2_000_000 + i * 100_000, TrangThai = new[] { ApprovalStatuses.Pending, ApprovalStatuses.Approved, ApprovalStatuses.Rejected }[i % 3], NguoiDuyet = i % 3 == 0 ? null : "hr@tic.local", NgayDuyet = i % 3 == 0 ? null : DateTime.UtcNow, PhanCongs = [new PhanCongCongTac { NhanVienID = employees[i % employees.Count].ID, DaXem = i % 2 == 0 }] });
        }
        await db.SaveChangesAsync();

        var demoShifts = await db.CaLamViecs.Where(x => x.MaCa.StartsWith("DEMO-")).OrderBy(x => x.ID).ToListAsync();
        for (var i = 0; i < employees.Count; i++)
        {
            var employee = employees[i];
            if (!await db.PhanCaNhanViens.AnyAsync(x => x.NhanVienID == employee.ID && x.TuNgay == new DateTime(DemoYear, 1, 1)))
                db.PhanCaNhanViens.Add(new PhanCaNhanVien { NhanVienID = employee.ID, CaLamViecID = demoShifts[i % demoShifts.Count].ID, TuNgay = new DateTime(DemoYear, 1, 1), DenNgay = new DateTime(DemoYear, 12, 31) });
            if (!await db.DanhGiaHieuSuat.AnyAsync(x => x.NhanVienID == employee.ID && x.TuNgay == new DateTime(DemoYear, 1, 1)))
                db.DanhGiaHieuSuat.Add(new DanhGiaHieuSuat { NhanVienID = employee.ID, TuNgay = new DateTime(DemoYear, 1, 1), DenNgay = new DateTime(DemoYear, 6, 30), ChatLuongCongViec = 3 + i % 3, TienDoCongViec = 3 + (i + 1) % 3, TinhThanHopTac = 3 + (i + 2) % 3, KyLuat = 3 + i % 2, DiemManh = "Hoàn thành công việc được giao", NoiDungCanCaiThien = i % 2 == 0 ? "Cải thiện kỹ năng báo cáo" : "Nâng cao kỹ năng phối hợp", DeXuat = i % 3 == 0 ? "Xem xét khen thưởng" : "Tiếp tục theo dõi", NguoiDanhGia = "quanly@tic.local" });
        }
        await db.SaveChangesAsync();
    }

    private static async Task EnsurePayrollsAsync(
        AppDbContext db, IReadOnlyList<NhanVien> employees, IReadOnlyDictionary<int, HopDong> contracts)
    {
        foreach (var employee in employees)
        foreach (var month in new[] { 5, 6, 7 })
        {
            if (await db.Luongs.AnyAsync(x => x.NhanVienID == employee.ID && x.Thang == month && x.Nam == DemoYear)) continue;
            var baseSalary = contracts[employee.ID].Luongcoban;
            var coefficient = employee.ChucVu?.Hesoluong ?? 1m;
            var workDays = 20m + (employee.ID + month) % 3;
            var allowance = 700_000m + employee.ID % 5 * 100_000m;
            var bonus = month == 7 ? 1_000_000m + employee.ID % 4 * 250_000m : 0m;
            var otherIncome = month == 6 ? 500_000m : 0m;
            var overtimeHours = (employee.ID + month) % 5 * 2m;
            var salaryBasis = baseSalary * coefficient;
            var overtimePay = Math.Round(overtimeHours * salaryBasis / 22m / 8m * 2m, 0);
            var penalty = employee.ID % 4 == 0 ? Math.Round(salaryBasis / 22m / 8m, 0) : 0m;
            var insurance = 1_890_000m;
            var net = Math.Round(salaryBasis / 22m * workDays + allowance + bonus + otherIncome + overtimePay - penalty - insurance, 0);
            db.Luongs.Add(new Luong { NhanVienID = employee.ID, Ten = $"Lương tháng {month:00}/{DemoYear}", Thang = month, Nam = DemoYear, Luongcoban = baseSalary, HeSoLuong = coefficient, SoNgayCong = workDays, SoNgayChuan = 22, Phucap = allowance, Thuong = bonus, ThuNhapKhac = otherIncome, SoGioTangCa = overtimeHours, TienTangCa = overtimePay, Khautru = penalty, BaoHiem = insurance, ThueTNCN = 0, Tongluong = net, TrangThai = PayrollStatuses.Paid, NgayTinh = new DateTime(DemoYear, month, 28), NgayDoiSoat = new DateTime(DemoYear, month, 29), NgayKhoa = new DateTime(DemoYear, month, 30), NguoiDoiSoat = "hr@tic.local", NguoiKhoa = "admin@tic.local" });
        }
        await db.SaveChangesAsync();
    }

    private static async Task EnsureAuditRowsAsync(AppDbContext db, IReadOnlyList<NhanVien> employees)
    {
        for (var i = 1; i <= 20; i++)
        {
            var target = $"DEMO-AUDIT-{i:00}";
            if (!await db.NhatKyHeThongs.AnyAsync(x => x.DoiTuongID == target))
                db.NhatKyHeThongs.Add(new NhatKyHeThong { HanhDong = i % 2 == 0 ? "Create" : "Approve", DoiTuong = i % 2 == 0 ? "NhanVien" : "YeuCau", DoiTuongID = target, ChiTiet = $"Nhật ký dữ liệu kiểm thử số {i}", NguoiThucHien = i % 2 == 0 ? "hr@tic.local" : employees[i % employees.Count].Email!, ThoiGian = DateTime.UtcNow.AddMinutes(-i) });
        }
        await db.SaveChangesAsync();
    }

    private static async Task<List<string>> ValidateCoverageAsync(AppDbContext db)
    {
        var counts = new Dictionary<string, int>
        {
            ["PhongBan"] = await db.PhongBans.CountAsync(), ["ChucVu"] = await db.ChucVus.CountAsync(),
            ["NhanVien"] = await db.NhanViens.CountAsync(), ["TaiKhoan"] = await db.TaiKhoans.CountAsync(),
            ["HopDong"] = await db.HopDongs.CountAsync(), ["TaiLieuHopDong"] = await db.TaiLieuHopDongs.CountAsync(),
            ["Luong"] = await db.Luongs.CountAsync(), ["ChamCong"] = await db.ChamCongs.CountAsync(),
            ["NghiPhep"] = await db.NghiPheps.CountAsync(), ["SoDuNghiPhep"] = await db.SoDuNghiPheps.CountAsync(),
            ["PhuCap"] = await db.PhuCaps.CountAsync(), ["ChinhSachPhuCap"] = await db.ChinhSachPhuCaps.CountAsync(),
            ["ChinhSachBHXH"] = await db.ChinhSachBhxhs.CountAsync(), ["BaoHiem"] = await db.BaoHiems.CountAsync(),
            ["ThuongPhat"] = await db.ThuongPhats.CountAsync(), ["TrinhDoChungChi"] = await db.TrinhDoChungChis.CountAsync(),
            ["TaiLieuNhanVien"] = await db.TaiLieuNhanViens.CountAsync(), ["DieuChinhChamCong"] = await db.DieuChinhChamCongs.CountAsync(),
            ["YeuCauCapNhatThongTinCaNhan"] = await db.YeuCauCapNhatThongTinCaNhans.CountAsync(), ["ThongBaoCaNhan"] = await db.ThongBaoCaNhans.CountAsync(),
            ["DangKyTangCa"] = await db.DangKyTangCas.CountAsync(), ["TaiLieuNoiBo"] = await db.TaiLieuNoiBos.CountAsync(),
            ["ThongBaoNoiBo"] = await db.ThongBaoNoiBos.CountAsync(), ["KhaoSatNoiBo"] = await db.KhaoSatNoiBos.CountAsync(),
            ["YeuCauNhanLuc"] = await db.YeuCauNhanLuc.CountAsync(), ["DanhGiaHieuSuat"] = await db.DanhGiaHieuSuat.CountAsync(),
            ["NhatKyHeThong"] = await db.NhatKyHeThongs.CountAsync()
        };
        var warnings = counts.Where(x => x.Value < 10).Select(x => $"{x.Key}={x.Value}").ToList();
        var employeeIds = await db.NhanViens.Where(x => x.MaNhanVien.Contains("DEMO") || x.MaNhanVien.StartsWith("QL-") || x.MaNhanVien.StartsWith("NV-")).Select(x => x.ID).ToListAsync();
        foreach (var id in employeeIds)
        {
            var payrollMonths = await db.Luongs.Where(x => x.NhanVienID == id && x.Nam == DemoYear && new[] { 5, 6, 7 }.Contains(x.Thang)).Select(x => x.Thang).Distinct().CountAsync();
            if (payrollMonths < 3) warnings.Add($"NhanVien#{id}: thiếu lương 05-07");
            var attendance = await db.ChamCongs.CountAsync(x => x.NhanVienID == id && x.Ngay >= new DateTime(DemoYear, 8, 1) && x.Ngay < new DateTime(DemoYear, 9, 1));
            if (attendance < 21) warnings.Add($"NhanVien#{id}: công tháng 08={attendance}");
        }
        return warnings;
    }

    private static (string Path, long Size) EnsureDemoImage()
    {
        const string relativePath = "demo/demo-proof.png";
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "uploads", "demo", "demo-proof.png");
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        if (!File.Exists(fullPath))
        {
            var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
            File.WriteAllBytes(fullPath, png);
        }
        return (relativePath, new FileInfo(fullPath).Length);
    }

    private static void Ensure(IdentityResult result, string action)
    {
        if (result.Succeeded) return;
        throw new InvalidOperationException($"Không thể {action}: {string.Join("; ", result.Errors.Select(x => x.Description))}");
    }
}
