using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLNS.Models.Entities;

public class CaLamViec
{
    public int ID { get; set; }
    [Required, MaxLength(30)] public string MaCa { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string TenCa { get; set; } = string.Empty;
    public TimeSpan GioBatDau { get; set; }
    public TimeSpan GioKetThuc { get; set; }
    public int SoPhutNghi { get; set; } = 60;
    public int ChoPhepDiMuonPhut { get; set; } = 5;
    public bool HoatDong { get; set; } = true;
    public ICollection<PhanCaNhanVien> PhanCas { get; set; } = new List<PhanCaNhanVien>();
}

public class PhanCaNhanVien
{
    public int ID { get; set; }
    public int NhanVienID { get; set; }
    public int CaLamViecID { get; set; }
    public DateTime TuNgay { get; set; }
    public DateTime? DenNgay { get; set; }
    [ForeignKey(nameof(NhanVienID))] public NhanVien? NhanVien { get; set; }
    [ForeignKey(nameof(CaLamViecID))] public CaLamViec? CaLamViec { get; set; }
}

public class DangKyTangCa
{
    public int ID { get; set; }
    public int NhanVienID { get; set; }
    public DateTime Ngay { get; set; }
    public TimeSpan GioBatDau { get; set; }
    public TimeSpan GioKetThuc { get; set; }
    [Column(TypeName="decimal(4,2)")] public decimal HeSo { get; set; } = 2m;
    [Column(TypeName="decimal(6,2)")] public decimal SoGioDuyet { get; set; }
    [Required, MaxLength(500)] public string LyDo { get; set; } = string.Empty;
    [MaxLength(255)] public string? TenFileMinhChung { get; set; }
    [MaxLength(500)] public string? DuongDanMinhChung { get; set; }
    [MaxLength(100)] public string? ContentTypeMinhChung { get; set; }
    [Required, MaxLength(30)] public string TrangThai { get; set; } = ApprovalStatuses.Pending;
    [MaxLength(500)] public string? PhanHoi { get; set; }
    [MaxLength(150)] public string? NguoiDuyet { get; set; }
    [MaxLength(500)] public string? PhanHoiQuanLy { get; set; }
    [MaxLength(150)] public string? NguoiDuyetQuanLy { get; set; }
    public DateTime? NgayDuyetQuanLy { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public DateTime? NgayDuyet { get; set; }
    [NotMapped] public decimal SoGioDangKy => GioKetThuc <= GioBatDau ? 0 : (decimal)(GioKetThuc-GioBatDau).TotalHours;
    [ForeignKey(nameof(NhanVienID))] public NhanVien? NhanVien { get; set; }
    public ICollection<DangKyTangCaMinhChung> MinhChungs { get; set; } = new List<DangKyTangCaMinhChung>();
}

public class DangKyTangCaMinhChung
{
    public int ID { get; set; }
    public int DangKyTangCaID { get; set; }
    [Required, MaxLength(260)] public string TenFileGoc { get; set; } = string.Empty;
    [Required, MaxLength(500)] public string DuongDan { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string ContentType { get; set; } = "application/octet-stream";
    public long KichThuoc { get; set; }
    public DateTime NgayTaiLen { get; set; } = DateTime.UtcNow;
    [ForeignKey(nameof(DangKyTangCaID))] public DangKyTangCa? DangKyTangCa { get; set; }
}

public class CongTac
{
    public int ID { get; set; }
    // Giữ khóa nhân viên cũ để tương thích dữ liệu trước khi hỗ trợ nhiều người.
    public int? NhanVienID { get; set; }
    public DateTime TuNgay { get; set; }
    public DateTime DenNgay { get; set; }
    [Required, MaxLength(200)] public string DiaDiem { get; set; } = string.Empty;
    [Required, MaxLength(500)] public string MucDich { get; set; } = string.Empty;
    [Column(TypeName="decimal(18,2)")] public decimal TamUng { get; set; }
    [Required, MaxLength(30)] public string TrangThai { get; set; } = ApprovalStatuses.Pending;
    [MaxLength(500)] public string? PhanHoi { get; set; }
    [MaxLength(150)] public string? NguoiDuyet { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public DateTime? NgayDuyet { get; set; }
    [ForeignKey(nameof(NhanVienID))] public NhanVien? NhanVien { get; set; }
    public ICollection<PhanCongCongTac> PhanCongs { get; set; } = new List<PhanCongCongTac>();
}

public class PhanCongCongTac
{
    public int ID { get; set; }
    public int CongTacID { get; set; }
    public int NhanVienID { get; set; }
    public DateTime NgayThongBao { get; set; } = DateTime.UtcNow;
    public bool DaXem { get; set; }
    [ForeignKey(nameof(CongTacID))] public CongTac? CongTac { get; set; }
    [ForeignKey(nameof(NhanVienID))] public NhanVien? NhanVien { get; set; }
}
