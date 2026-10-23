using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLNS.Models.Entities;

public class NhanVien
{
    public int ID { get; set; }

    [Required, MaxLength(30)]
    [Display(Name = "Mã nhân viên")]
    public string MaNhanVien { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    [Display(Name = "Họ và tên")]
    public string Ten { get; set; } = string.Empty;

    [MaxLength(150), EmailAddress]
    public string? Email { get; set; }

    // Giữ lại để tương thích dữ liệu cũ; mật khẩu đăng nhập chỉ lưu ở TaiKhoan.Pass.
    [MaxLength(255)]
    public string? Matkhau { get; set; }

    [MaxLength(15), Phone]
    [Display(Name = "Số điện thoại")]
    public string? SDT { get; set; }

    [MaxLength(30)]
    [Display(Name = "Số tài khoản")]
    public string? SoTaiKhoan { get; set; }

    [Display(Name = "Giới tính")]
    public bool Gioitinh { get; set; }

    [Display(Name = "Ngày sinh")]
    public DateTime? NgaySinh { get; set; }

    [MaxLength(20)]
    [Display(Name = "CCCD/Hộ chiếu")]
    public string? CanCuocCongDan { get; set; }

    [MaxLength(300)]
    [Display(Name = "Địa chỉ")]
    public string? DiaChi { get; set; }

    [MaxLength(300)]
    [Display(Name = "Ảnh đại diện")]
    public string? AnhDaiDien { get; set; }

    [MaxLength(20)]
    [Display(Name = "Mã số thuế")]
    public string? MaSothue { get; set; }

    [Display(Name = "Đang làm việc")]
    public bool Trangthai { get; set; } = true;

    public DateTime Ngaytao { get; set; } = DateTime.UtcNow;

    [Display(Name = "Ngày vào làm")]
    public DateTime Ngayvaolam { get; set; } = DateTime.Today;

    [Display(Name = "Ngày nghỉ việc")]
    public DateTime? Ngayxoa { get; set; }

    public DateTime Ngaycapnhat { get; set; } = DateTime.UtcNow;

    [Display(Name = "Phòng ban")]
    public int? PhongBanID { get; set; }

    [Display(Name = "Chức vụ")]
    public int? ChucVuID { get; set; }

    [ForeignKey(nameof(PhongBanID))]
    public PhongBan? PhongBan { get; set; }

    [ForeignKey(nameof(ChucVuID))]
    public ChucVu? ChucVu { get; set; }

    public ICollection<Luong> Luongs { get; set; } = new List<Luong>();
    public ICollection<ChamCong> ChamCongs { get; set; } = new List<ChamCong>();
    public ICollection<HopDong> HopDongs { get; set; } = new List<HopDong>();
    public ICollection<NghiPhep> NghiPheps { get; set; } = new List<NghiPhep>();
    public TaiKhoan? TaiKhoan { get; set; }
    public ICollection<BaoHiem> BaoHiems { get; set; } = new List<BaoHiem>();
    public ICollection<ThuongPhat> ThuongPhats { get; set; } = new List<ThuongPhat>();
    public ICollection<TrinhDoChungChi> TrinhDoChungChis { get; set; } = new List<TrinhDoChungChi>();
    public ICollection<QuaTrinhCongTac> QuaTrinhCongTacs { get; set; } = new List<QuaTrinhCongTac>();
    public ICollection<TaiLieuNhanVien> TaiLieuNhanViens { get; set; } = new List<TaiLieuNhanVien>();
    public ICollection<SoDuNghiPhep> SoDuNghiPheps { get; set; } = new List<SoDuNghiPhep>();
    public ICollection<DieuChinhChamCong> DieuChinhChamCongs { get; set; } = new List<DieuChinhChamCong>();
    public ICollection<YeuCauCapNhatThongTinCaNhan> YeuCauCapNhatThongTinCaNhans { get; set; } = new List<YeuCauCapNhatThongTinCaNhan>();
    public ICollection<ThongBaoCaNhan> ThongBaoCaNhans { get; set; } = new List<ThongBaoCaNhan>();
    public ICollection<PhanCaNhanVien> PhanCaNhanViens { get; set; } = new List<PhanCaNhanVien>();
    public ICollection<DangKyTangCa> DangKyTangCas { get; set; } = new List<DangKyTangCa>();
    public ICollection<CongTac> CongTacs { get; set; } = new List<CongTac>();
    public ICollection<PhanCongCongTac> PhanCongCongTacs { get; set; } = new List<PhanCongCongTac>();
    public ICollection<YeuCauNhanLuc> YeuCauDieuChuyens { get; set; } = new List<YeuCauNhanLuc>();
    public ICollection<DanhGiaHieuSuat> DanhGiaHieuSuats { get; set; } = new List<DanhGiaHieuSuat>();
}
