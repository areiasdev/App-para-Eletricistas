using FluentAssertions;
using TecnicoApp.Domain.Entities;
using Xunit;

namespace TecnicoApp.UnitTests.Domain;

public class QuoteCalculationTests
{
    [Fact]
    public void SubTotal_sums_line_quantity_times_unit_price()
    {
        var quote = new Quote
        {
            Number = "ORC-2026-0001",
            Lines =
            [
                new QuoteLine { Description = "A", Quantity = 2, UnitPrice = 10m, VatRate = 23m },
                new QuoteLine { Description = "B", Quantity = 1, UnitPrice = 5m, VatRate = 23m },
            ],
        };

        quote.SubTotal.Should().Be(25m);
    }

    [Fact]
    public void VatTotal_applies_each_line_own_vat_rate()
    {
        var quote = new Quote
        {
            Number = "ORC-2026-0001",
            Lines =
            [
                new QuoteLine { Description = "A", Quantity = 1, UnitPrice = 100m, VatRate = 23m }, // 23.00
                new QuoteLine { Description = "B", Quantity = 1, UnitPrice = 100m, VatRate = 6m },   // 6.00
            ],
        };

        quote.VatTotal.Should().Be(29m);
    }

    [Fact]
    public void Total_subtracts_discount_from_subtotal_plus_vat()
    {
        var quote = new Quote
        {
            Number = "ORC-2026-0001",
            Discount = 10m,
            Lines = [new QuoteLine { Description = "A", Quantity = 1, UnitPrice = 100m, VatRate = 23m }],
        };

        // SubTotal 100 + VAT 23 - Discount 10 = 113
        quote.Total.Should().Be(113m);
    }

    [Fact]
    public void Total_with_no_discount_and_no_lines_is_zero()
    {
        var quote = new Quote { Number = "ORC-2026-0001" };

        quote.Total.Should().Be(0m);
    }

    [Fact]
    public void Line_totals_are_rounded_individually_before_summing_to_avoid_cent_drift()
    {
        var quote = new Quote
        {
            Number = "ORC-2026-0001",
            Lines = Enumerable.Range(0, 3)
                .Select(_ => new QuoteLine { Description = "X", Quantity = 1, UnitPrice = 0.1m, VatRate = 0m })
                .ToList(),
        };

        quote.SubTotal.Should().Be(0.3m);
    }
}
