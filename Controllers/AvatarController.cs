using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.Data;
using QLNS.Services;

namespace QLNS.Controllers;

[Authorize]
[Route("avatar")]
public sealed class AvatarController(AppDbContext context, FileStorageService files) : Controller
{
    [HttpGet("employee/{id:int}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Employee(int id)
    {
        var path = await context.NhanViens.AsNoTracking()
            .Where(x => x.ID == id)
            .Select(x => x.AnhDaiDien)
            .SingleOrDefaultAsync();
        if (string.IsNullOrWhiteSpace(path)) return NotFound();

        try
        {
            var contentType = Path.GetExtension(path).Equals(".png", StringComparison.OrdinalIgnoreCase)
                ? "image/png"
                : "image/jpeg";
            return File(files.OpenRead(path), contentType);
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
    }
}
