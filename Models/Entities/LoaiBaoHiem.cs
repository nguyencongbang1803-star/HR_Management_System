using System.ComponentModel.DataAnnotations;

namespace QLNS.Models.Entities
{
    public class LoaiBaoHiem
    {
        [Key]
        public int MaLoaiBaoHiem { get; set; }

        [Required, MaxLength(150)]
        [Display(Name = "Tên loại bảo hiểm")]
        public string TenLoaiBaoHiem { get; set; } = string.Empty;

        public ICollection<BaoHiem> BaoHiems { get; set; } = new List<BaoHiem>();
    }
}