using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLNS.Models.Entities;

public class ChamCong
{
    public int ID { get; set; }
    public int NhanVienID { get; set; }

    [Display(Name = "Ngày")]
    public DateTime Ngay { get; set; }

    [Display(Name = "Giờ vào")]
    public DateTime? Giovaolam { get; set; }

    [Display(Name = "Giờ ra")]
    public DateTime? Giotanlam { get; set; }

    [Required, MaxLength(30)]
    [Display(Name = "Trạng thái")]
    public string TrangThai { get; set; } = AttendanceStatuses.Present;

    [MaxLength(500)]
    [Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }

    public int PhutDiMuon { get; set; }
    public int PhutVeSom { get; set; }

    [NotMapped]
    [Display(Name = "Số giờ làm")]
    public double? SoGioLam => Giovaolam.HasValue && Giotanlam.HasValue
        ? Math.Max(0, (Giotanlam.Value - Giovaolam.Value).TotalHours)
        : null;

    [ForeignKey(nameof(NhanVienID))]
    public NhanVien? NhanVien { get; set; }
}
