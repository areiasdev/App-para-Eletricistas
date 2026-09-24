using TecnicoApp.Domain.Common;
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
    public DateTime? EmailSentAt { get; set; }

    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public ICollection<QuoteLine> Lines { get; set; } = [];

    // Propriedades calculadas — não persistidas (see DocumentMath for the rounding rules)
    public decimal SubTotal => DocumentMath.SubTotal(Lines);
    public decimal VatTotal => DocumentMath.VatTotal(Lines);
    public decimal Total => DocumentMath.Total(Lines, Discount);
}
