namespace QLNS.Models.Entities;

public static class LeaveStatuses
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Cancelled = "Cancelled";
}

public static class AttendanceStatuses
{
    public const string Present = "Present";
    public const string Remote = "Remote";
    public const string PaidLeave = "PaidLeave";
    public const string UnpaidLeave = "UnpaidLeave";
    public const string Absent = "Absent";
    public const string BusinessTrip = "BusinessTrip";
}

public static class PayrollStatuses
{
    public const string Draft = "Draft";
    public const string Reconciled = "Reconciled";
    public const string Locked = "Locked";
    public const string Paid = "Paid";
}

public static class HrmDisplay
{
    public static string Leave(string value) => value switch
    {
        ApprovalStatuses.ManagerApproved => "Chờ HR duyệt",
        LeaveStatuses.Approved => "Đã duyệt",
        LeaveStatuses.Rejected => "Từ chối",
        LeaveStatuses.Cancelled => "Đã hủy",
        _ => "Chờ duyệt"
    };

    public static string Attendance(string value) => value switch
    {
        AttendanceStatuses.Remote => "Làm từ xa",
        AttendanceStatuses.PaidLeave => "Nghỉ có lương",
        AttendanceStatuses.UnpaidLeave => "Nghỉ không lương",
        AttendanceStatuses.Absent => "Vắng",
        AttendanceStatuses.BusinessTrip => "Công tác",
        _ => "Có mặt"
    };

    public static string Payroll(string value) => value switch
    {
        PayrollStatuses.Reconciled => "Đã đối soát",
        PayrollStatuses.Locked => "Đã khóa",
        PayrollStatuses.Paid => "Đã thanh toán",
        _ => "Bản nháp"
    };
}
