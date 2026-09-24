namespace TecnicoApp.Application.Common.Interfaces;

public interface IFileStorageService
{
    /// <summary>Saves a company logo to disk, replacing any previous logo for that user
    /// (including one with a different extension). Returns the URL path to serve it from.</summary>
    Task<string> SaveLogoAsync(Guid userId, byte[] content, string extension, CancellationToken cancellationToken = default);

    /// <summary>Saves a site/equipment photo under the company's folder with a random,
    /// unguessable name. Returns the URL path to serve it from ("/uploads/photos/…").</summary>
    Task<string> SavePhotoAsync(Guid ownerId, byte[] content, string extension, CancellationToken cancellationToken = default);

    /// <summary>Reads a previously-uploaded file (logo or photo) back off disk, given the URL
    /// path it is served from. Returns null if the URL is null/empty, external or the file is missing.</summary>
    Task<byte[]?> ReadUploadBytesAsync(string? logoUrl, CancellationToken cancellationToken = default);
}
