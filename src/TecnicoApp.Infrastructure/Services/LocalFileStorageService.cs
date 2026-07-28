using Microsoft.AspNetCore.Hosting;
using TecnicoApp.Application.Common.Interfaces;

namespace TecnicoApp.Infrastructure.Services;

// Self-hosted, single-tenant-per-install deployment — local disk under wwwroot is the
// right call here. No need for S3/Blob complexity when there's exactly one company's
// data per instance and the API/frontend already sit on the same host.
public class LocalFileStorageService(IWebHostEnvironment env) : IFileStorageService
{
    private const string RelativeFolder = "uploads/logos";

    public async Task<string> SaveLogoAsync(Guid userId, byte[] content, string extension, CancellationToken cancellationToken = default)
    {
        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var folder = Path.Combine(webRoot, RelativeFolder);
        Directory.CreateDirectory(folder);

        // Remove any previous logo for this user first — re-uploading in a different
        // format (e.g. png -> jpg) would otherwise leave the old file orphaned on disk.
        foreach (var existing in Directory.EnumerateFiles(folder, $"{userId}.*"))
            File.Delete(existing);

        var fileName = $"{userId}{extension}";
        var path = Path.Combine(folder, fileName);
        await File.WriteAllBytesAsync(path, content, cancellationToken);

        return $"/{RelativeFolder}/{fileName}";
    }

    public async Task<byte[]?> ReadLogoBytesAsync(string? logoUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(logoUrl))
            return null;

        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var relative = logoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var path = Path.Combine(webRoot, relative);

        // Guard against path traversal — the resolved path must stay inside wwwroot.
        var fullWebRoot = Path.GetFullPath(webRoot);
        var fullPath = Path.GetFullPath(path);
        if (!fullPath.StartsWith(fullWebRoot, StringComparison.OrdinalIgnoreCase))
            return null;

        if (!File.Exists(fullPath))
            return null;

        return await File.ReadAllBytesAsync(fullPath, cancellationToken);
    }
}
