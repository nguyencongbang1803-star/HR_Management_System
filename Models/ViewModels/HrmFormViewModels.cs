using System.ComponentModel.DataAnnotations;

namespace QLNS.Models.ViewModels;

public class DepartmentViewModel
{
    public int ID { get; set; }

    [Required, MaxLength(150)]
    [Display(Name = "Tên phòng ban")]
    public string Tenphongban { get; set; } = string.Empty;

    [Display(Name = "Trưởng phòng")]
    public int? Matruongphong { get; set; }

    [Display(Name = "Phòng ban cấp trên")]
    public int? PhongBanChaID { get; set; }
}

public class PositionViewModel
{
    public int ID { get; set; }

    [Required, MaxLength(100)]
    [Display(Name = "Tên chức vụ")]
    public string Chucvu { get; set; } = string.Empty;

    [Range(0, 100)]
    [Display(Name = "Hệ số lương")]
    public decimal? Hesoluong { get; set; } = 1m;

    [Range(0, 365)]
    [Display(Name = "Số ngày phép năm")]
    public decimal SoNgayPhepNam { get; set; } = 16m;

    [MinLength(1, ErrorMessage="Vui lòng chọn ít nhất một nhóm quyền.")]
    [Display(Name = "Nhóm quyền")]
    public string[] PhanQuyens { get; set; } = [Entities.AuthorizationRoles.Employee];
}

public class ContractFormViewModel : IValidatableObject
{
    public int ID { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn nhân viên.")]
    [Display(Name = "Nhân viên")]
    public int NhanVienID { get; set; }

    [Required, MaxLength(50)]
    [Display(Name = "Số hợp đồng")]
    public string SoHopDong { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    [Display(Name = "Loại hợp đồng")]
    public string Loaihopdong { get; set; } = string.Empty;

    [Required, DataType(DataType.Date)]
    [Display(Name = "Ngày ký")]
    public DateTime Ngayky { get; set; } = DateTime.Today;

    [Required, DataType(DataType.Date)]
    [Display(Name = "Ngày bắt đầu")]
    public DateTime Ngaybatdau { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    [Display(Name = "Ngày kết thúc")]
    public DateTime? Ngayketthuc { get; set; }

    [Range(0, 999999999999)]
    [Display(Name = "Lương cơ bản")]
    public decimal Luongcoban { get; set; }

    [Display(Name = "Đang hiệu lực")]
    public bool Trangthai { get; set; } = true;

    [Display(Name = "Hợp đồng trước")]
    public int? HopDongTruocID { get; set; }

    [Display(Name = "File hợp đồng")]
    public List<IFormFile> FilesHopDong { get; set; } = [];
    public string? TenFileHienTai { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Entities.ContractTypes.All.Contains(Loaihopdong))
            yield return new ValidationResult("Vui lòng chọn loại hợp đồng trong danh mục.", [nameof(Loaihopdong)]);
        var isIndefinite = Loaihopdong == Entities.ContractTypes.IndefiniteTerm;
        if (!isIndefinite && !Ngayketthuc.HasValue)
            yield return new ValidationResult("Loại hợp đồng này phải có ngày kết thúc.", [nameof(Ngayketthuc)]);
        if (Ngayketthuc.HasValue && Ngayketthuc.Value.Date < Ngaybatdau.Date)
            yield return new ValidationResult("Ngày kết thúc phải từ ngày bắt đầu trở đi.", [nameof(Ngayketthuc)]);
        if (Ngayky.Date > Ngaybatdau.Date.AddMonths(3))
            yield return new ValidationResult("Ngày ký không hợp lệ so với ngày bắt đầu.", [nameof(Ngayky)]);
    }
}

public class AttendanceFormViewModel : IValidatableObject
{
    public int ID { get; set; }

    [Required]
    [Display(Name = "Nhân viên")]
    public int NhanVienID { get; set; }

    [Required, DataType(DataType.Date)]
    [Display(Name = "Ngày")]
    public DateTime Ngay { get; set; } = DateTime.Today;

    [DataType(DataType.Time)]
    [Display(Name = "Giờ vào")]
    public TimeSpan? GioVao { get; set; } = new(8, 0, 0);

    [DataType(DataType.Time)]
    [Display(Name = "Giờ ra")]
    public TimeSpan? GioRa { get; set; } = new(17, 0, 0);

    [Required, MaxLength(30)]
    [Display(Name = "Trạng thái")]
    public string TrangThai { get; set; } = Entities.AttendanceStatuses.Present;

    [MaxLength(500)]
    [Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var needsTime = TrangThai is Entities.AttendanceStatuses.Present or Entities.AttendanceStatuses.Remote;
        if (needsTime && (!GioVao.HasValue || !GioRa.HasValue))
            yield return new ValidationResult("Ngày làm việc phải có đủ giờ vào và giờ ra.", [nameof(GioVao), nameof(GioRa)]);
        if (GioVao.HasValue && GioRa.HasValue && GioRa <= GioVao)
            yield return new ValidationResult("Giờ ra phải sau giờ vào.", [nameof(GioRa)]);
    }
}

public class LeaveFormViewModel : IValidatableObject
{
    [Required]
    [Display(Name = "Nhân viên")]
    public int NhanVienID { get; set; }

    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public int? LoaiNghiPhepID { get; set; }

    [Required, DataType(DataType.Date)]
    [Display(Name = "Từ ngày")]
    public DateTime Ngaybatdau { get; set; } = DateTime.Today;

    [Required, DataType(DataType.Date)]
    [Display(Name = "Đến ngày")]
    public DateTime NgayKetThuc { get; set; } = DateTime.Today;

    [Required, MaxLength(500)]
    [Display(Name = "Lý do")]
    public string Lydo { get; set; } = string.Empty;

    [Display(Name = "Nghỉ đột xuất (không thể báo trước 3 ngày)")]
    public bool LaDotXuat { get; set; }

    [Required, MinLength(1, ErrorMessage = "Vui lòng đính kèm ít nhất một file minh chứng.")]
    [Display(Name = "File minh chứng")]
    public List<IFormFile> FilesMinhChung { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (NgayKetThuc.Date < Ngaybatdau.Date)
            yield return new ValidationResult("Ngày kết thúc phải từ ngày bắt đầu trở đi.", [nameof(NgayKetThuc)]);
        if ((NgayKetThuc.Date - Ngaybatdau.Date).TotalDays > 90)
            yield return new ValidationResult("Một đơn nghỉ không được vượt quá 90 ngày.", [nameof(NgayKetThuc)]);
    }
}

public class QualificationFormViewModel : IValidatableObject
{
    [Required] public int NhanVienID { get; set; }
    [Required, RegularExpression("^(BangCap|ChungChi)$")] public string Loai { get; set; } = "BangCap";
    [Required, MaxLength(200)] public string Ten { get; set; } = string.Empty;
    [MaxLength(200)] public string? DonViCap { get; set; }
    [DataType(DataType.Date)] public DateTime? NgayCap { get; set; }
    [DataType(DataType.Date)] public DateTime? NgayHetHan { get; set; }
    [MaxLength(100)] public string? SoHieu { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (NgayHetHan.HasValue && NgayCap.HasValue && NgayHetHan < NgayCap)
            yield return new ValidationResult("Ngày hết hạn phải sau ngày cấp.", [nameof(NgayHetHan)]);
    }
}

public class WorkHistoryFormViewModel : IValidatableObject
{
    [Required] public int NhanVienID { get; set; }
    [Required, MaxLength(200)] public string DonVi { get; set; } = string.Empty;
    [Required, MaxLength(150)] public string ChucDanh { get; set; } = string.Empty;
    [Required, DataType(DataType.Date)] public DateTime TuNgay { get; set; } = DateTime.Today;
    [DataType(DataType.Date)] public DateTime? DenNgay { get; set; }
    [MaxLength(500)] public string? MoTa { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DenNgay.HasValue && DenNgay < TuNgay)
            yield return new ValidationResult("Ngày kết thúc phải sau ngày bắt đầu.", [nameof(DenNgay)]);
    }
}

public class EmployeeDocumentFormViewModel
{
    [Required] public int NhanVienID { get; set; }
    [Required, MaxLength(150)] public string TenTaiLieu { get; set; } = string.Empty;
    [Required, MinLength(1, ErrorMessage = "Vui lòng chọn ít nhất một file.")]
    public List<IFormFile> Files { get; set; } = [];
}

public class LeaveBalanceFormViewModel
{
    [Required] public int NhanVienID { get; set; }
    [Required] public int LoaiNghiPhepID { get; set; }
    [Range(2000, 2200)] public int Nam { get; set; } = DateTime.Today.Year;
    [Range(0, 365)] public decimal SoNgayDuocHuong { get; set; } = 16;
    [Range(-365, 365)] public decimal DieuChinh { get; set; }
    [MaxLength(300)] public string? GhiChu { get; set; }
}

public class AttendanceAdjustmentFormViewModel : IValidatableObject
{
    public int ID { get; set; }
    [Range(1,int.MaxValue)] public int ChamCongID { get; set; }
    [Required, DataType(DataType.Date)] public DateTime Ngay { get; set; } = DateTime.Today;
    [DataType(DataType.Time)] public TimeSpan? GioVao { get; set; }
    [DataType(DataType.Time)] public TimeSpan? GioRa { get; set; }
    [Required, MaxLength(500)] public string LyDo { get; set; } = string.Empty;
    [Required, MinLength(1, ErrorMessage="Vui lòng tải ít nhất một ảnh minh chứng.")]
    [Display(Name="Ảnh minh chứng")] public List<IFormFile> AnhMinhChung { get; set; } = [];
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!GioVao.HasValue && !GioRa.HasValue)
            yield return new ValidationResult("Cần nhập ít nhất giờ vào hoặc giờ ra.", [nameof(GioVao), nameof(GioRa)]);
        if (GioVao.HasValue && GioRa.HasValue && GioRa <= GioVao)
            yield return new ValidationResult("Giờ ra phải sau giờ vào.", [nameof(GioRa)]);
    }
}

public class OvertimeRequestViewModel : IValidatableObject
{
    [Required,DataType(DataType.Date)] public DateTime Ngay { get; set; }=DateTime.Today;
    [Required,DataType(DataType.Time)] public TimeSpan GioBatDau { get; set; }=new(18,0,0);
    [Required,DataType(DataType.Time)] public TimeSpan GioKetThuc { get; set; }=new(20,0,0);
    [Required,MaxLength(500)] public string LyDo { get; set; }=string.Empty;
    [Display(Name="Tệp đính kèm (không bắt buộc)")] public List<IFormFile> FilesMinhChung { get; set; }=[];
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext){var hours=(GioKetThuc-GioBatDau).TotalHours;if(hours<=0||hours>12)yield return new ValidationResult("Thời gian tăng ca phải lớn hơn 0 và không quá 12 giờ.",[nameof(GioKetThuc)]);}
}

public class PersonalInformationChangeViewModel
{
    [MaxLength(150), EmailAddress, Display(Name="Email")] public string? Email { get; set; }
    [MaxLength(15), Phone, Display(Name="Số điện thoại")] public string? SDT { get; set; }
    [DataType(DataType.Date), Display(Name="Ngày sinh")] public DateTime? NgaySinh { get; set; }
    [Display(Name="Giới tính nam")] public bool GioiTinh { get; set; }
    [MaxLength(20), Display(Name="CCCD/Hộ chiếu")] public string? CanCuocCongDan { get; set; }
    [MaxLength(300), Display(Name="Địa chỉ")] public string? DiaChi { get; set; }
    [MaxLength(30), Display(Name="Số tài khoản")] public string? SoTaiKhoan { get; set; }
    [MaxLength(20), Display(Name="Mã số thuế")] public string? MaSoThue { get; set; }
    [Required, MaxLength(500), Display(Name="Lý do thay đổi")] public string LyDo { get; set; } = string.Empty;
}

public class LeaveDecisionViewModel
{
    public int ID { get; set; }

    [Required]
    public string Decision { get; set; } = string.Empty;

    [MaxLength(500)]
    [Display(Name = "Phản hồi")]
    public string? Response { get; set; }
}

public class DashboardViewModel
{
    public int ActiveEmployees { get; set; }
    public int Departments { get; set; }
    public int PendingLeaves { get; set; }
    public int ExpiringContracts { get; set; }
    public decimal CurrentPayroll { get; set; }
    public IReadOnlyList<Entities.NghiPhep> RecentLeaves { get; set; } = [];
    public IReadOnlyList<Entities.HopDong> ContractsExpiringSoon { get; set; } = [];
}

public class AccountCreateViewModel
{
    [Required, Display(Name = "Nhân viên")]
    public int? NhanVienID { get; set; }

    [Required, MinLength(8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự.")]
    [RegularExpression(@"^(?=.*\d)(?=.*[^\p{L}\p{N}]).{8,}$",
        ErrorMessage = "Mật khẩu phải có ít nhất một chữ số và một ký tự đặc biệt.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu tạm")]
    public string Password { get; set; } = string.Empty;

}

public class DepartmentSummaryViewModel
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class AllowanceFormViewModel
{
    [Required] public int NhanVienID { get; set; }
    [Required, MaxLength(150)] public string TenPhuCap { get; set; } = string.Empty;
    [DataType(DataType.Date)] public DateTime NgayApDung { get; set; } = DateTime.Today;
    [Range(0, 999999999999)] public decimal SoTien { get; set; }
    [MaxLength(500)] public string? GhiChu { get; set; }
}

public class AdjustmentFormViewModel
{
    [Required] public int NhanVienID { get; set; }
    [Required, RegularExpression("^(Thuong|Phat|ThuNhapKhac)$")] public string Loai { get; set; } = "Thuong";
    [Required, MaxLength(500)] public string LyDo { get; set; } = string.Empty;
    [Range(0, 999999999999)] public decimal SoTien { get; set; }
    [DataType(DataType.Date)] public DateTime NgayApDung { get; set; } = DateTime.Today;
}

public class InsuranceFormViewModel : IValidatableObject
{
    [Required] public int NhanVienID { get; set; }
    [Required] public int LoaiBaoHiemID { get; set; }
    [Range(0, 999999999999)] public decimal SoTienDong { get; set; }
    [DataType(DataType.Date)] public DateTime NgayBatDau { get; set; } = DateTime.Today;
    [DataType(DataType.Date)] public DateTime? NgayKetThuc { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (NgayKetThuc.HasValue && NgayKetThuc < NgayBatDau)
            yield return new ValidationResult("Ngày kết thúc phải sau ngày bắt đầu.", [nameof(NgayKetThuc)]);
    }
}
