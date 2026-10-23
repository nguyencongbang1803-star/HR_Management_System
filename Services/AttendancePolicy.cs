using QLNS.Models.Entities;

namespace QLNS.Services;

public static class AttendancePolicy
{
    public static readonly TimeSpan LateThreshold = new(8, 15, 0);
    public const int LatePenaltyMinutes = 60;

    public static void ApplyLateRule(ChamCong attendance)
    {
        attendance.PhutDiMuon = attendance.TrangThai is AttendanceStatuses.Present or AttendanceStatuses.Remote
            && attendance.Giovaolam.HasValue
            && attendance.Giovaolam.Value.TimeOfDay > LateThreshold
                ? LatePenaltyMinutes
                : 0;
    }
}
