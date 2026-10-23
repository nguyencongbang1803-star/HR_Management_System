using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLNS.Models.Entities
{
    public class HopDong
    {
        public int ID { get; set; }

        public int NhanVienID { get; set; }

        [Required, MaxLength(50)]
        [Display(Name = "Số hợp đồng")]
        public string SoHopDong { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        [Display(Name = "Loại hợp đồng")]
        public string Loaihopdong { get; set; } = string.Empty;

        [Display(Name = "Ngày ký")]
        public DateTime Ngayky { get; set; }

        [Display(Name = "Ngày bắt đầu")]
        public DateTime Ngaybatdau { get; set; }

        [Display(Name = "Ngày kết thúc")]
        public DateTime? Ngayketthuc { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Lương cơ bản")]
        public decimal Luongcoban { get; set; }

        [Display(Name = "Trạng thái")]
        public bool Trangthai { get; set; } = true;

        [MaxLength(260)] public string? TenFileGoc { get; set; }
        [MaxLength(260)] public string? DuongDanFile { get; set; }
        [MaxLength(100)] public string? ContentType { get; set; }
        public int? HopDongTruocID { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("NhanVienID")]
        public NhanVien? NhanVien { get; set; }
        [ForeignKey(nameof(HopDongTruocID))] public HopDong? HopDongTruoc { get; set; }
        public ICollection<TaiLieuHopDong> TaiLieuHopDongs { get; set; } = new List<TaiLieuHopDong>();
    }

    public class TaiLieuHopDong
    {
        public int ID { get; set; }
        public int HopDongID { get; set; }
        [Required, MaxLength(260)] public string TenFileGoc { get; set; } = string.Empty;
        [Required, MaxLength(260)] public string DuongDanFile { get; set; } = string.Empty;
        [MaxLength(100)] public string? ContentType { get; set; }
        public long KichThuoc { get; set; }
        public DateTime NgayTaiLen { get; set; } = DateTime.UtcNow;
        [ForeignKey(nameof(HopDongID))] public HopDong? HopDong { get; set; }
    }
}
