using TecnicoApp.Domain.Common;

namespace TecnicoApp.Domain.Entities;

public class InvoiceLine : BaseEntity, IDocumentLine
{
    public required string Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; } = DocumentMath.StandardVatRate;

    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
}
