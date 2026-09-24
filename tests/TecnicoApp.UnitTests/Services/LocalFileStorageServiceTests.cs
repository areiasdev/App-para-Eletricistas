using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using NSubstitute;
using TecnicoApp.Infrastructure.Services;
using Xunit;

namespace TecnicoApp.UnitTests.Services;

// LocalFileStorageService.SaveLogoAsync/ReadUploadBytesAsync operate on real disk paths — this
// is what actually feeds the "logo works in PDFs but 404s on the site" bug: the write side
// (tested here) is fine on its own; the read-over-HTTP path is a hosting/StaticFiles concern
// (fixed in Program.cs, not unit-testable at this layer).
public class LocalFileStorageServiceTests : IDisposable
{
    private readonly string _tempRoot;

    public LocalFileStorageServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "tecnicoapp-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_tempRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }

    private LocalFileStorageService CreateService()
    {
        var env = Substitute.For<IWebHostEnvironment>();
        env.WebRootPath.Returns(_tempRoot);
        env.ContentRootPath.Returns(_tempRoot);
        return new LocalFileStorageService(env);
    }

    [Fact]
    public async Task SaveLogoAsync_creates_the_uploads_folder_when_missing()
    {
        // Regression: wwwroot itself is gitignored and doesn't exist on a fresh clone/install
        // until something writes to it — the save path must not assume it's already there.
        var service = CreateService();
        var userId = Guid.NewGuid();

        var url = await service.SaveLogoAsync(userId, [1, 2, 3], ".png", CancellationToken.None);

        url.Should().Be($"/uploads/logos/{userId}.png");
        File.Exists(Path.Combine(_tempRoot, "uploads", "logos", $"{userId}.png")).Should().BeTrue();
    }

    [Fact]
    public async Task SaveLogoAsync_reuploading_in_a_different_format_removes_the_old_file()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();

        await service.SaveLogoAsync(userId, [1, 2, 3], ".png", CancellationToken.None);
        await service.SaveLogoAsync(userId, [4, 5, 6], ".jpg", CancellationToken.None);

        var folder = Path.Combine(_tempRoot, "uploads", "logos");
        File.Exists(Path.Combine(folder, $"{userId}.png")).Should().BeFalse();
        File.Exists(Path.Combine(folder, $"{userId}.jpg")).Should().BeTrue();
    }

    [Fact]
    public async Task ReadUploadBytesAsync_returns_the_saved_bytes()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();
        byte[] content = [10, 20, 30];

        var url = await service.SaveLogoAsync(userId, content, ".png", CancellationToken.None);
        var read = await service.ReadUploadBytesAsync(url, CancellationToken.None);

        read.Should().BeEquivalentTo(content);
    }

    [Fact]
    public async Task ReadUploadBytesAsync_returns_null_for_a_missing_file()
    {
        var service = CreateService();
        var read = await service.ReadUploadBytesAsync("/uploads/logos/does-not-exist.png", CancellationToken.None);
        read.Should().BeNull();
    }

    [Fact]
    public async Task ReadUploadBytesAsync_returns_null_for_null_or_empty_input()
    {
        var service = CreateService();
        (await service.ReadUploadBytesAsync(null, CancellationToken.None)).Should().BeNull();
        (await service.ReadUploadBytesAsync("", CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task ReadUploadBytesAsync_blocks_path_traversal_outside_wwwroot()
    {
        // Security-relevant: a logoUrl containing ".." must never resolve outside webRoot,
        // even though logoUrl is normally server-generated and not directly user-supplied.
        var service = CreateService();

        var secretFile = Path.Combine(_tempRoot, "..", $"secret-{Guid.NewGuid()}.txt");
        try
        {
            await File.WriteAllTextAsync(secretFile, "top secret");

            var read = await service.ReadUploadBytesAsync($"/../{Path.GetFileName(secretFile)}", CancellationToken.None);

            read.Should().BeNull();
        }
        finally
        {
            if (File.Exists(secretFile)) File.Delete(secretFile);
        }
    }
}
