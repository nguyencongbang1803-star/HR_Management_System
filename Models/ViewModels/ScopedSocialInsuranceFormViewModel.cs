using System.ComponentModel.DataAnnotations;
using QLNS.Models.Entities;

namespace QLNS.Models.ViewModels;

public sealed class ScopedSocialInsuranceFormViewModel : IValidatableObject
{
    public int ID { get; set; }

    [Required, MaxLength(150), Display(Name = "Tên khoản BHXH")]
    public string TenKhoan { get; set; } = string.Empty;

    [Range(1, 999999999999), Display(Name = "Số tiền đóng mỗi tháng")]
    public decimal SoTienDong { get; set; }

    [Required, Display(Name = "Quy mô áp dụng")]
    public string PhamVi { get; set; } = AllowanceScopes.Company;

    public int? PhongBanID { get; set; }
    public int? ChucVuID { get; set; }
    public int? NhanVienID { get; set; }

    [Required, DataType(DataType.Date), Display(Name = "Bắt đầu áp dụng")]
    public DateTime NgayBatDau { get; set; } = new(DateTime.Today.Year, DateTime.Today.Month, 1);

    [Required, Display(Name = "Thời gian")]
    public string KieuThoiHan { get; set; } = AllowanceDurations.Monthly;

    [Range(1, 120), Display(Name = "Số tháng")]
    public int? SoThang { get; set; }
    public bool HoatDong { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PhamVi is not (AllowanceScopes.Company or AllowanceScopes.Department or AllowanceScopes.Position or AllowanceScopes.Employee))
            yield return new ValidationResult("Quy mô áp dụng không hợp lệ.", [nameof(PhamVi)]);
        if (PhamVi == AllowanceScopes.Department && !PhongBanID.HasValue)
            yield return new ValidationResult("Vui lòng chọn phòng ban.", [nameof(PhongBanID)]);
        if (PhamVi == AllowanceScopes.Position && !ChucVuID.HasValue)
            yield return new ValidationResult("Vui lòng chọn chức vụ.", [nameof(ChucVuID)]);
        if (PhamVi == AllowanceScopes.Employee && !NhanVienID.HasValue)
            yield return new ValidationResult("Vui lòng chọn nhân viên.", [nameof(NhanVienID)]);
        if (KieuThoiHan is not (AllowanceDurations.Monthly or AllowanceDurations.FixedMonths))
            yield return new ValidationResult("Thời gian áp dụng không hợp lệ.", [nameof(KieuThoiHan)]);
        if (KieuThoiHan == AllowanceDurations.FixedMonths && !SoThang.HasValue)
            yield return new ValidationResult("Vui lòng nhập số tháng áp dụng.", [nameof(SoThang)]);
    }
}
