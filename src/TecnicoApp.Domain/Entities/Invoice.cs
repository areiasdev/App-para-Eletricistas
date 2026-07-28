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

    public ICollection<InvoiceLine> Lines { get; set; } = [];

    // Propriedades calculadas — não persistidas
    // Round each line individually before summing to avoid cent-level drift across many lines
    public decimal SubTotal => Lines.Sum(l => Math.Round(l.Quantity * l.UnitPrice, 2, MidpointRounding.AwayFromZero));
    public decimal VatTotal => Lines.Sum(l => Math.Round(l.Quantity * l.UnitPrice * (l.VatRate / 100), 2, MidpointRounding.AwayFromZero));
    public decimal Total => Math.Round(SubTotal + VatTotal - (Discount ?? 0), 2, MidpointRounding.AwayFromZero);
}
