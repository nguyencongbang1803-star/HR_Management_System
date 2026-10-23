using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QLNS.Models.Entities;

namespace QLNS.Data;

public static class DbSeeder
{
    private static readonly string[] HrmRoles = ["Admin", "HR", "QuanLy", "NhanVien"];

    public static async Task InitializeAsync(
        IServiceProvider services,
        IWebHostEnvironment environment,
        ILogger logger)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TaiKhoan>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

        if (environment.IsDevelopment())
            await context.Database.MigrateAsync();

        if (!await context.LoaiNghiPheps.AnyAsync())
        {
            context.LoaiNghiPheps.AddRange(
                new LoaiNghiPhep { MaLoai = "PHEP_NAM", TenLoai = "Nghỉ phép năm", HuongLuong = true, TruQuyPhep = true },
                new LoaiNghiPhep { MaLoai = "NGHI_OM", TenLoai = "Nghỉ ốm", HuongLuong = true },
                new LoaiNghiPhep { MaLoai = "VIEC_RIENG", TenLoai = "Nghỉ việc riêng", HuongLuong = false },
                new LoaiNghiPhep { MaLoai = "KHONG_LUONG", TenLoai = "Nghỉ không lương", HuongLuong = false });
        }

        if (!await context.CaLamViecs.AnyAsync(x => x.MaCa == "HC"))
            context.CaLamViecs.Add(new CaLamViec
            {
                MaCa = "HC", TenCa = "Hành chính", GioBatDau = new TimeSpan(8, 0, 0),
                GioKetThuc = new TimeSpan(17, 0, 0), SoPhutNghi = 60, ChoPhepDiMuonPhut = 5
            });
        if (!await context.CaLamViecs.AnyAsync(x => x.MaCa == "SANG"))
            context.CaLamViecs.Add(new CaLamViec
            {
                MaCa = "SANG", TenCa = "Ca sáng", GioBatDau = new TimeSpan(6, 0, 0),
                GioKetThuc = new TimeSpan(14, 0, 0), SoPhutNghi = 30, ChoPhepDiMuonPhut = 5
            });
        await context.SaveChangesAsync();

        foreach (var role in HrmRoles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole<int>(role));
                EnsureSuccess(result, $"tạo vai trò {role}");
            }
        }

        // Nâng cấp các tài khoản cũ: chuẩn hóa thông tin Identity và đưa vai trò vào bảng liên kết.
        foreach (var account in await userManager.Users.ToListAsync())
        {
            var needsUpdate = false;
            var desiredRoles = account.NhanVienID.HasValue
                ? AuthorizationRoles.Parse(await context.NhanViens
                    .Where(x => x.ID == account.NhanVienID.Value)
                    .Select(x => x.ChucVu == null ? null : x.ChucVu.PhanQuyen)
                    .SingleOrDefaultAsync())
                : AuthorizationRoles.Parse(account.Role);
            var desiredRole=AuthorizationRoles.All.Reverse().First(desiredRoles.Contains);
            if (string.IsNullOrWhiteSpace(account.UserName))
            {
                account.UserName = account.Email;
                needsUpdate = true;
            }
            if (!account.EmailConfirmed)
            {
                account.EmailConfirmed = true;
                needsUpdate = true;
            }
            if (account.Role != desiredRole)
            {
                account.Role = desiredRole;
                needsUpdate = true;
            }
            if (needsUpdate)
                EnsureSuccess(await userManager.UpdateAsync(account), $"nâng cấp tài khoản {account.Email}");

            var currentRoles = await userManager.GetRolesAsync(account);
            if (!currentRoles.ToHashSet(StringComparer.Ordinal).SetEquals(desiredRoles))
            {
                if (currentRoles.Count > 0)
                    EnsureSuccess(await userManager.RemoveFromRolesAsync(account, currentRoles),
                        $"xóa vai trò cũ của {account.Email}");
                EnsureSuccess(await userManager.AddToRolesAsync(account, desiredRoles),
                    $"gán vai trò cho {account.Email}");
            }
        }

        if (!await userManager.Users.AnyAsync(x => x.Role == "Admin"))
        {
            await CreateAccountAsync(userManager,
                Environment.GetEnvironmentVariable("HRM_ADMIN_EMAIL") ?? "admin@tic.local",
                Environment.GetEnvironmentVariable("HRM_ADMIN_PASSWORD") ?? "Admin@123456",
                "ADMIN", "Admin", logger);
        }

        if (!await userManager.Users.AnyAsync(x => x.Role == "QuanLy"))
            await CreateAccountAsync(userManager, "quanly@tic.local", "Quanly@123456", "QL01", "QuanLy", logger);

        if (!await userManager.Users.AnyAsync(x => x.Role == "NhanVien"))
            await CreateAccountAsync(userManager, "nhanvien@tic.local", "Nhanvien@123456", "NV01", "NhanVien", logger);

        if (!await userManager.Users.AnyAsync(x => x.Email == "hrtesst@tic.local"))
            await CreateAccountAsync(userManager, "hrtesst@tic.local", "Hrtesst@123456", "HRTEST", "HR", logger);

        if (environment.IsDevelopment())
        {
            await DemoDataSeeder.SeedAsync(context, userManager, logger);
            await ComprehensiveCompanySeeder.SeedAsync(context, userManager, logger);
            await CompanyPresentationSeeder.SeedAsync(context, userManager, logger);
        }
    }

    private static async Task CreateAccountAsync(
        UserManager<TaiKhoan> userManager,
        string email,
        string password,
        string employeeCode,
        string role,
        ILogger logger)
    {
        email = email.Trim().ToLowerInvariant();
        var account = new TaiKhoan
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            MaNhanVien = employeeCode,
            Role = role
        };
        EnsureSuccess(await userManager.CreateAsync(account, password), $"tạo tài khoản {email}");
        EnsureSuccess(await userManager.AddToRoleAsync(account, role), $"gán vai trò {role} cho {email}");
        logger.LogInformation("Đã tạo tài khoản Identity mặc định: {Email} ({Role})", email, role);
    }

    private static void EnsureSuccess(IdentityResult result, string action)
    {
        if (result.Succeeded) return;
        throw new InvalidOperationException(
            $"Không thể {action}: {string.Join("; ", result.Errors.Select(x => x.Description))}");
    }
}
