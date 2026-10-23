using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLNS.Models.Entities;

public class DieuChinhChamCong
{
    public int ID { get; set; }
    public int? ChamCongID { get; set; }
    public int NhanVienID { get; set; }
    public DateTime Ngay { get; set; }
    public TimeSpan? GioVaoDeXuat { get; set; }
    public TimeSpan? GioRaDeXuat { get; set; }
    [Required, MaxLength(500)] public string LyDo { get; set; } = string.Empty;
    [Required, MaxLength(30)] public string TrangThai { get; set; } = ApprovalStatuses.Pending;
    [MaxLength(500)] public string? PhanHoi { get; set; }
    [MaxLength(150)] public string? NguoiDuyet { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public DateTime? NgayDuyet { get; set; }
    [ForeignKey(nameof(NhanVienID))] public NhanVien? NhanVien { get; set; }
    [ForeignKey(nameof(ChamCongID))] public ChamCong? ChamCong { get; set; }
    public ICollection<DieuChinhChamCongMinhChung> MinhChungs { get; set; } = new List<DieuChinhChamCongMinhChung>();
}

public class DieuChinhChamCongMinhChung
{
    public int ID { get; set; }
    public int DieuChinhChamCongID { get; set; }
    [Required, MaxLength(255)] public string TenFileGoc { get; set; } = string.Empty;
    [Required, MaxLength(500)] public string DuongDan { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string ContentType { get; set; } = string.Empty;
    public long KichThuoc { get; set; }
    [ForeignKey(nameof(DieuChinhChamCongID))] public DieuChinhChamCong? YeuCau { get; set; }
}

public class YeuCauCapNhatThongTinCaNhan
{
    public int ID { get; set; }
    public int NhanVienID { get; set; }
    [MaxLength(150), EmailAddress] public string? Email { get; set; }
    [MaxLength(15), Phone] public string? SDT { get; set; }
    public DateTime? NgaySinh { get; set; }
    public bool GioiTinh { get; set; }
    [MaxLength(20)] public string? CanCuocCongDan { get; set; }
    [MaxLength(300)] public string? DiaChi { get; set; }
    [MaxLength(30)] public string? SoTaiKhoan { get; set; }
    [MaxLength(20)] public string? MaSoThue { get; set; }
    [Required, MaxLength(500)] public string LyDo { get; set; } = string.Empty;
    [Required, MaxLength(30)] public string TrangThai { get; set; } = ApprovalStatuses.Pending;
    [MaxLength(500)] public string? PhanHoi { get; set; }
    [MaxLength(150)] public string? NguoiDuyet { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public DateTime? NgayDuyet { get; set; }
    [ForeignKey(nameof(NhanVienID))] public NhanVien? NhanVien { get; set; }
}

public static class ApprovalStatuses
{
    public const string Pending = "Pending";
    public const string ManagerApproved = "ManagerApproved";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
}
