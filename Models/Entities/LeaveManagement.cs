using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLNS.Models.Entities;

public class LoaiNghiPhep
{
    public int ID { get; set; }
    [Required, MaxLength(30)] public string MaLoai { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string TenLoai { get; set; } = string.Empty;
    public bool HuongLuong { get; set; }
    public bool TruQuyPhep { get; set; }
    public bool HoatDong { get; set; } = true;
    public ICollection<NghiPhep> DonNghi { get; set; } = new List<NghiPhep>();
}

public class SoDuNghiPhep
{
    public int ID { get; set; }
    public int NhanVienID { get; set; }
    public int LoaiNghiPhepID { get; set; }
    public int Nam { get; set; }
    [Column(TypeName = "decimal(6,2)")] public decimal SoNgayDuocHuong { get; set; }
    [Column(TypeName = "decimal(6,2)")] public decimal DieuChinh { get; set; }
    [MaxLength(300)] public string? GhiChu { get; set; }
    [ForeignKey(nameof(NhanVienID))] public NhanVien? NhanVien { get; set; }
    [ForeignKey(nameof(LoaiNghiPhepID))] public LoaiNghiPhep? LoaiNghiPhep { get; set; }
}
