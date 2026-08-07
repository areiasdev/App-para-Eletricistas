namespace TecnicoApp.Application.Common.Extensions;

public static class DocumentNumberExtensions
{
    /// <summary>
    /// Formats a sequential per-tenant, per-year document number, e.g. "ORC-2026-0004" for
    /// <c>FormatDocumentNumber("ORC", 2026, 3)</c> (the 4th quote/invoice issued that year —
    /// <paramref name="countThisYear"/> is the count of documents already issued).
    /// Shared by quote ("ORC") and invoice ("FT") numbering so the two can't drift apart
    /// (e.g. one changing zero-padding width without the other).
    /// </summary>
    public static string FormatDocumentNumber(string prefix, int year, int countThisYear) =>
        $"{prefix}-{year}-{(countThisYear + 1):D4}";
}
