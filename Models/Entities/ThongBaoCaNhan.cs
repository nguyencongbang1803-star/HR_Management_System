using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLNS.Models.Entities;

public class ThongBaoCaNhan
{
    public int ID { get; set; }
    public int NhanVienID { get; set; }
    [Required,MaxLength(50)] public string Loai { get; set; } = string.Empty;
    [Required,MaxLength(200)] public string TieuDe { get; set; } = string.Empty;
    [Required,MaxLength(1000)] public string NoiDung { get; set; } = string.Empty;
    [Required,MaxLength(150)] public string MaThamChieu { get; set; } = string.Empty;
    [MaxLength(500)] public string? DuongDan { get; set; }
    public bool DaDoc { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public DateTime? NgayDoc { get; set; }
    [ForeignKey(nameof(NhanVienID))] public NhanVien? NhanVien { get; set; }
}
