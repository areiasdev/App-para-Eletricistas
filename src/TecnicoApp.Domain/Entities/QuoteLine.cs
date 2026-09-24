using TecnicoApp.Domain.Common;

namespace TecnicoApp.Domain.Entities;

public class QuoteLine : BaseEntity, IDocumentLine
{
    public required string Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; } = DocumentMath.StandardVatRate;

    public Guid QuoteId { get; set; }
    public Quote Quote { get; set; } = null!;
}
