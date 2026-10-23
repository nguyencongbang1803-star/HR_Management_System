using QLNS.Models.Entities;

namespace QLNS.Models.ViewModels;

public class DepartmentIndexViewModel
{
    public IReadOnlyList<DepartmentListItemViewModel> Items { get; set; } = [];
    public IReadOnlyList<DepartmentTreeNodeViewModel> Roots { get; set; } = [];
    public string CompanyName { get; set; } = "Công ty TNHH Giải Pháp Công Nghệ Thông Tin Golden Bee";
    public string? Search { get; set; }
    public string? ManagerSearch { get; set; }
    public string Sort { get; set; } = "name";
    public bool Descending { get; set; }
}

public class DepartmentTreeNodeViewModel
{
    public required DepartmentListItemViewModel Item { get; set; }
    public List<DepartmentTreeNodeViewModel> Children { get; set; } = [];
}

public class DepartmentListItemViewModel
{
    public required PhongBan Department { get; set; }
    public string? ManagerName { get; set; }
    public int EmployeeCount { get; set; }
    public IReadOnlyList<NhanVien> Employees { get; set; } = [];
}

public class PositionIndexViewModel
{
    public IReadOnlyList<PositionListItemViewModel> Items { get; set; } = [];
    public string? Search { get; set; }
    public decimal? MinCoefficient { get; set; }
    public decimal? MaxCoefficient { get; set; }
    public string Sort { get; set; } = "name";
    public bool Descending { get; set; }
}

public class PositionListItemViewModel
{
    public required ChucVu Position { get; set; }
    public int EmployeeCount { get; set; }
    public IReadOnlyList<NhanVien> Employees { get; set; } = [];
}
