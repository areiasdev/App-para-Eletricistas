namespace TecnicoApp.Domain.Entities;

public class InvoiceLine : BaseEntity
{
    public required string Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; } = 23m;  // IVA normal PT

    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
}
