using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLNS.Models.Entities
{
    public class PhuCap
    {
        [Key]
        public int MaPhuCap { get; set; }
        public int NhanVienID { get; set; }
        public NhanVien? NhanVien { get; set; }


        [Required, MaxLength(150)]
        [Display(Name = "Tên phụ cấp")]
        public string TenPhuCap { get; set; } = string.Empty;
        
        
        [Display(Name = "Ngày áp dụng")]
        public DateTime NgayApDung { get; set; }


        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Số tiền")]
        public decimal SoTien { get; set; }

        [MaxLength(500)]
        [Display(Name = "Ghi chú")]
        public string? GhiChu { get; set; }
    }
}