using System.Globalization;

namespace TecnicoApp.Application.Common.Formatting;

/// <summary>
/// Portuguese (pt-PT) formatting for anything a client reads — emails, WhatsApp messages, PDFs.
/// One shared culture instance instead of <c>new CultureInfo("pt-PT")</c> at every call site.
/// </summary>
public static class PtFormat
{
    public static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("pt-PT");

    /// <summary>1234.5 → "1234,50 €"</summary>
    public static string Currency(decimal value) => value.ToString("C", Culture);

    /// <summary>Unit price with up to 4 decimals (cable per metre): 0.4575 → "0,4575 €", 35 → "35,00 €".</summary>
    public static string UnitPrice(decimal value) => value.ToString("#,##0.00##", Culture) + " €";

    /// <summary>VAT rate: 23 → "23%", 6.5 → "6,5%".</summary>
    public static string Percent(decimal value) => value.ToString("0.##", Culture) + "%";

    /// <summary>"12/04/2026"</summary>
    public static string ShortDate(DateTime value) => value.ToString("dd/MM/yyyy", Culture);

    /// <summary>"12 de abril de 2026"</summary>
    public static string LongDate(DateTime value) => value.ToString("d 'de' MMMM 'de' yyyy", Culture);

    /// <summary>"12/04/2026 às 09:30"</summary>
    public static string DateTime(DateTime value) => value.ToString("dd/MM/yyyy 'às' HH:mm", Culture);
}
