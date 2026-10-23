using System.ComponentModel.DataAnnotations;

namespace QLNS.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập email hoặc mã nhân viên."), MaxLength(150)]
    [Display(Name = "Email hoặc mã nhân viên")]
    public string TaiKhoan { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu."), DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Ghi nhớ đăng nhập")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class ChangePasswordViewModel
{
    [Required, DataType(DataType.Password)]
    [Display(Name = "Mật khẩu hiện tại")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, MinLength(8, ErrorMessage = "Mật khẩu mới phải có ít nhất 8 ký tự.")]
    [RegularExpression(@"^(?=.*\d)(?=.*[^\p{L}\p{N}]).{8,}$",
        ErrorMessage = "Mật khẩu mới phải có ít nhất một chữ số và một ký tự đặc biệt.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu mới")]
    public string NewPassword { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(NewPassword))]
    [Display(Name = "Xác nhận mật khẩu mới")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
