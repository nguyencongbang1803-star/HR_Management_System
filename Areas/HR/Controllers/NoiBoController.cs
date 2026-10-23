using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.Data;
using QLNS.Models.Entities;
using QLNS.Models.ViewModels;
using QLNS.Services;

namespace QLNS.Areas.HR.Controllers;

[Area("HR")]
[Authorize]
public class NoiBoController(AppDbContext context, FileStorageService files, AuditService audit) : Controller
{
    private static readonly HashSet<string> SupportedRoles =
        new(["NhanVien", "QuanLy", "HR", "Admin"], StringComparer.Ordinal);

    public async Task<IActionResult> Index(string? q = null)
    {
        var accountId = AccountId();
        if (!accountId.HasValue) return Challenge();

        ViewBag.PhongBans = await context.PhongBans.AsNoTracking()
            .OrderBy(x => x.Tenphongban).ToListAsync();
        ViewBag.ChucVus = await context.ChucVus.AsNoTracking()
            .OrderBy(x => x.Chucvu).ToListAsync();

        var roles = CurrentRoles();
        var departmentId = await DepartmentIdAsync(accountId.Value);
        var positionId = await PositionIdAsync(accountId.Value);
        var now = DateTime.UtcNow;

        var documents = await VisibleDocuments(roles, departmentId, positionId)
            .Include(x => x.TepDinhKems)
            .OrderByDescending(x => x.NgayPhatHanh).ToListAsync();
        var announcements = await VisibleAnnouncements(roles, departmentId, positionId, now)
            .Include(x => x.BinhLuans).ThenInclude(x => x.TaiKhoan)
            .OrderByDescending(x => x.Ghim).ThenByDescending(x => x.BatDau).ToListAsync();
        var surveys = await context.KhaoSatNoiBos.AsNoTracking()
            .Include(x => x.LuaChons).ThenInclude(x => x.PhieuKhaoSats)
            .AsSplitQuery()
            .Where(x => x.HoatDong && x.BatDau <= now && (x.KetThuc == null || x.KetThuc >= now))
            .OrderByDescending(x => x.BatDau).ToListAsync();

        var model = new InternalPortalViewModel
        {
            Documents = documents,
            Announcements = announcements,
            Surveys = surveys,
            ConfirmedDocumentIds = (await context.XacNhanTaiLieus
                .Where(x => x.TaiKhoanID == accountId.Value)
                .Select(x => x.TaiLieuNoiBoID).ToListAsync()).ToHashSet(),
            AnsweredSurveyIds = (await context.PhieuKhaoSats
                .Where(x => x.TaiKhoanID == accountId.Value)
                .Select(x => x.KhaoSatNoiBoID).ToListAsync()).ToHashSet(),
            ChatQuestion = q
        };

        if (!string.IsNullOrWhiteSpace(q))
        {
            var words = q.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => x.Length > 2).Take(8).ToArray();
            model.ChatResults = documents
                .Select(x => new ChatbotResult(x.TieuDe, x.TomTat ?? "Tài liệu nội bộ", "Document", x.ID))
                .Concat(announcements.Select(x => new ChatbotResult(x.TieuDe, x.NoiDung, "Announcement", x.ID)))
                .Select(x => new
                {
                    Item = x,
                    Score = words.Count(w => (x.Title + " " + x.Summary)
                        .Contains(w, StringComparison.OrdinalIgnoreCase))
                })
                .Where(x => x.Score > 0).OrderByDescending(x => x.Score).Take(5)
                .Select(x => x.Item).ToList();
        }

        return View(model);
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPost]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> CreateDocument(
        string title, string category, string? summary, string scope = "All",
        int? departmentId = null, int? positionId = null, string? role = null,
        bool required = false, List<IFormFile>? attachments = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title)) return Error("Tiêu đề tài liệu là bắt buộc.");
        var audienceError = await ValidateAudienceAsync(scope, departmentId, positionId, role);
        if (audienceError is not null) return Error(audienceError);

        var item = new TaiLieuNoiBo
        {
            TieuDe = title.Trim(),
            DanhMuc = category.Trim(),
            TomTat = summary?.Trim(),
            PhamVi = scope,
            PhongBanID = scope == "Department" ? departmentId : null,
            ChucVuID = scope == "Position" ? positionId : null,
            VaiTro = scope == "Role" ? role : null,
            BatBuocXacNhan = required,
            NguoiTao = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? ""
        };

        var storedFiles = new List<StoredFile>();
        try
        {
            foreach (var attachment in attachments ?? [])
                storedFiles.Add(await files.SaveAsync(attachment, "internal-documents", cancellationToken));

            if (storedFiles.Count > 0)
            {
                var first = storedFiles[0];
                item.DuongDan = first.RelativePath;
                item.TenFileGoc = first.OriginalName;
                item.ContentType = first.ContentType;
                item.KichThuoc = first.Length;
                item.TepDinhKems = storedFiles.Select(stored => new TepTaiLieuNoiBo
                {
                    TenFileGoc = stored.OriginalName,
                    DuongDan = stored.RelativePath,
                    ContentType = stored.ContentType,
                    KichThuoc = stored.Length
                }).ToList();
            }

            context.Add(item);
            audit.Add("Create", "TaiLieuNoiBo", title);
            await context.SaveChangesAsync(cancellationToken);
            return Success($"Đã phát hành tài liệu với {storedFiles.Count} tệp đính kèm.");
        }
        catch (InvalidOperationException ex)
        {
            foreach (var stored in storedFiles) files.Delete(stored.RelativePath);
            return Error(ex.Message);
        }
        catch
        {
            foreach (var stored in storedFiles) files.Delete(stored.RelativePath);
            throw;
        }
    }

    [HttpGet]
    public async Task<IActionResult> DownloadDocument(int id)
    {
        var accountId = AccountId();
        if (!accountId.HasValue) return Challenge();
        var item = await VisibleDocuments(CurrentRoles(), await DepartmentIdAsync(accountId.Value), await PositionIdAsync(accountId.Value))
            .FirstOrDefaultAsync(x => x.ID == id);
        if (item?.DuongDan is null) return NotFound();

        try
        {
            return File(files.OpenRead(item.DuongDan), item.ContentType ?? "application/octet-stream",
                item.TenFileGoc ?? "tai-lieu");
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet]
    public async Task<IActionResult> DownloadDocumentAttachment(int id)
    {
        var accountId = AccountId();
        if (!accountId.HasValue) return Challenge();
        var attachment = await context.TepTaiLieuNoiBos.AsNoTracking().FirstOrDefaultAsync(x => x.ID == id);
        if (attachment is null) return NotFound();
        var canAccess = await VisibleDocuments(CurrentRoles(), await DepartmentIdAsync(accountId.Value), await PositionIdAsync(accountId.Value))
            .AnyAsync(x => x.ID == attachment.TaiLieuNoiBoID);
        if (!canAccess) return NotFound();
        try
        {
            return File(files.OpenRead(attachment.DuongDan), attachment.ContentType, attachment.TenFileGoc);
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmDocument(int id)
    {
        var accountId = AccountId();
        if (!accountId.HasValue) return Challenge();
        var canAccess = await VisibleDocuments(CurrentRoles(), await DepartmentIdAsync(accountId.Value), await PositionIdAsync(accountId.Value))
            .AnyAsync(x => x.ID == id);
        if (!canAccess) return NotFound();

        if (!await context.XacNhanTaiLieus.AnyAsync(x =>
                x.TaiLieuNoiBoID == id && x.TaiKhoanID == accountId.Value))
        {
            context.Add(new XacNhanTaiLieu { TaiLieuNoiBoID = id, TaiKhoanID = accountId.Value });
            audit.Add("Confirm", "TaiLieuNoiBo", id.ToString());
            await context.SaveChangesAsync();
        }

        return Success("Đã xác nhận đọc tài liệu.");
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPost]
    public async Task<IActionResult> CreateAnnouncement(
        string title, string content, string scope = "All", int? departmentId = null,
        int? positionId = null, string? role = null, bool pinned = false, DateTime? end = null)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
            return Error("Thông báo phải có tiêu đề và nội dung.");
        var audienceError = await ValidateAudienceAsync(scope, departmentId, positionId, role);
        if (audienceError is not null) return Error(audienceError);

        var item = new ThongBaoNoiBo
        {
            TieuDe = title.Trim(),
            NoiDung = content.Trim(),
            PhamVi = scope,
            PhongBanID = scope == "Department" ? departmentId : null,
            ChucVuID = scope == "Position" ? positionId : null,
            VaiTro = scope == "Role" ? role : null,
            Ghim = pinned,
            KetThuc = end,
            NguoiTao = User.FindFirstValue(ClaimTypes.Email) ?? ""
        };
        context.Add(item);
        audit.Add("Create", "ThongBaoNoiBo", title);
        await context.SaveChangesAsync();
        return Success("Đã đăng thông báo.");
    }

    [HttpPost]
    public async Task<IActionResult> Comment(int id, string content)
    {
        var accountId = AccountId();
        if (!accountId.HasValue) return Challenge();
        if (string.IsNullOrWhiteSpace(content) || content.Length > 1000)
            return Error("Bình luận không hợp lệ.");

        var canAccess = await VisibleAnnouncements(CurrentRoles(), await DepartmentIdAsync(accountId.Value), await PositionIdAsync(accountId.Value), DateTime.UtcNow)
            .AnyAsync(x => x.ID == id);
        if (!canAccess) return NotFound();

        context.Add(new BinhLuanThongBao
        {
            ThongBaoNoiBoID = id,
            TaiKhoanID = accountId.Value,
            NoiDung = content.Trim()
        });
        await context.SaveChangesAsync();
        return Success("Đã gửi bình luận.");
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPost]
    public async Task<IActionResult> CreateSurvey(
        string title, string question, string options, DateTime? end = null)
    {
        var choices = options.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct().Take(10).ToList();
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(question) || choices.Count < 2)
            return Error("Khảo sát cần câu hỏi và ít nhất hai lựa chọn.");

        var survey = new KhaoSatNoiBo
        {
            TieuDe = title.Trim(),
            CauHoi = question.Trim(),
            KetThuc = end,
            NguoiTao = User.FindFirstValue(ClaimTypes.Email) ?? "",
            LuaChons = choices.Select(x => new LuaChonKhaoSat { NoiDung = x }).ToList()
        };
        context.Add(survey);
        audit.Add("Create", "KhaoSatNoiBo", title);
        await context.SaveChangesAsync();
        return Success("Đã tạo khảo sát.");
    }

    [HttpPost]
    public async Task<IActionResult> Vote(int surveyId, int optionId)
    {
        var accountId = AccountId();
        if (!accountId.HasValue) return Challenge();
        if (await context.PhieuKhaoSats.AnyAsync(x =>
                x.KhaoSatNoiBoID == surveyId && x.TaiKhoanID == accountId.Value))
            return Error("Bạn đã trả lời khảo sát này.");

        var now = DateTime.UtcNow;
        var validOption = await context.LuaChonKhaoSats.AnyAsync(x => x.ID == optionId &&
            x.KhaoSatNoiBoID == surveyId && x.KhaoSat!.HoatDong && x.KhaoSat.BatDau <= now &&
            (x.KhaoSat.KetThuc == null || x.KhaoSat.KetThuc >= now));
        if (!validOption) return BadRequest();

        context.Add(new PhieuKhaoSat
        {
            KhaoSatNoiBoID = surveyId,
            LuaChonKhaoSatID = optionId,
            TaiKhoanID = accountId.Value
        });
        await context.SaveChangesAsync();
        return Success("Đã ghi nhận câu trả lời.");
    }

    private IQueryable<TaiLieuNoiBo> VisibleDocuments(string[] roles, int? departmentId, int? positionId) =>
        context.TaiLieuNoiBos.AsNoTracking().Where(x => x.DaPhatHanh &&
            (x.PhamVi == "All" ||
             (x.PhamVi == "Role" && x.VaiTro != null && roles.Contains(x.VaiTro)) ||
             (x.PhamVi == "Department" && departmentId.HasValue && x.PhongBanID == departmentId) ||
             (x.PhamVi == "Position" && positionId.HasValue && x.ChucVuID == positionId)));

    private IQueryable<ThongBaoNoiBo> VisibleAnnouncements(string[] roles, int? departmentId, int? positionId, DateTime now) =>
        context.ThongBaoNoiBos.AsNoTracking().Where(x => x.BatDau <= now &&
            (x.KetThuc == null || x.KetThuc >= now) &&
            (x.PhamVi == "All" ||
             (x.PhamVi == "Role" && x.VaiTro != null && roles.Contains(x.VaiTro)) ||
             (x.PhamVi == "Department" && departmentId.HasValue && x.PhongBanID == departmentId) ||
             (x.PhamVi == "Position" && positionId.HasValue && x.ChucVuID == positionId)));

    private async Task<string?> ValidateAudienceAsync(string scope, int? departmentId, int? positionId, string? role)
    {
        if (scope is not ("All" or "Role" or "Department" or "Position"))
            return "Phạm vi phát hành không hợp lệ.";
        if (scope == "Department" && (!departmentId.HasValue ||
            !await context.PhongBans.AnyAsync(x => x.ID == departmentId.Value)))
            return "Vui lòng chọn phòng ban nhận nội dung.";
        if (scope == "Position" && (!positionId.HasValue ||
            !await context.ChucVus.AnyAsync(x => x.ID == positionId.Value)))
            return "Vui lòng chọn chức vụ nhận nội dung.";
        if (scope == "Role" && (string.IsNullOrWhiteSpace(role) || !SupportedRoles.Contains(role)))
            return "Vai trò nhận nội dung không hợp lệ.";
        return null;
    }

    private string[] CurrentRoles() => User.FindAll(ClaimTypes.Role).Select(x => x.Value).Distinct().ToArray();
    private int? AccountId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private async Task<int?> DepartmentIdAsync(int accountId) => await context.TaiKhoans
        .Where(x => x.Id == accountId).Select(x => x.NhanVien!.PhongBanID).FirstOrDefaultAsync();
    private async Task<int?> PositionIdAsync(int accountId) => await context.TaiKhoans
        .Where(x => x.Id == accountId).Select(x => x.NhanVien!.ChucVuID).FirstOrDefaultAsync();

    private RedirectToActionResult Error(string text)
    {
        TempData["Error"] = text;
        return RedirectToAction(nameof(Index));
    }

    private RedirectToActionResult Success(string text)
    {
        TempData["Success"] = text;
        return RedirectToAction(nameof(Index));
    }
}
