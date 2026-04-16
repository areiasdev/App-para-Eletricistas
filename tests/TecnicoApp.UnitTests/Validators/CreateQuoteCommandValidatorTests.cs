using FluentAssertions;
using TecnicoApp.Application.Features.Quotes.Commands.CreateQuote;
using Xunit;

namespace TecnicoApp.UnitTests.Validators;

public class CreateQuoteCommandValidatorTests
{
    private readonly CreateQuoteCommandValidator _validator = new();

    private static CreateQuoteLineRequest ValidLine() =>
        new("Mão de obra", 2, 50m, 23m);

    private static CreateQuoteCommand Valid() =>
        new(Guid.NewGuid(), null, null, null, [ValidLine()]);

    [Fact]
    public void Valid_command_passes()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_clientId_fails()
    {
        var result = _validator.Validate(Valid() with { ClientId = Guid.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateQuoteCommand.ClientId));
    }

    [Fact]
    public void Empty_lines_fails()
    {
        var result = _validator.Validate(Valid() with { Lines = [] });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("pelo menos uma linha"));
    }

    [Fact]
    public void Line_with_empty_description_fails()
    {
        var line = ValidLine() with { Description = "" };
        var result = _validator.Validate(Valid() with { Lines = [line] });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("descrição"));
    }

    [Fact]
    public void Line_with_zero_quantity_fails()
    {
        var line = ValidLine() with { Quantity = 0 };
        var result = _validator.Validate(Valid() with { Lines = [line] });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("quantidade"));
    }

    [Fact]
    public void Line_with_negative_quantity_fails()
    {
        var line = ValidLine() with { Quantity = -1 };
        var result = _validator.Validate(Valid() with { Lines = [line] });
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Line_with_negative_unit_price_fails()
    {
        var line = ValidLine() with { UnitPrice = -0.01m };
        var result = _validator.Validate(Valid() with { Lines = [line] });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("preço unitário"));
    }

    [Fact]
    public void Line_with_zero_unit_price_passes()
    {
        var line = ValidLine() with { UnitPrice = 0 };
        var result = _validator.Validate(Valid() with { Lines = [line] });
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Line_with_invalid_vat_rate_fails(decimal vat)
    {
        var line = ValidLine() with { VatRate = vat };
        var result = _validator.Validate(Valid() with { Lines = [line] });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("IVA"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(13)]
    [InlineData(23)]
    [InlineData(100)]
    public void Valid_vat_rates_pass(decimal vat)
    {
        var line = ValidLine() with { VatRate = vat };
        _validator.Validate(Valid() with { Lines = [line] }).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Negative_discount_fails()
    {
        var result = _validator.Validate(Valid() with { Discount = -1m });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("desconto"));
    }

    [Fact]
    public void Discount_exceeding_total_fails()
    {
        // Line total = 2 * 50 * 1.23 = 123
        var result = _validator.Validate(Valid() with { Discount = 200m });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("desconto"));
    }

    [Fact]
    public void Discount_equal_to_total_passes()
    {
        // Line total = 2 * 50 * 1.23 = 123
        var result = _validator.Validate(Valid() with { Discount = 123m });
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Null_discount_passes()
    {
        var result = _validator.Validate(Valid() with { Discount = null });
        result.IsValid.Should().BeTrue();
    }
}
