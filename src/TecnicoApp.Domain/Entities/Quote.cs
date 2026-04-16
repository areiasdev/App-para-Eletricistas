using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Domain.Entities;

public class Quote : BaseEntity
{
    public required string Number { get; set; }   // ORC-2025-0042
    public QuoteStatus Status { get; set; } = QuoteStatus.Draft;
    public decimal? Discount { get; set; }
    public string? Notes { get; set; }
    public DateTime? ValidUntil { get; set; }
    public DateTime? SignedAt { get; set; }
    public string? SignatureUrl { get; set; }
    public string? PdfUrl { get; set; }

    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public ICollection<QuoteLine> Lines { get; set; } = [];

    // Propriedades calculadas — não persistidas
    // Round each line individually before summing to avoid cent-level drift across many lines
    public decimal SubTotal => Lines.Sum(l => Math.Round(l.Quantity * l.UnitPrice, 2, MidpointRounding.AwayFromZero));
    public decimal VatTotal => Lines.Sum(l => Math.Round(l.Quantity * l.UnitPrice * (l.VatRate / 100), 2, MidpointRounding.AwayFromZero));
    public decimal Total => Math.Round(SubTotal + VatTotal - (Discount ?? 0), 2, MidpointRounding.AwayFromZero);
}
