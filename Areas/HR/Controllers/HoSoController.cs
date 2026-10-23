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
[Authorize(Roles = "Admin,HR")]
public class HoSoController : Controller
{
    private readonly AppDbContext _context;
    private readonly FileStorageService _files;
    private readonly AuditService _audit;
    public HoSoController(AppDbContext context, FileStorageService files, AuditService audit) =>
        (_context, _files, _audit) = (context, files, audit);

    [HttpPost]
    public async Task<IActionResult> AddQualification(QualificationFormViewModel model)
    {
        if (!ModelState.IsValid) return Back(model.NhanVienID, "Thông tin bằng cấp/chứng chỉ không hợp lệ.");
        var item = new TrinhDoChungChi { NhanVienID=model.NhanVienID, Loai=model.Loai, Ten=model.Ten.Trim(), DonViCap=model.DonViCap?.Trim(), NgayCap=model.NgayCap?.Date, NgayHetHan=model.NgayHetHan?.Date, SoHieu=model.SoHieu?.Trim() };
        _context.TrinhDoChungChis.Add(item); await _context.SaveChangesAsync();
        _audit.Add("Create", "TrinhDoChungChi", item.ID.ToString()); await _context.SaveChangesAsync();
        return Back(model.NhanVienID, null, "Đã thêm bằng cấp/chứng chỉ.");
    }

    [HttpPost]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> UploadDocument(EmployeeDocumentFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || model.Files.Count == 0) return Back(model.NhanVienID, "Tài liệu không hợp lệ.");
        var storedFiles = new List<StoredFile>();
        try
        {
            foreach (var file in model.Files)
                storedFiles.Add(await _files.SaveAsync(file, "employees", cancellationToken));
            var items = storedFiles.Select(stored => new TaiLieuNhanVien
            {
                NhanVienID=model.NhanVienID, TenTaiLieu=model.TenTaiLieu.Trim(),
                TenFileGoc=stored.OriginalName, DuongDan=stored.RelativePath,
                ContentType=stored.ContentType, KichThuoc=stored.Length,
                NguoiTaiLen=User.FindFirstValue(ClaimTypes.Email)
            }).ToList();
            _context.TaiLieuNhanViens.AddRange(items);
            await _context.SaveChangesAsync();
            foreach (var item in items)
                _audit.Add("Upload", "TaiLieuNhanVien", item.ID.ToString(), item.TenFileGoc);
            await _context.SaveChangesAsync();
            return Back(model.NhanVienID, null, $"Đã tải lên {items.Count} tài liệu.");
        }
        catch (InvalidOperationException ex)
        {
            foreach (var stored in storedFiles) _files.Delete(stored.RelativePath);
            return Back(model.NhanVienID, ex.Message);
        }
        catch
        {
            foreach (var stored in storedFiles) _files.Delete(stored.RelativePath);
            throw;
        }
    }

    [HttpGet]
    public async Task<IActionResult> DownloadDocument(int id)
    {
        var item = await _context.TaiLieuNhanViens.AsNoTracking().FirstOrDefaultAsync(x => x.ID == id);
        if (item is null) return NotFound();
        try { return File(_files.OpenRead(item.DuongDan), item.ContentType, item.TenFileGoc); }
        catch (FileNotFoundException) { return NotFound(); }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteDocument(int id)
    {
        var item = await _context.TaiLieuNhanViens.FindAsync(id); if (item is null) return NotFound();
        _files.Delete(item.DuongDan); _context.Remove(item); _audit.Add("Delete", "TaiLieuNhanVien", id.ToString()); await _context.SaveChangesAsync();
        return Back(item.NhanVienID, null, "Đã xóa tài liệu.");
    }

    private RedirectToActionResult Back(int employeeId, string? error = null, string? success = null)
    {
        if (error is not null) TempData["Error"] = error;
        if (success is not null) TempData["Success"] = success;
        return RedirectToAction("Detail", "NhanVien", new { id = employeeId });
    }
}
