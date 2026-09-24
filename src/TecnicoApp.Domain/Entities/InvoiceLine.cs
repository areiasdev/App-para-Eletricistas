using TecnicoApp.Domain.Common;

namespace TecnicoApp.Domain.Entities;

public class InvoiceLine : BaseEntity, IDocumentLine
{
    public required string Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; } = DocumentMath.StandardVatRate;

    /// <summary>Unit of measure shown on the document (un, m, m², h, kg, vg…).</summary>
    public string Unit { get; set; } = DocumentMath.DefaultUnit;

    /// <summary>Order the line was entered in — documents always list lines by this.</summary>
    public int Position { get; set; }

    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
}
