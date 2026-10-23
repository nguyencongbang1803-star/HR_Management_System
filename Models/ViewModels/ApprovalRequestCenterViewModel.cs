using QLNS.Models.Entities;

namespace QLNS.Models.ViewModels;

public sealed class ApprovalRequestCenterViewModel
{
    public IReadOnlyList<NghiPhep> Leaves { get; init; } = [];
    public IReadOnlySet<int> DecidableLeaveIds { get; init; } = new HashSet<int>();
    public IReadOnlyList<DangKyTangCa> OvertimeRequests { get; init; } = [];
    public IReadOnlySet<int> DecidableOvertimeIds { get; init; } = new HashSet<int>();
    public IReadOnlyList<DieuChinhChamCong> AttendanceAdjustments { get; init; } = [];
    public IReadOnlyList<YeuCauCapNhatThongTinCaNhan> ProfileChanges { get; init; } = [];
    public bool CanReviewHrRequests { get; init; }

    public int PendingLeaves => Leaves.Count(x =>
        DecidableLeaveIds.Contains(x.ID) &&
        x.Trangthai is LeaveStatuses.Pending or ApprovalStatuses.ManagerApproved);
    public int PendingOvertime => OvertimeRequests.Count(x => DecidableOvertimeIds.Contains(x.ID));
    public int PendingAttendanceAdjustments => AttendanceAdjustments.Count(x => x.TrangThai == ApprovalStatuses.Pending);
    public int PendingProfileChanges => ProfileChanges.Count(x => x.TrangThai == ApprovalStatuses.Pending);
}
