using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLNS.Models.Entities;

public sealed class ChinhSachBhxh
{
    public int ID { get; set; }

    [Required, MaxLength(150)]
    public string TenKhoan { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal SoTienDong { get; set; }

    [Required, MaxLength(30)]
    public string PhamVi { get; set; } = AllowanceScopes.Company;

    public int? PhongBanID { get; set; }
    public int? ChucVuID { get; set; }
    public int? NhanVienID { get; set; }
    public DateTime NgayBatDau { get; set; }

    [Required, MaxLength(30)]
    public string KieuThoiHan { get; set; } = AllowanceDurations.Monthly;

    public int? SoThang { get; set; }
    public bool HoatDong { get; set; } = true;
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public DateTime? NgayCapNhat { get; set; }

    [ForeignKey(nameof(PhongBanID))] public PhongBan? PhongBan { get; set; }
    [ForeignKey(nameof(ChucVuID))] public ChucVu? ChucVu { get; set; }
    [ForeignKey(nameof(NhanVienID))] public NhanVien? NhanVien { get; set; }
}
