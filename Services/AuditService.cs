using System.Security.Claims;
using QLNS.Data;
using QLNS.Models.Entities;

namespace QLNS.Services;

public class AuditService
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public void Add(string action, string entity, string entityId, string? details = null)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        _context.NhatKyHeThongs.Add(new NhatKyHeThong
        {
            HanhDong = action,
            DoiTuong = entity,
            DoiTuongID = entityId,
            ChiTiet = details,
            NguoiThucHien = user?.FindFirstValue(ClaimTypes.Email) ?? user?.Identity?.Name ?? "system",
            ThoiGian = DateTime.UtcNow
        });
    }
}
