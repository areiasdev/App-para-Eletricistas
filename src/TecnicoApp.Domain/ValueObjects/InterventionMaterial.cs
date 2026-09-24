namespace TecnicoApp.Domain.ValueObjects;

/// <param name="UnitCost">What the material cost the company (for profitability).</param>
/// <param name="UnitPrice">What the client is charged per unit when the job is invoiced;
/// null = not yet priced (invoicing falls back to <paramref name="UnitCost"/>).</param>
public record InterventionMaterial(string Name, decimal Quantity, decimal UnitCost, decimal? UnitPrice = null);
