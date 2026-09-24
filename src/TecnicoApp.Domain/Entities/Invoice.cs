using TecnicoApp.Domain.Common;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Domain.Entities;

public class Invoice : BaseEntity
{
    public required string Number { get; set; }   // FT-2025-0042
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Issued;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }
    public decimal? Discount { get; set; }

    // Nullable — this phase only creates invoices from an Accepted quote, so it is always
    // set today, but a future phase may allow ad-hoc invoices with no source quote.
    public Guid? QuoteId { get; set; }
    public Quote? Quote { get; set; }

    // Copied from the quote at creation time (not derived through Quote) so the invoice
    // stays resolvable even if the source quote is ever soft-deleted.
    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    // Online payment (Phase 4) — magic-link token for the public "Pagar agora" page.
    // Only the SHA256 hash is ever persisted; the raw token lives only in the URL/email.
    public string? PayTokenHash { get; set; }
    public DateTime? PayTokenExpiresAt { get; set; }
    // Set when a Stripe Checkout Session is created for this invoice; the webhook looks the
    // invoice back up by this id when Stripe reports the payment as completed.
    public string? StripeCheckoutSessionId { get; set; }

    public ICollection<InvoiceLine> Lines { get; set; } = [];

    // Propriedades calculadas — não persistidas (see DocumentMath for the rounding rules)
    public decimal SubTotal => DocumentMath.SubTotal(Lines);
    public decimal VatTotal => DocumentMath.VatTotal(Lines);
    public decimal Total => DocumentMath.Total(Lines, Discount);
}
