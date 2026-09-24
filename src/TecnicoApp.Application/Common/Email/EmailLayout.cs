using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using TecnicoApp.Domain.Entities;

namespace TecnicoApp.Application.Common.Email;

/// <summary>
/// Who an email appears to come from. Client-facing emails carry the company's own name and
/// brand colour (from Perfil); internal/system emails use the product name.
/// </summary>
public sealed record EmailBranding(string SenderName, string? BrandColorHex = null)
{
    /// <summary>Branding of the team owner — the company identity, never an individual technician.</summary>
    public static EmailBranding ForCompany(User owner) =>
        new(string.IsNullOrWhiteSpace(owner.CompanyName) ? owner.FullName : owner.CompanyName!, owner.BrandColor);
}

/// <summary>
/// Single HTML shell for every transactional email (quotes, invoices, invites, portal access,
/// password reset, maintenance alerts), so layout, colours and escaping live in one place
/// instead of being copy-pasted into each handler.
/// </summary>
public static partial class EmailLayout
{
    public const string DefaultAccentHex = "#f59e0b";

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex HexColor();

    public static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    /// <summary>Strips CR/LF so user-supplied names can't inject extra headers into a subject line.</summary>
    public static string SubjectSafe(string value) => value.Replace('\r', ' ').Replace('\n', ' ');

    /// <param name="bodyHtml">Already-escaped HTML for the main content block.</param>
    /// <param name="footerText">Plain text; escaped here.</param>
    public static string Render(EmailBranding branding, string bodyHtml, string footerText)
    {
        var accent = Accent(branding);
        return $"""
            <!DOCTYPE html>
            <html lang="pt">
            <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
            <body style="margin:0;padding:0;background:#f7f7f4;font-family:'Helvetica Neue',Arial,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background:#f7f7f4;padding:40px 0;">
                <tr><td align="center">
                  <table width="560" cellpadding="0" cellspacing="0" style="max-width:560px;background:#ffffff;border-radius:12px;overflow:hidden;border:1px solid #e5e7eb;">
                    <tr>
                      <td style="background:#17171a;border-bottom:4px solid {accent};padding:24px 32px;text-align:center;">
                        <span style="color:#ffffff;font-size:20px;font-weight:700;">{Encode(branding.SenderName)}</span>
                      </td>
                    </tr>
                    <tr><td style="padding:32px;color:#374151;font-size:15px;line-height:1.6;">
                      {bodyHtml}
                    </td></tr>
                    <tr>
                      <td style="background:#f9fafb;padding:16px 32px;text-align:center;border-top:1px solid #e5e7eb;">
                        <p style="margin:0;color:#9ca3af;font-size:11px;">{Encode(footerText)}</p>
                      </td>
                    </tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;
    }

    /// <summary>Call-to-action button in the sender's brand colour, with readable text on top.</summary>
    public static string Button(EmailBranding branding, string url, string label)
    {
        var accent = Accent(branding);
        var text = IsLight(accent) ? "#1c1917" : "#ffffff";
        return $"""
            <div style="text-align:center;margin:8px 0 24px;">
              <a href="{Encode(url)}" style="display:inline-block;background:{accent};color:{text};font-weight:700;font-size:15px;padding:12px 28px;border-radius:8px;text-decoration:none">{Encode(label)}</a>
            </div>
            """;
    }

    /// <summary>Two-column key/value summary box (e.g. Total / Vencimento).</summary>
    public static string SummaryTable(params (string Label, string Value)[] rows)
    {
        var body = string.Concat(rows.Select(r => $"""
            <tr>
              <td style="color:#6b7280;font-size:13px;padding:4px 0;">{Encode(r.Label)}</td>
              <td align="right" style="color:#1a1a1a;font-size:15px;font-weight:600;padding:4px 0;">{Encode(r.Value)}</td>
            </tr>
            """));
        return $"""
            <table width="100%" cellpadding="0" cellspacing="0" style="background:#f9fafb;border-radius:8px;border:1px solid #e5e7eb;margin-bottom:24px;">
              <tr><td style="padding:16px 20px;"><table width="100%">{body}</table></td></tr>
            </table>
            """;
    }

    private static string Accent(EmailBranding branding) =>
        branding.BrandColorHex is { } hex && HexColor().IsMatch(hex) ? hex : DefaultAccentHex;

    // Relative luminance (WCAG) — light brand colours get dark button text and vice versa.
    private static bool IsLight(string hex)
    {
        static double Channel(string h, int i)
        {
            var c = int.Parse(h.AsSpan(i, 2), NumberStyles.HexNumber) / 255.0;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }
        var l = 0.2126 * Channel(hex, 1) + 0.7152 * Channel(hex, 3) + 0.0722 * Channel(hex, 5);
        return l > 0.4;
    }
}
