using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QLNS.Data;
using QLNS.Models.Entities;

namespace QLNS.Services;

public sealed class HrmClaimsPrincipalFactory(
    UserManager<TaiKhoan> userManager,
    RoleManager<IdentityRole<int>> roleManager,
    IOptions<IdentityOptions> options,
    AppDbContext context)
    : UserClaimsPrincipalFactory<TaiKhoan, IdentityRole<int>>(userManager, roleManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(TaiKhoan user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        if (user.NhanVienID.HasValue)
        {
            // Employee permissions always come from the current position.
            // Identity roles on the account are maintained only as a compatibility cache.
            foreach (var claim in identity.FindAll(ClaimTypes.Role).ToList())
                identity.RemoveClaim(claim);

            var profile = await context.NhanViens.AsNoTracking()
                .Where(x => x.ID == user.NhanVienID.Value)
                .Select(x => new
                {
                    x.Ten,
                    PositionId = x.ChucVu == null ? (int?)null : x.ChucVu.ID,
                    PositionName = x.ChucVu == null ? null : x.ChucVu.Chucvu,
                    PositionRole = x.ChucVu == null ? null : x.ChucVu.PhanQuyen
                })
                .SingleOrDefaultAsync();
            var configuredRoles = AuthorizationRoles.Parse(profile?.PositionRole);
            foreach(var role in configuredRoles)identity.AddClaim(new Claim(ClaimTypes.Role,role));
            if ((configuredRoles.Contains(AuthorizationRoles.Manager) || configuredRoles.Contains(AuthorizationRoles.HumanResources)) &&
                !identity.HasClaim(ClaimTypes.Role,AuthorizationRoles.Employee))
                identity.AddClaim(new Claim(ClaimTypes.Role, AuthorizationRoles.Employee));

            var managedDepartment = await context.PhongBans.AsNoTracking()
                .Where(x => x.Matruongphong == user.NhanVienID.Value)
                .Select(x => new
                {
                    IsHrDepartment = x.NhanViens.Any(n => n.ChucVu != null &&
                        ("," + n.ChucVu.PhanQuyen + ",").Contains(",HR,"))
                })
                .FirstOrDefaultAsync();
            if (managedDepartment is not null)
            {
                if (!identity.HasClaim(ClaimTypes.Role, AuthorizationRoles.Manager))
                    identity.AddClaim(new Claim(ClaimTypes.Role, AuthorizationRoles.Manager));
                if (!identity.HasClaim(ClaimTypes.Role, AuthorizationRoles.Employee))
                    identity.AddClaim(new Claim(ClaimTypes.Role, AuthorizationRoles.Employee));
                if (managedDepartment.IsHrDepartment &&
                    !identity.HasClaim(ClaimTypes.Role, AuthorizationRoles.HumanResources))
                    identity.AddClaim(new Claim(ClaimTypes.Role, AuthorizationRoles.HumanResources));
            }
            identity.AddClaim(new Claim("NhanVienID", user.NhanVienID.Value.ToString()));
            if (profile is not null)
            {
                identity.AddClaim(new Claim("NhanVienTen", profile.Ten));
                if (profile.PositionId.HasValue)
                    identity.AddClaim(new Claim("ChucVuID", profile.PositionId.Value.ToString()));
                if (!string.IsNullOrWhiteSpace(profile.PositionName))
                    identity.AddClaim(new Claim("ChucVu", profile.PositionName));
            }
        }
        return identity;
    }
}
