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
public class HopDongController : Controller
{
    private readonly AppDbContext _context;
    private readonly AuditService _audit;
    private readonly FileStorageService _files;
    public HopDongController(AppDbContext context, AuditService audit, FileStorageService files) { _context = context; _audit = audit; _files = files; }

    public async Task<IActionResult> Index(string? search, bool activeOnly = true)
    {
        var query = _context.HopDongs.AsNoTracking().Include(h => h.NhanVien).AsQueryable();
        if (activeOnly) query = query.Where(h => h.Trangthai);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(h => h.SoHopDong.Contains(search) || h.NhanVien!.Ten.Contains(search));
        ViewBag.Search = search; ViewBag.ActiveOnly = activeOnly;
        ViewBag.RenewedContractIds = (await _context.HopDongs.AsNoTracking()
            .Where(x => x.HopDongTruocID.HasValue)
            .Select(x => x.HopDongTruocID!.Value)
            .ToListAsync()).ToHashSet();
        await LoadContractTypesAsync();
        return View(await query.OrderByDescending(h => h.Ngaybatdau).ToListAsync());
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpGet]
    public async Task<IActionResult> Create(int? renewFromId, int? nhanVienId)
    {
        if (!renewFromId.HasValue)
        {
            if (nhanVienId.HasValue && !await _context.NhanViens.AsNoTracking()
                    .AnyAsync(x => x.ID == nhanVienId.Value && x.Trangthai))
                return NotFound();
            await LoadEmployeesAsync();
            return View(new ContractFormViewModel { NhanVienID = nhanVienId ?? 0 });
        }

        var previous = await _context.HopDongs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ID == renewFromId.Value);
        if (previous is null) return NotFound();
        if (!previous.Ngayketthuc.HasValue)
        {
            TempData["Error"] = "Hợp đồng không xác định thời hạn không cần gia hạn.";
            return RedirectToAction(nameof(Detail), new { id = previous.ID });
        }
        if (await _context.HopDongs.AsNoTracking().AnyAsync(x => x.HopDongTruocID == previous.ID))
        {
            TempData["Error"] = "Hợp đồng này đã được gia hạn.";
            return RedirectToAction(nameof(Detail), new { id = previous.ID });
        }

        await LoadEmployeesAsync(previous.ID);
        return View(new ContractFormViewModel
        {
            NhanVienID = previous.NhanVienID,
            Loaihopdong = previous.Loaihopdong,
            Luongcoban = previous.Luongcoban,
            Ngayky = DateTime.Today,
            Ngaybatdau = previous.Ngayketthuc?.Date.AddDays(1) ?? DateTime.Today,
            HopDongTruocID = previous.ID,
            Trangthai = true
        });
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPost]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> Create(ContractFormViewModel model)
    {
        await ValidateContractAsync(model);
        if (!ModelState.IsValid) { await LoadEmployeesAsync(model.HopDongTruocID); return View(model); }
        var item = new HopDong(); Map(model, item); item.CreatedAt = DateTime.UtcNow;
        var storedFiles = new List<StoredFile>();
        try
        {
            foreach (var upload in model.FilesHopDong)
                storedFiles.Add(await _files.SaveAsync(upload, "contracts"));
        }
        catch (InvalidOperationException ex)
        {
            foreach (var stored in storedFiles) _files.Delete(stored.RelativePath);
            ModelState.AddModelError(nameof(model.FilesHopDong), ex.Message);
            await LoadEmployeesAsync(model.HopDongTruocID);
            return View(model);
        }
        item.TaiLieuHopDongs = storedFiles.Select(ToContractFile).ToList();
        _context.HopDongs.Add(item);
        try { await _context.SaveChangesAsync(); }
        catch { foreach (var stored in storedFiles) _files.Delete(stored.RelativePath); throw; }
        _audit.Add("Create", "HopDong", item.ID.ToString(), item.SoHopDong); await _context.SaveChangesAsync();
        TempData["Success"] = "Đã tạo hợp đồng."; return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _context.HopDongs.FindAsync(id); if (item is null) return NotFound();
        await LoadEmployeesAsync(item.HopDongTruocID);
        await LoadContractFilesAsync(id);
        await SetCanRenewAsync(item);
        return View(ToForm(item));
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPost]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> Edit(int id, ContractFormViewModel model)
    {
        if (id != model.ID) return BadRequest();
        var item = await _context.HopDongs.FindAsync(id); if (item is null) return NotFound();
        await ValidateContractAsync(model, id);
        if (!ModelState.IsValid)
        {
            await LoadEmployeesAsync(model.HopDongTruocID);
            await LoadContractFilesAsync(id);
            await SetCanRenewAsync(item);
            return View(model);
        }
        Map(model, item);
        var storedFiles = new List<StoredFile>();
        try
        {
            foreach (var upload in model.FilesHopDong)
                storedFiles.Add(await _files.SaveAsync(upload, "contracts"));
        }
        catch (InvalidOperationException ex)
        {
            foreach (var stored in storedFiles) _files.Delete(stored.RelativePath);
            ModelState.AddModelError(nameof(model.FilesHopDong), ex.Message);
            await LoadEmployeesAsync(model.HopDongTruocID);
            await LoadContractFilesAsync(id);
            await SetCanRenewAsync(item);
            return View(model);
        }
        foreach (var stored in storedFiles) _context.TaiLieuHopDongs.Add(ToContractFile(stored, id));
        item.UpdatedAt = DateTime.UtcNow; _audit.Add("Update", "HopDong", id.ToString(), item.SoHopDong);
        try { await _context.SaveChangesAsync(); }
        catch { foreach (var stored in storedFiles) _files.Delete(stored.RelativePath); throw; }
        TempData["Success"] = "Đã cập nhật hợp đồng."; return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Detail(int id)
    {
        var item = await _context.HopDongs.AsNoTracking()
            .Include(x => x.NhanVien)
            .Include(x => x.HopDongTruoc)
            .Include(x => x.TaiLieuHopDongs)
            .FirstOrDefaultAsync(x => x.ID == id);
        if (item is null) return NotFound();
        ViewBag.CanRenew = item.Ngayketthuc.HasValue &&
            !await _context.HopDongs.AsNoTracking().AnyAsync(x => x.HopDongTruocID == id);
        return View(item);
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPost]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var item = await _context.HopDongs.FindAsync(id); if (item is null) return NotFound();
        item.Trangthai = !item.Trangthai; item.UpdatedAt = DateTime.UtcNow;
        _audit.Add(item.Trangthai ? "Activate" : "Deactivate", "HopDong", id.ToString());
        await _context.SaveChangesAsync(); TempData["Success"] = "Đã cập nhật trạng thái hợp đồng."; return RedirectToAction(nameof(Index), new { activeOnly = false });
    }

    [HttpGet]
    public async Task<IActionResult> Download(int id)
    {
        var item=await _context.HopDongs.AsNoTracking().FirstOrDefaultAsync(x=>x.ID==id);
        if(item?.DuongDanFile is null)return NotFound();
        try{return File(_files.OpenRead(item.DuongDanFile),item.ContentType??"application/octet-stream",item.TenFileGoc??"hop-dong");}
        catch(FileNotFoundException){return NotFound();}
    }

    [HttpGet]
    public async Task<IActionResult> DownloadAttachment(int id)
    {
        var item = await _context.TaiLieuHopDongs.AsNoTracking().FirstOrDefaultAsync(x => x.ID == id);
        if (item is null) return NotFound();
        try { return File(_files.OpenRead(item.DuongDanFile), item.ContentType ?? "application/octet-stream", item.TenFileGoc); }
        catch (FileNotFoundException) { return NotFound(); }
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPost]
    public async Task<IActionResult> DeleteAttachment(int id)
    {
        var item = await _context.TaiLieuHopDongs.FindAsync(id);
        if (item is null) return NotFound();
        var contractId = item.HopDongID;
        _context.TaiLieuHopDongs.Remove(item);
        _audit.Add("Delete", "TaiLieuHopDong", id.ToString(), item.TenFileGoc);
        await _context.SaveChangesAsync();
        _files.Delete(item.DuongDanFile);
        TempData["Success"] = "Đã xóa file hợp đồng.";
        return RedirectToAction(nameof(Edit), new { id = contractId });
    }

    private async Task ValidateContractAsync(ContractFormViewModel model, int? exceptId = null)
    {
        if (!string.IsNullOrWhiteSpace(model.Loaihopdong))
        {
            var contractType = model.Loaihopdong.Trim();
            var typeExists = await _context.LoaiHopDongs.AnyAsync(x => x.TenLoai == contractType);
            if (!typeExists || !ContractTypes.All.Contains(contractType))
                ModelState.AddModelError(nameof(model.Loaihopdong), "Loại hợp đồng không tồn tại trong danh mục.");
        }
        if (await _context.HopDongs.AnyAsync(h => h.SoHopDong == model.SoHopDong.Trim() && h.ID != exceptId))
            ModelState.AddModelError(nameof(model.SoHopDong), "Số hợp đồng đã tồn tại.");
        if (model.HopDongTruocID.HasValue)
        {
            var previous = await _context.HopDongs.AsNoTracking()
                .FirstOrDefaultAsync(x => x.ID == model.HopDongTruocID.Value);
            if (previous is null)
                ModelState.AddModelError(nameof(model.HopDongTruocID), "Hợp đồng trước không tồn tại.");
            else if (previous.NhanVienID != model.NhanVienID)
                ModelState.AddModelError(nameof(model.HopDongTruocID), "Hợp đồng gia hạn phải thuộc cùng nhân viên.");
            else if (await _context.HopDongs.AnyAsync(x => x.ID != exceptId && x.HopDongTruocID == previous.ID))
                ModelState.AddModelError(nameof(model.HopDongTruocID), "Hợp đồng này đã được gia hạn.");
        }
        if (!model.Trangthai) return;
        var end = model.Ngayketthuc ?? DateTime.MaxValue;
        var overlap = await _context.HopDongs.AnyAsync(h => h.ID != exceptId && h.NhanVienID == model.NhanVienID && h.Trangthai &&
            h.Ngaybatdau <= end && (h.Ngayketthuc == null || h.Ngayketthuc >= model.Ngaybatdau));
        if (overlap) ModelState.AddModelError(string.Empty, "Nhân viên đã có hợp đồng hiệu lực bị chồng lấn thời gian.");
    }

    private async Task LoadEmployeesAsync(int? previousContractId = null)
    {
        ViewBag.NhanViens = await _context.NhanViens.AsNoTracking().Where(n => n.Trangthai).OrderBy(n => n.Ten).ToListAsync();
        ViewBag.HopDongs = await _context.HopDongs.AsNoTracking().OrderByDescending(x => x.Ngaybatdau).ToListAsync();
        ViewBag.PreviousContract = previousContractId.HasValue
            ? await _context.HopDongs.AsNoTracking().FirstOrDefaultAsync(x => x.ID == previousContractId.Value)
            : null;
        await LoadContractTypesAsync();
    }

    private async Task LoadContractTypesAsync() =>
        ViewBag.ContractTypes = await _context.LoaiHopDongs.AsNoTracking().OrderBy(x => x.TenLoai).ToListAsync();

    private async Task SetCanRenewAsync(HopDong item) =>
        ViewBag.CanRenew = item.Ngayketthuc.HasValue &&
            !await _context.HopDongs.AsNoTracking().AnyAsync(x => x.HopDongTruocID == item.ID);

    private async Task LoadContractFilesAsync(int contractId) =>
        ViewBag.ContractFiles = await _context.TaiLieuHopDongs.AsNoTracking()
            .Where(x => x.HopDongID == contractId).OrderBy(x => x.NgayTaiLen).ToListAsync();

    private static TaiLieuHopDong ToContractFile(StoredFile file, int contractId = 0) => new()
    {
        HopDongID = contractId,
        TenFileGoc = file.OriginalName,
        DuongDanFile = file.RelativePath,
        ContentType = file.ContentType,
        KichThuoc = file.Length
    };

    private static void Map(ContractFormViewModel s, HopDong t) { t.NhanVienID=s.NhanVienID; t.SoHopDong=s.SoHopDong.Trim().ToUpperInvariant(); t.Loaihopdong=s.Loaihopdong.Trim(); t.Ngayky=s.Ngayky.Date; t.Ngaybatdau=s.Ngaybatdau.Date; t.Ngayketthuc=s.Loaihopdong==ContractTypes.IndefiniteTerm?null:s.Ngayketthuc?.Date; t.Luongcoban=s.Luongcoban; t.Trangthai=s.Trangthai;t.HopDongTruocID=s.HopDongTruocID; }
    private static ContractFormViewModel ToForm(HopDong h) => new() { ID=h.ID, NhanVienID=h.NhanVienID, SoHopDong=h.SoHopDong, Loaihopdong=h.Loaihopdong, Ngayky=h.Ngayky, Ngaybatdau=h.Ngaybatdau, Ngayketthuc=h.Ngayketthuc, Luongcoban=h.Luongcoban, Trangthai=h.Trangthai,HopDongTruocID=h.HopDongTruocID,TenFileHienTai=h.TenFileGoc };
}
