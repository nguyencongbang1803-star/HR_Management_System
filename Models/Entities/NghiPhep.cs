using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLNS.Models.Entities;

public class NghiPhep
{
    public int ID { get; set; }
    public int NhanVienID { get; set; }
    public int? LoaiNghiPhepID { get; set; }

    [Required, MaxLength(50)]
    [Display(Name = "Loại nghỉ")]
    public string LoaiNghi { get; set; } = "Nghỉ phép năm";

    [Display(Name = "Ngày bắt đầu")]
    public DateTime Ngaybatdau { get; set; }

    [Display(Name = "Ngày kết thúc")]
    public DateTime NgayKetThuc { get; set; }

    [Required, MaxLength(500)]
    [Display(Name = "Lý do")]
    public string Lydo { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    [Display(Name = "Trạng thái")]
    public string Trangthai { get; set; } = LeaveStatuses.Pending;

    [MaxLength(500)]
    [Display(Name = "Phản hồi")]
    public string? PhanHoi { get; set; }

    [MaxLength(150)]
    public string? NguoiDuyet { get; set; }
    public DateTime? NgayDuyet { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    [Display(Name = "Trường hợp đột xuất")]
    public bool LaDotXuat { get; set; }

    public int? SoNgayHuongLuong { get; set; }
    public int? SoNgayKhongLuong { get; set; }

    [MaxLength(500)] public string? PhanHoiQuanLy { get; set; }
    [MaxLength(150)] public string? NguoiDuyetQuanLy { get; set; }
    public DateTime? NgayDuyetQuanLy { get; set; }

    [MaxLength(260)] public string? TenFileMinhChung { get; set; }
    [MaxLength(260)] public string? DuongDanMinhChung { get; set; }
    [MaxLength(100)] public string? ContentTypeMinhChung { get; set; }

    [ForeignKey(nameof(NhanVienID))]
    public NhanVien? NhanVien { get; set; }
    [ForeignKey(nameof(LoaiNghiPhepID))]
    public LoaiNghiPhep? LoaiNghiPhep { get; set; }
    public ICollection<NghiPhepMinhChung> MinhChungs { get; set; } = new List<NghiPhepMinhChung>();

    [NotMapped]
    public int SoNgay => NgayKetThuc.Date < Ngaybatdau.Date
        ? 0
        : (NgayKetThuc.Date - Ngaybatdau.Date).Days + 1;
}

public class NghiPhepMinhChung
{
    public int ID { get; set; }
    public int NghiPhepID { get; set; }
    [Required, MaxLength(260)] public string TenFileGoc { get; set; } = string.Empty;
    [Required, MaxLength(500)] public string DuongDan { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string ContentType { get; set; } = "application/octet-stream";
    public long KichThuoc { get; set; }
    public DateTime NgayTaiLen { get; set; } = DateTime.UtcNow;
    [ForeignKey(nameof(NghiPhepID))] public NghiPhep? NghiPhep { get; set; }
}
