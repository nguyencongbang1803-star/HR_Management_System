using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLNS.Models.Entities;

public class TaiLieuNoiBo
{
    public int ID { get; set; }
    [Required,MaxLength(200)] public string TieuDe { get; set; }=string.Empty;
    [Required,MaxLength(50)] public string DanhMuc { get; set; }="ChinhSach";
    [MaxLength(4000)] public string? TomTat { get; set; }
    [Required,MaxLength(30)] public string PhamVi { get; set; }="All";
    public int? PhongBanID { get; set; }
    public int? ChucVuID { get; set; }
    [MaxLength(50)] public string? VaiTro { get; set; }
    public bool BatBuocXacNhan { get; set; }
    public bool DaPhatHanh { get; set; }=true;
    [MaxLength(260)] public string? TenFileGoc { get; set; }
    [MaxLength(260)] public string? DuongDan { get; set; }
    [MaxLength(100)] public string? ContentType { get; set; }
    public long KichThuoc { get; set; }
    public DateTime NgayPhatHanh { get; set; }=DateTime.UtcNow;
    [MaxLength(150)] public string NguoiTao { get; set; }=string.Empty;
    [ForeignKey(nameof(PhongBanID))] public PhongBan? PhongBan { get; set; }
    [ForeignKey(nameof(ChucVuID))] public ChucVu? ChucVu { get; set; }
    public ICollection<XacNhanTaiLieu> XacNhans { get; set; }=new List<XacNhanTaiLieu>();
    public ICollection<TepTaiLieuNoiBo> TepDinhKems { get; set; }=new List<TepTaiLieuNoiBo>();
}

public class TepTaiLieuNoiBo
{
    public int ID { get; set; }
    public int TaiLieuNoiBoID { get; set; }
    [Required, MaxLength(260)] public string TenFileGoc { get; set; } = string.Empty;
    [Required, MaxLength(500)] public string DuongDan { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string ContentType { get; set; } = "application/octet-stream";
    public long KichThuoc { get; set; }
    public DateTime NgayTaiLen { get; set; } = DateTime.UtcNow;
    [ForeignKey(nameof(TaiLieuNoiBoID))] public TaiLieuNoiBo? TaiLieuNoiBo { get; set; }
}

public class XacNhanTaiLieu
{
    public int ID { get; set; }
    public int TaiLieuNoiBoID { get; set; }
    public int TaiKhoanID { get; set; }
    public DateTime ThoiGian { get; set; }=DateTime.UtcNow;
    [ForeignKey(nameof(TaiLieuNoiBoID))] public TaiLieuNoiBo? TaiLieu { get; set; }
    [ForeignKey(nameof(TaiKhoanID))] public TaiKhoan? TaiKhoan { get; set; }
}

public class ThongBaoNoiBo
{
    public int ID { get; set; }
    [Required,MaxLength(200)] public string TieuDe { get; set; }=string.Empty;
    [Required,MaxLength(4000)] public string NoiDung { get; set; }=string.Empty;
    [Required,MaxLength(30)] public string PhamVi { get; set; }="All";
    public int? PhongBanID { get; set; }
    public int? ChucVuID { get; set; }
    [MaxLength(50)] public string? VaiTro { get; set; }
    public bool Ghim { get; set; }
    public DateTime BatDau { get; set; }=DateTime.UtcNow;
    public DateTime? KetThuc { get; set; }
    [MaxLength(150)] public string NguoiTao { get; set; }=string.Empty;
    [ForeignKey(nameof(PhongBanID))] public PhongBan? PhongBan { get; set; }
    [ForeignKey(nameof(ChucVuID))] public ChucVu? ChucVu { get; set; }
    public ICollection<BinhLuanThongBao> BinhLuans { get; set; }=new List<BinhLuanThongBao>();
}

public class BinhLuanThongBao
{
    public int ID { get; set; }
    public int ThongBaoNoiBoID { get; set; }
    public int TaiKhoanID { get; set; }
    [Required,MaxLength(1000)] public string NoiDung { get; set; }=string.Empty;
    public DateTime NgayTao { get; set; }=DateTime.UtcNow;
    [ForeignKey(nameof(ThongBaoNoiBoID))] public ThongBaoNoiBo? ThongBao { get; set; }
    [ForeignKey(nameof(TaiKhoanID))] public TaiKhoan? TaiKhoan { get; set; }
}

public class KhaoSatNoiBo
{
    public int ID { get; set; }
    [Required,MaxLength(200)] public string TieuDe { get; set; }=string.Empty;
    [Required,MaxLength(500)] public string CauHoi { get; set; }=string.Empty;
    public DateTime BatDau { get; set; }=DateTime.UtcNow;
    public DateTime? KetThuc { get; set; }
    public bool HoatDong { get; set; }=true;
    [MaxLength(150)] public string NguoiTao { get; set; }=string.Empty;
    public ICollection<LuaChonKhaoSat> LuaChons { get; set; }=new List<LuaChonKhaoSat>();
}

public class LuaChonKhaoSat
{
    public int ID { get; set; }
    public int KhaoSatNoiBoID { get; set; }
    [Required,MaxLength(300)] public string NoiDung { get; set; }=string.Empty;
    public ICollection<PhieuKhaoSat> PhieuKhaoSats { get; set; }=new List<PhieuKhaoSat>();
    [ForeignKey(nameof(KhaoSatNoiBoID))] public KhaoSatNoiBo? KhaoSat { get; set; }
}

public class PhieuKhaoSat
{
    public int ID { get; set; }
    public int KhaoSatNoiBoID { get; set; }
    public int LuaChonKhaoSatID { get; set; }
    public int TaiKhoanID { get; set; }
    public DateTime NgayTraLoi { get; set; }=DateTime.UtcNow;
    [ForeignKey(nameof(KhaoSatNoiBoID))] public KhaoSatNoiBo? KhaoSat { get; set; }
    [ForeignKey(nameof(LuaChonKhaoSatID))] public LuaChonKhaoSat? LuaChon { get; set; }
    [ForeignKey(nameof(TaiKhoanID))] public TaiKhoan? TaiKhoan { get; set; }
}
