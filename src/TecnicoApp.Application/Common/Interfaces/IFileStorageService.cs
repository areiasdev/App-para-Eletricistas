namespace TecnicoApp.Application.Common.Interfaces;

public interface IFileStorageService
{
    /// <summary>Saves a company logo to disk, replacing any previous logo for that user
    /// (including one with a different extension). Returns the URL path to serve it from.</summary>
    Task<string> SaveLogoAsync(Guid userId, byte[] content, string extension, CancellationToken cancellationToken = default);

    /// <summary>Reads a previously-saved logo's bytes back off disk, given the URL path
    /// stored on User.LogoUrl. Returns null if the URL is null/empty or the file is missing.</summary>
    Task<byte[]?> ReadLogoBytesAsync(string? logoUrl, CancellationToken cancellationToken = default);
}
