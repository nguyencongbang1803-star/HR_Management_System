using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLNS.Models.Entities
{
    public class BaoHiem
    {
        [Key]
        public int MaBaoHiem { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Số tiền đóng")]
        public decimal SoTienDong { get; set; }

        [Display(Name = "Ngày bắt đầu")]
        public DateTime NgayBatDau { get; set; }

        [Display(Name = "Ngày kết thúc")]
        public DateTime? NgayKetThuc { get; set; }

        [MaxLength(50)]
        [Display(Name = "Trạng thái")]
        public string TrangThai { get; set; } = "DangDong"; // DangDong | NgungDong | HetHan

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }

        // FK liên kết NhanVien
        public int NhanVienID { get; set; }

        [ForeignKey("NhanVienID")]
        public NhanVien? NhanVien { get; set; }

        // FK liên kết LoaiBaoHiem
        public int LoaiBaoHiemID { get; set; }

        [ForeignKey("LoaiBaoHiemID")]
        public LoaiBaoHiem? LoaiBaoHiem { get; set; }
    }
}