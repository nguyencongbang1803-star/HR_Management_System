namespace QLNS.Services;

public sealed class FileStorageService
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
    private const long MaxBytes = 10 * 1024 * 1024;
    private readonly string _root;

    public FileStorageService(IWebHostEnvironment environment)
    {
        _root = Path.Combine(environment.ContentRootPath, "App_Data", "uploads");
    }

    public async Task<StoredFile> SaveAsync(IFormFile file, string category, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension)) throw new InvalidOperationException("Chỉ chấp nhận PDF, Word, JPG và PNG.");
        var isImage = extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                      extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                      extension.Equals(".png", StringComparison.OrdinalIgnoreCase);
        if (file.Length <= 0 || (!isImage && file.Length > MaxBytes))
            throw new InvalidOperationException(isImage
                ? "Tệp ảnh không được để trống."
                : "Tệp PDF hoặc Word phải có dung lượng từ 1 byte đến 10 MB.");
        var safeCategory = new string(category.Where(char.IsLetterOrDigit).ToArray());
        var directory = Path.Combine(_root, safeCategory);
        Directory.CreateDirectory(directory);
        var storedName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var relativePath = Path.Combine(safeCategory, storedName);
        await using var stream = File.Create(Path.Combine(_root, relativePath));
        await file.CopyToAsync(stream, cancellationToken);
        return new StoredFile(relativePath, Path.GetFileName(file.FileName), file.ContentType, file.Length);
    }

    public async Task<StoredFile> SaveAvatarAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file.Length <= 0)
            throw new InvalidOperationException("Ảnh đại diện không được để trống.");

        var header = new byte[8];
        await using (var input = file.OpenReadStream())
        {
            var read = await input.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
            if (read < 3) throw new InvalidOperationException("Tệp ảnh không hợp lệ.");
        }

        var isJpeg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        var isPng = header.SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        if (!isJpeg && !isPng)
            throw new InvalidOperationException("Chỉ chấp nhận ảnh JPG hoặc PNG hợp lệ.");

        const string category = "avatars";
        var directory = Path.Combine(_root, category);
        Directory.CreateDirectory(directory);
        var extension = isPng ? ".png" : ".jpg";
        var contentType = isPng ? "image/png" : "image/jpeg";
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var relativePath = Path.Combine(category, storedName);
        await using var stream = File.Create(Path.Combine(_root, relativePath));
        await file.CopyToAsync(stream, cancellationToken);
        return new StoredFile(relativePath, Path.GetFileName(file.FileName), contentType, file.Length);
    }

    public async Task<StoredFile> SaveEvidenceImageAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file.Length <= 0)
            throw new InvalidOperationException("Ảnh minh chứng không được để trống.");
        var header = new byte[8];
        await using (var input = file.OpenReadStream())
        {
            var read = await input.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
            if (read < 3) throw new InvalidOperationException("Tệp ảnh không hợp lệ.");
        }
        var isJpeg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        var isPng = header.SequenceEqual(new byte[] { 0x89,0x50,0x4E,0x47,0x0D,0x0A,0x1A,0x0A });
        if (!isJpeg && !isPng) throw new InvalidOperationException("Minh chứng chỉ chấp nhận ảnh JPG hoặc PNG hợp lệ.");
        const string category = "attendanceproofs";
        var directory = Path.Combine(_root, category);Directory.CreateDirectory(directory);
        var extension = isPng ? ".png" : ".jpg";
        var contentType = isPng ? "image/png" : "image/jpeg";
        var relativePath = Path.Combine(category, $"{Guid.NewGuid():N}{extension}");
        await using var stream = File.Create(Path.Combine(_root, relativePath));
        await file.CopyToAsync(stream, cancellationToken);
        return new StoredFile(relativePath, Path.GetFileName(file.FileName), contentType, file.Length);
    }

    public FileStream OpenRead(string relativePath)
    {
        var fullPath = Resolve(relativePath);
        if (!File.Exists(fullPath)) throw new FileNotFoundException();
        return File.OpenRead(fullPath);
    }

    public static string? InlinePreviewContentType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".pdf" => "application/pdf",
            _ => null
        };

    public void Delete(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;
        var fullPath = Resolve(relativePath);
        if (File.Exists(fullPath)) File.Delete(fullPath);
    }

    private string Resolve(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_root, relativePath));
        var root = Path.GetFullPath(_root) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Đường dẫn tệp không hợp lệ.");
        return fullPath;
    }
}

public record StoredFile(string RelativePath, string OriginalName, string ContentType, long Length);
