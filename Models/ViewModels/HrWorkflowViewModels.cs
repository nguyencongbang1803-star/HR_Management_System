using System.ComponentModel.DataAnnotations;
using QLNS.Models.Entities;

namespace QLNS.Models.ViewModels;

public class WorkforceRequestFormViewModel
{
    [Required] public int PhongBanID { get; set; }
    [Required, MaxLength(150)] public string ViTriCanBoSung { get; set; } = string.Empty;
    [Range(1, 100)] public int SoLuong { get; set; } = 1;
    [Required, MaxLength(1000)] public string LyDo { get; set; } = string.Empty;
}

public class WorkforcePlanViewModel
{
    [Required] public int ID { get; set; }
    [Required, RegularExpression("^(InternalTransfer|ExternalRecruitment)$")]
    public string HinhThucBoSung { get; set; } = WorkforceSupplyMethods.ExternalRecruitment;
    [Required, MaxLength(4000)] public string MoTaCongViec { get; set; } = string.Empty;
    public int? NhanVienNoiBoID { get; set; }
    [MaxLength(1000)] public string? GhiChuXuLy { get; set; }
}

public class WorkforceStageViewModel
{
    [Required] public int ID { get; set; }
    [Required, RegularExpression("^(Interviewing|Onboarding|Completed|Rejected)$")]
    public string TrangThai { get; set; } = WorkforceRequestStatuses.Interviewing;
    [MaxLength(150)] public string? TenUngVien { get; set; }
    [DataType(DataType.Date)] public DateTime? NgayPhongVan { get; set; }
    public int? NhanVienNoiBoID { get; set; }
    [MaxLength(1000)] public string? GhiChuXuLy { get; set; }
}

public class PerformanceReviewFormViewModel : IValidatableObject
{
    [Required] public int NhanVienID { get; set; }
    [Required, DataType(DataType.Date)] public DateTime TuNgay { get; set; } = new(DateTime.Today.Year, 1, 1);
    [Required, DataType(DataType.Date)] public DateTime DenNgay { get; set; } = DateTime.Today;
    [Range(1, 5)] public int ChatLuongCongViec { get; set; } = 3;
    [Range(1, 5)] public int TienDoCongViec { get; set; } = 3;
    [Range(1, 5)] public int TinhThanHopTac { get; set; } = 3;
    [Range(1, 5)] public int KyLuat { get; set; } = 3;
    [MaxLength(2000)] public string? DiemManh { get; set; }
    [MaxLength(2000)] public string? NoiDungCanCaiThien { get; set; }
    [MaxLength(1000)] public string? DeXuat { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DenNgay.Date < TuNgay.Date)
            yield return new ValidationResult("Ngày kết thúc phải từ ngày bắt đầu trở đi.", [nameof(DenNgay)]);
        if ((DenNgay.Date - TuNgay.Date).TotalDays > 366)
            yield return new ValidationResult("Một kỳ đánh giá không được dài quá 12 tháng.", [nameof(DenNgay)]);
    }
}

public class HrWorkflowIndexViewModel
{
    public IReadOnlyList<YeuCauNhanLuc> WorkforceRequests { get; set; } = [];
    public IReadOnlyList<DanhGiaHieuSuat> PerformanceReviews { get; set; } = [];
    public IReadOnlyList<PhongBan> Departments { get; set; } = [];
    public IReadOnlyList<NhanVien> Employees { get; set; } = [];
}
