using System.ComponentModel.DataAnnotations;

namespace QLNS.Models.ViewModels;

public class EmployeeFormViewModel : IValidatableObject
{
    public int ID { get; set; }

    [Required, MaxLength(30)]
    [Display(Name = "Mã nhân viên")]
    public string MaNhanVien { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    [Display(Name = "Họ và tên")]
    public string Ten { get; set; } = string.Empty;

    [EmailAddress, MaxLength(150)]
    public string? Email { get; set; }

    [Phone, MaxLength(15)]
    [Display(Name = "Số điện thoại")]
    public string? SDT { get; set; }

    [Display(Name = "Giới tính")]
    public bool Gioitinh { get; set; } = true;

    [DataType(DataType.Date)]
    [Display(Name = "Ngày sinh")]
    public DateTime? NgaySinh { get; set; }

    [Required(ErrorMessage = "CCCD/Hộ chiếu không được để trống.")]
    [MaxLength(20)]
    [Display(Name = "CCCD/Hộ chiếu")]
    public string? CanCuocCongDan { get; set; }

    [MaxLength(300)]
    [Display(Name = "Địa chỉ")]
    public string? DiaChi { get; set; }

    [MaxLength(20)]
    [Display(Name = "Mã số thuế")]
    public string? MaSothue { get; set; }

    [MaxLength(30)]
    [Display(Name = "Số tài khoản")]
    public string? SoTaiKhoan { get; set; }

    [Required, DataType(DataType.Date)]
    [Display(Name = "Ngày vào làm")]
    public DateTime Ngayvaolam { get; set; } = DateTime.Today;

    [Display(Name = "Phòng ban")]
    public int? PhongBanID { get; set; }

    [Display(Name = "Chức vụ")]
    public int? ChucVuID { get; set; }

    public bool Trangthai { get; set; } = true;

    [Display(Name = "Ảnh đại diện")]
    public IFormFile? Avatar { get; set; }

    public string? CurrentAvatar { get; set; }

    [Display(Name = "Xóa ảnh đại diện hiện tại")]
    public bool RemoveAvatar { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (NgaySinh.HasValue && NgaySinh.Value.Date >= DateTime.Today)
            yield return new ValidationResult("Ngày sinh phải trước ngày hiện tại.", [nameof(NgaySinh)]);
        if (NgaySinh.HasValue && Ngayvaolam.Date < NgaySinh.Value.Date.AddYears(15))
            yield return new ValidationResult("Ngày vào làm không hợp lệ so với ngày sinh.", [nameof(Ngayvaolam)]);
    }
}

public class EmployeeIndexViewModel
{
    public IReadOnlyList<Entities.NhanVien> Items { get; set; } = [];
    public string? Search { get; set; }
    public int? DepartmentId { get; set; }
    public int? PositionId { get; set; }
    public bool ShowInactive { get; set; }
    public bool WithoutContract { get; set; }
    public int? ContractMonth { get; set; }
    public int? ContractYear { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; }
}
