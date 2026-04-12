namespace TecnicoApp.Domain.Entities;

public class QuoteLine : BaseEntity
{
    public required string Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; } = 23m;  // IVA normal PT

    public Guid QuoteId { get; set; }
    public Quote Quote { get; set; } = null!;
}
