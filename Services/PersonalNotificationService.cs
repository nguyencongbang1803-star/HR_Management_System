using Microsoft.EntityFrameworkCore;
using QLNS.Data;
using QLNS.Models.Entities;

namespace QLNS.Services;

public sealed class PersonalNotificationService(AppDbContext context)
{
    public async Task<int> SyncAsync(int employeeId,CancellationToken cancellationToken=default)
    {
        var existing=(await context.ThongBaoCaNhans.Where(x=>x.NhanVienID==employeeId)
            .ToListAsync(cancellationToken)).ToDictionary(x=>x.MaThamChieu,StringComparer.Ordinal);
        var added=0;var changed=false;
        void Add(string type,string key,string title,string content,string? url=null,DateTime? created=null)
        {
            if(existing.TryGetValue(key,out var notification))
            {
                if(notification.Loai==type&&notification.TieuDe==title&&notification.NoiDung==content&&notification.DuongDan==url)return;
                notification.Loai=type;notification.TieuDe=title;notification.NoiDung=content;notification.DuongDan=url;changed=true;return;
            }
            notification=new ThongBaoCaNhan{NhanVienID=employeeId,Loai=type,MaThamChieu=key,TieuDe=title,NoiDung=content,DuongDan=url,NgayTao=created??DateTime.UtcNow};
            context.ThongBaoCaNhans.Add(notification);existing[key]=notification;added++;changed=true;
        }

        var leaves=await context.NghiPheps.AsNoTracking().Where(x=>x.NhanVienID==employeeId).ToListAsync(cancellationToken);
        foreach(var x in leaves)Add("YeuCau",$"leave:{x.ID}:{x.Trangthai}","Đơn nghỉ phép",WithFeedback($"Đơn nghỉ {x.Ngaybatdau:dd/MM/yyyy} – {x.NgayKetThuc:dd/MM/yyyy}: {Status(x.Trangthai)}.",x.PhanHoiQuanLy,x.PhanHoi),"/Employee/Home/RequestLeave",x.NgayDuyet??x.NgayDuyetQuanLy??x.NgayTao);

        var overtime=await context.DangKyTangCas.AsNoTracking().Where(x=>x.NhanVienID==employeeId).ToListAsync(cancellationToken);
        foreach(var x in overtime)Add("YeuCau",$"overtime:{x.ID}:{x.TrangThai}","Đơn yêu cầu tăng ca",WithFeedback($"Tăng ca ngày {x.Ngay:dd/MM/yyyy}, {x.GioBatDau:hh\\:mm}–{x.GioKetThuc:hh\\:mm}: {Status(x.TrangThai)}.",x.PhanHoiQuanLy,x.PhanHoi),"/Employee/Home/RequestOvertime",x.NgayDuyet??x.NgayDuyetQuanLy??x.NgayTao);

        var adjustments=await context.DieuChinhChamCongs.AsNoTracking().Where(x=>x.NhanVienID==employeeId).ToListAsync(cancellationToken);
        foreach(var x in adjustments)Add("YeuCau",$"attendance-adjustment:{x.ID}:{x.TrangThai}","Yêu cầu sửa công",WithFeedback($"Yêu cầu sửa công ngày {x.Ngay:dd/MM/yyyy}: {Status(x.TrangThai)}.",null,x.PhanHoi), $"/Employee/Home/Attendance?thang={x.Ngay.Month}&nam={x.Ngay.Year}",x.NgayDuyet??x.NgayTao);

        var profileChanges=await context.YeuCauCapNhatThongTinCaNhans.AsNoTracking().Where(x=>x.NhanVienID==employeeId).ToListAsync(cancellationToken);
        foreach(var x in profileChanges)Add("YeuCau",$"profile:{x.ID}:{x.TrangThai}","Yêu cầu đổi hồ sơ",WithFeedback($"Yêu cầu thay đổi thông tin cá nhân: {Status(x.TrangThai)}.",null,x.PhanHoi),"/Employee/Home/RequestPersonalInformationChange",x.NgayDuyet??x.NgayTao);

        var payrolls=await context.Luongs.AsNoTracking().Where(x=>x.NhanVienID==employeeId&&x.TrangThai!=PayrollStatuses.Draft).ToListAsync(cancellationToken);
        foreach(var x in payrolls)Add("BangLuong",$"payroll:{x.ID}:{x.TrangThai}","Bảng lương",$"Bảng lương tháng {x.Thang:00}/{x.Nam} đã được cập nhật: {Payroll(x.TrangThai)}. Thực lĩnh {x.Tongluong:N0} ₫.",$"/Employee/Home/Attendance?thang={x.Thang}&nam={x.Nam}",x.NgayKhoa??x.NgayDoiSoat??x.NgayTinh);

        var lateRecords=await context.ChamCongs.AsNoTracking().Where(x=>x.NhanVienID==employeeId&&x.PhutDiMuon>0).ToListAsync(cancellationToken);
        foreach(var x in lateRecords)Add("DiTre",$"late:{x.ID}","Đi trễ",$"Bạn chấm công lúc {x.Giovaolam:HH:mm} ngày {x.Ngay:dd/MM/yyyy}, sau 08:15 và bị trừ 1 giờ làm.",$"/Employee/Home/Attendance?thang={x.Ngay.Month}&nam={x.Ngay.Year}",x.Giovaolam);

        if(changed)await context.SaveChangesAsync(cancellationToken);return added;
    }

    private static string WithFeedback(string content,string? managerFeedback,string? hrFeedback)
    {
        if(!string.IsNullOrWhiteSpace(managerFeedback))content+=$"\nPhản hồi quản lý: {Limit(managerFeedback.Trim(),400)}";
        if(!string.IsNullOrWhiteSpace(hrFeedback))content+=$"\nPhản hồi HR: {Limit(hrFeedback.Trim(),400)}";
        return Limit(content,1000);
    }

    private static string Limit(string value,int maxLength)=>value.Length<=maxLength?value:$"{value[..(maxLength-1)]}…";

    private static string Status(string value)=>value switch{ApprovalStatuses.Pending=>"Đang chờ duyệt",ApprovalStatuses.ManagerApproved=>"Quản lý đã duyệt, đang chờ HR",ApprovalStatuses.Approved=>"Đã duyệt",ApprovalStatuses.Rejected=>"Đã từ chối",LeaveStatuses.Cancelled=>"Đã hủy",_=>value};
    private static string Payroll(string value)=>value switch{PayrollStatuses.Reconciled=>"Đang đối soát",PayrollStatuses.Locked=>"Đã chốt",PayrollStatuses.Paid=>"Đã thanh toán",_=>value};
}
