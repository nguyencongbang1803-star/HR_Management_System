using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLNS.Models.Entities;

public class TrinhDoChungChi
{
    public int ID { get; set; }
    public int NhanVienID { get; set; }
    [Required, MaxLength(30)] public string Loai { get; set; } = "BangCap";
    [Required, MaxLength(200)] public string Ten { get; set; } = string.Empty;
    [MaxLength(200)] public string? DonViCap { get; set; }
    public DateTime? NgayCap { get; set; }
    public DateTime? NgayHetHan { get; set; }
    [MaxLength(100)] public string? SoHieu { get; set; }
    [ForeignKey(nameof(NhanVienID))] public NhanVien? NhanVien { get; set; }
}

public class QuaTrinhCongTac
{
    public int ID { get; set; }
    public int NhanVienID { get; set; }
    [Required, MaxLength(200)] public string DonVi { get; set; } = string.Empty;
    [Required, MaxLength(150)] public string ChucDanh { get; set; } = string.Empty;
    public DateTime TuNgay { get; set; }
    public DateTime? DenNgay { get; set; }
    [MaxLength(500)] public string? MoTa { get; set; }
    [ForeignKey(nameof(NhanVienID))] public NhanVien? NhanVien { get; set; }
}

public class TaiLieuNhanVien
{
    public int ID { get; set; }
    public int NhanVienID { get; set; }
    [Required, MaxLength(150)] public string TenTaiLieu { get; set; } = string.Empty;
    [Required, MaxLength(260)] public string TenFileGoc { get; set; } = string.Empty;
    [Required, MaxLength(260)] public string DuongDan { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string ContentType { get; set; } = "application/octet-stream";
    public long KichThuoc { get; set; }
    public DateTime NgayTaiLen { get; set; } = DateTime.UtcNow;
    [MaxLength(150)] public string? NguoiTaiLen { get; set; }
    [ForeignKey(nameof(NhanVienID))] public NhanVien? NhanVien { get; set; }
}
