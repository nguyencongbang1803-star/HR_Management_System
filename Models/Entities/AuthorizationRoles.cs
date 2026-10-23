namespace QLNS.Models.Entities;

public static class AuthorizationRoles
{
    public const string Employee = "NhanVien";
    public const string Manager = "QuanLy";
    public const string HumanResources = "HR";
    public const string Administrator = "Admin";

    public static readonly IReadOnlyList<string> All =
        [Employee, Manager, HumanResources, Administrator];

    public static bool IsValid(string? role) =>
        role is Employee or Manager or HumanResources or Administrator;

    public static string Normalize(string? role) =>
        IsValid(role) ? role! : Employee;

    public static IReadOnlyList<string> Parse(string? roles)
    {
        var result = (roles ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(IsValid).Distinct(StringComparer.Ordinal).ToList();
        return result.Count == 0 ? [Employee] : result;
    }

    public static string Serialize(IEnumerable<string>? roles)
    {
        var selected = (roles ?? []).Where(IsValid).Distinct(StringComparer.Ordinal).ToList();
        return string.Join(',', All.Where(selected.Contains).DefaultIfEmpty(Employee));
    }

    public static bool Contains(string? roles, string role) => Parse(roles).Contains(role);
}
