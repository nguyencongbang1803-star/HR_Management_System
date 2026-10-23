using System.ComponentModel.DataAnnotations;

namespace QLNS.Models.Entities;

public class NhatKyHeThong
{
    public long ID { get; set; }

    [Required, MaxLength(100)]
    public string HanhDong { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string DoiTuong { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string DoiTuongID { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? ChiTiet { get; set; }

    [Required, MaxLength(150)]
    public string NguoiThucHien { get; set; } = string.Empty;

    public DateTime ThoiGian { get; set; } = DateTime.UtcNow;
}
