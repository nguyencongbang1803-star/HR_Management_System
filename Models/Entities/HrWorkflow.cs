using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLNS.Models.Entities;

public class YeuCauNhanLuc
{
    public int ID { get; set; }
    [Required, MaxLength(30)] public string MaYeuCau { get; set; } = string.Empty;
    public int PhongBanID { get; set; }
    [Required, MaxLength(150)] public string ViTriCanBoSung { get; set; } = string.Empty;
    [Range(1, 100)] public int SoLuong { get; set; } = 1;
    [Required, MaxLength(1000)] public string LyDo { get; set; } = string.Empty;
    [MaxLength(30)] public string HinhThucBoSung { get; set; } = WorkforceSupplyMethods.Unassigned;
    [MaxLength(4000)] public string? MoTaCongViec { get; set; }
    public int? NhanVienNoiBoID { get; set; }
    [MaxLength(150)] public string? TenUngVien { get; set; }
    public DateTime? NgayPhongVan { get; set; }
    [Required, MaxLength(30)] public string TrangThai { get; set; } = WorkforceRequestStatuses.Pending;
    [MaxLength(1000)] public string? GhiChuXuLy { get; set; }
    [Required, MaxLength(150)] public string NguoiDeXuat { get; set; } = string.Empty;
    [MaxLength(150)] public string? NguoiXuLy { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public DateTime? NgayCapNhat { get; set; }
    [ForeignKey(nameof(PhongBanID))] public PhongBan? PhongBan { get; set; }
    [ForeignKey(nameof(NhanVienNoiBoID))] public NhanVien? NhanVienNoiBo { get; set; }
}

public class DanhGiaHieuSuat
{
    public int ID { get; set; }
    public int NhanVienID { get; set; }
    public DateTime TuNgay { get; set; }
    public DateTime DenNgay { get; set; }
    [Range(1, 5)] public int ChatLuongCongViec { get; set; }
    [Range(1, 5)] public int TienDoCongViec { get; set; }
    [Range(1, 5)] public int TinhThanHopTac { get; set; }
    [Range(1, 5)] public int KyLuat { get; set; }
    [MaxLength(2000)] public string? DiemManh { get; set; }
    [MaxLength(2000)] public string? NoiDungCanCaiThien { get; set; }
    [MaxLength(1000)] public string? DeXuat { get; set; }
    [Required, MaxLength(150)] public string NguoiDanhGia { get; set; } = string.Empty;
    public DateTime NgayDanhGia { get; set; } = DateTime.UtcNow;
    [NotMapped] public decimal DiemTrungBinh =>
        Math.Round((ChatLuongCongViec + TienDoCongViec + TinhThanHopTac + KyLuat) / 4m, 2);
    [ForeignKey(nameof(NhanVienID))] public NhanVien? NhanVien { get; set; }
}

public static class WorkforceSupplyMethods
{
    public const string Unassigned = "Unassigned";
    public const string InternalTransfer = "InternalTransfer";
    public const string ExternalRecruitment = "ExternalRecruitment";
}

public static class WorkforceRequestStatuses
{
    public const string Pending = "Pending";
    public const string InternalSelection = "InternalSelection";
    public const string Recruiting = "Recruiting";
    public const string Interviewing = "Interviewing";
    public const string Onboarding = "Onboarding";
    public const string Completed = "Completed";
    public const string Rejected = "Rejected";
}
