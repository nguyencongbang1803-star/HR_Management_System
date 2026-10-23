using System.ComponentModel.DataAnnotations;

namespace QLNS.Models.Entities
{
    public class ChucVu
    {
        public int ID { get; set; }

        [Required, MaxLength(100)]
        [Display(Name = "Chức vụ")]
        public string Chucvu { get; set; } = string.Empty;

        [Range(0, 100)]
        [Display(Name = "Hệ số lương")]
        public decimal? Hesoluong { get; set; } = 1m;

        [Range(0, 365)]
        [Display(Name = "Số ngày phép năm")]
        public decimal SoNgayPhepNam { get; set; } = 16m;

        [Required, MaxLength(50)]
        [Display(Name = "Nhóm quyền")]
        public string PhanQuyen { get; set; } = AuthorizationRoles.Employee;

        public ICollection<NhanVien> NhanViens { get; set; } = new List<NhanVien>();
    }
}
