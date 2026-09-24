namespace TecnicoApp.Application.Common.Interfaces;

/// <summary>
/// Per-install settings read from configuration (section "App"). Every value has a sensible
/// default so a fresh install works, but none of them should be hardcoded at the call site.
/// </summary>
public interface IAppSettings
{
    /// <summary>Public URL of the frontend — used to build links in emails (pay, portal, reset).</summary>
    string BaseUrl { get; }

    /// <summary>Product name shown on internal/system emails and PDF footers (App:ProductName).</summary>
    string ProductName { get; }

    /// <summary>Days between issue date and due date of a new invoice (App:InvoicePaymentTermDays).</summary>
    int InvoicePaymentTermDays { get; }

    /// <summary>
    /// When false (default), public self-registration is only allowed while no account exists
    /// yet — the first account becomes the Owner and everyone else joins by invite.
    /// </summary>
    bool AllowOpenRegistration { get; }
}
