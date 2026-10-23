using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLNS.Models.Entities;

public class Luong
{
    public int ID { get; set; }
    public int NhanVienID { get; set; }

    [Required, MaxLength(100)]
    public string Ten { get; set; } = string.Empty;

    [Range(1, 12)]
    public int Thang { get; set; }

    [Range(2000, 2200)]
    public int Nam { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Lương cơ bản")]
    public decimal Luongcoban { get; set; }

    [Column(TypeName = "decimal(8,2)")]
    [Display(Name = "Hệ số lương chức vụ")]
    public decimal HeSoLuong { get; set; } = 1m;

    [Display(Name = "Ngày công")]
    public decimal SoNgayCong { get; set; }

    [Display(Name = "Ngày công chuẩn")]
    public int SoNgayChuan { get; set; } = 22;

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Phụ cấp")]
    public decimal Phucap { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Thưởng")]
    public decimal Thuong { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Thu nhập khác")]
    public decimal ThuNhapKhac { get; set; }

    [Column(TypeName = "decimal(8,2)")]
    [Display(Name = "Giờ tăng ca")]
    public decimal SoGioTangCa { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Tiền tăng ca")]
    public decimal TienTangCa { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Khấu trừ/phạt")]
    public decimal Khautru { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Bảo hiểm")]
    public decimal BaoHiem { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Thuế thu nhập cá nhân")]
    public decimal ThueTNCN { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Thực lĩnh")]
    public decimal Tongluong { get; set; }

    [Required, MaxLength(30)]
    public string TrangThai { get; set; } = PayrollStatuses.Draft;
    public DateTime NgayTinh { get; set; } = DateTime.UtcNow;
    public DateTime? NgayKhoa { get; set; }
    public DateTime? NgayDoiSoat { get; set; }
    [MaxLength(150)] public string? NguoiDoiSoat { get; set; }
    [MaxLength(150)] public string? NguoiKhoa { get; set; }

    [ForeignKey(nameof(NhanVienID))]
    public NhanVien? NhanVien { get; set; }
}
