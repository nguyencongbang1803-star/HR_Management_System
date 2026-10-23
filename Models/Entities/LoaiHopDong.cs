using System.ComponentModel.DataAnnotations;

namespace QLNS.Models.Entities;

public class LoaiHopDong
{
    public int ID { get; set; }

    [Required, MaxLength(100)]
    [Display(Name = "Tên loại hợp đồng")]
    public string TenLoai { get; set; } = string.Empty;
}

public static class ContractTypes
{
    public const string Collaborator = "Hợp đồng cộng tác viên";
    public const string Probation = "Hợp đồng thử việc";
    public const string VocationalTraining = "Hợp đồng đào tạo nghề";
    public const string IndefiniteTerm = "Hợp đồng lao động không xác định thời hạn";
    public const string FixedTerm = "Hợp đồng lao động xác định thời hạn";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Collaborator, Probation, VocationalTraining, IndefiniteTerm, FixedTerm
    };
}
