using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace QLNS.Models.Entities;

public class TaiKhoan : IdentityUser<int>
{
    [NotMapped]
    public int MaTaiKhoan
    {
        get => Id;
        set => Id = value;
    }

    [Required, MaxLength(50)]
    [Display(Name = "Mã nhân viên")]
    public string MaNhanVien { get; set; } = string.Empty;

    // Bản sao tương thích; với tài khoản nhân viên, quyền thực tế lấy từ ChucVu.PhanQuyen.
    [Required, MaxLength(50)]
    [Display(Name = "Vai trò")]
    public string Role { get; set; } = "NhanVien";

    public int? NhanVienID { get; set; }

    [ForeignKey(nameof(NhanVienID))]
    public NhanVien? NhanVien { get; set; }
}
