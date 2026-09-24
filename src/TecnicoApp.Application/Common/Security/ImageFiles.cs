namespace TecnicoApp.Application.Common.Security;

/// <summary>Accepted image uploads (logos, site photos) and their real file-signature check.</summary>
public static class ImageFiles
{
    public static readonly IReadOnlyDictionary<string, string> ExtensionsByContentType =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/png"] = ".png",
            ["image/jpeg"] = ".jpg",
            ["image/webp"] = ".webp",
        };

    public static bool IsAllowedContentType(string? contentType) =>
        contentType is not null && ExtensionsByContentType.ContainsKey(contentType);

    /// <summary>
    /// The Content-Type header is attacker-controlled (a multipart form field the browser doesn't
    /// verify) — checking it alone would let a mislabeled/malformed file through to the image
    /// decoders used for PDFs. Verify the file's magic bytes match the claimed type.
    /// </summary>
    public static bool MatchesClaimedType(byte[] content, string contentType) => contentType.ToLowerInvariant() switch
    {
        "image/png" => content.Length >= 8 &&
            content[0] == 0x89 && content[1] == 0x50 && content[2] == 0x4E && content[3] == 0x47 &&
            content[4] == 0x0D && content[5] == 0x0A && content[6] == 0x1A && content[7] == 0x0A,
        "image/jpeg" => content.Length >= 3 &&
            content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF,
        "image/webp" => content.Length >= 12 &&
            content[0] == 'R' && content[1] == 'I' && content[2] == 'F' && content[3] == 'F' &&
            content[8] == 'W' && content[9] == 'E' && content[10] == 'B' && content[11] == 'P',
        _ => false,
    };

    /// <summary>Folder (under wwwroot) where uploaded site/equipment photos are stored.</summary>
    public const string PhotoUrlPrefix = "/uploads/photos/";

    /// <summary>
    /// A photo reference on an intervention/equipment: either a photo uploaded to this install
    /// (relative "/uploads/photos/…" path) or an external HTTPS URL.
    /// </summary>
    public static bool IsValidPhotoUrl(string? url) =>
        !string.IsNullOrWhiteSpace(url) && url.Length <= 2048 &&
        ((url.StartsWith(PhotoUrlPrefix, StringComparison.Ordinal) && !url.Contains("..")) ||
         (Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
          (uri.Scheme == Uri.UriSchemeHttps || uri.Host == "localhost")));
}
