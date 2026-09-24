using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace TecnicoApp.Application.Common.Security;

/// <summary>Helpers shared by every magic-link flow (invoice pay link, quote approval, portal).</summary>
public static partial class PublicTokens
{
    /// <summary>Hex SHA256 of a raw link token — the only form a token is ever stored in.</summary>
    public static string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    [GeneratedRegex(@"^data:image/(png|jpeg|webp);base64,[A-Za-z0-9+/]+=*$", RegexOptions.None, 200)]
    private static partial Regex SignatureDataUrl();

    /// <summary>Max size of a signature data URI (~375 KB image) — a canvas signature is a few KB.</summary>
    public const int MaxSignatureLength = 500_000;

    /// <summary>
    /// A hand-drawn signature captured from a canvas: a raster data URI only (never SVG, which
    /// can embed script) and of a sane size.
    /// </summary>
    public static bool IsValidSignature(string? dataUrl)
    {
        if (string.IsNullOrWhiteSpace(dataUrl) || dataUrl.Length > MaxSignatureLength) return false;
        var match = SignatureDataUrl().Match(dataUrl);
        if (!match.Success) return false;

        // The bytes must really be the claimed image type — it's rendered into PDFs later, and a
        // malformed image there would make every PDF of that document fail to generate.
        try
        {
            var bytes = Convert.FromBase64String(dataUrl[(dataUrl.IndexOf(',') + 1)..]);
            return ImageFiles.MatchesClaimedType(bytes, $"image/{match.Groups[1].Value}");
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
