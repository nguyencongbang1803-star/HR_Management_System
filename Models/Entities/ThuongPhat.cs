using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLNS.Models.Entities
{
    public class ThuongPhat
    {
        [Key]
        public int MaThuongPhat { get; set; }

        [Required, MaxLength(50)]
        [Display(Name = "Loại")]
        public string Loai { get; set; } = "Thuong"; // Thuong | Phat

        [MaxLength(500)]
        [Display(Name = "Lý do")]
        public string? LyDo { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Số tiền")]
        public decimal SoTien { get; set; }

        [Display(Name = "Ngày áp dụng")]
        public DateTime NgayApDung { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }

        // FK liên kết NhanVien
        public int NhanVienID { get; set; }

        [ForeignKey("NhanVienID")]
        public NhanVien? NhanVien { get; set; }
    }
}