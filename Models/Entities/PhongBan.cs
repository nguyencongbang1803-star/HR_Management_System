using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLNS.Models.Entities
{
    public class PhongBan
    {
        public int ID { get; set; }

        [Required, MaxLength(150)]
        [Display(Name = "Tên phòng ban")]
        public string Tenphongban { get; set; } = string.Empty;

        [Display(Name = "Mã trưởng phòng")]
        public int? Matruongphong { get; set; }

        [Display(Name = "Phòng ban cấp trên")]
        public int? PhongBanChaID { get; set; }

        [ForeignKey(nameof(PhongBanChaID))]
        public PhongBan? PhongBanCha { get; set; }

        public ICollection<PhongBan> PhongBanCons { get; set; } = new List<PhongBan>();

        public ICollection<NhanVien> NhanViens { get; set; } = new List<NhanVien>();
    }
}
