using FluentAssertions;
using TecnicoApp.Application.Features.Clients.Commands.CreateClient;
using Xunit;

namespace TecnicoApp.UnitTests.Validators;

public class CreateClientCommandValidatorTests
{
    private readonly CreateClientCommandValidator _validator = new();

    private static CreateClientCommand Valid() =>
        new("Empresa XPTO", null, null, null, null, null);

    [Fact]
    public void Valid_minimal_command_passes()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_name_fails(string name)
    {
        var result = _validator.Validate(Valid() with { Name = name });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateClientCommand.Name));
    }

    [Fact]
    public void Name_over_200_chars_fails()
    {
        var result = _validator.Validate(Valid() with { Name = new string('x', 201) });
        result.IsValid.Should().BeFalse();
    }

    // NIF validation
    [Theory]
    [InlineData("12345678")]   // 8 digits — too short
    [InlineData("1234567890")] // 10 digits — too long
    [InlineData("12345678a")]  // non-digit char
    public void Invalid_nif_fails(string nif)
    {
        var result = _validator.Validate(Valid() with { Nif = nif });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateClientCommand.Nif));
    }

    [Fact]
    public void Valid_nif_passes()
    {
        var result = _validator.Validate(Valid() with { Nif = "123456789" });
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Null_nif_passes()
    {
        var result = _validator.Validate(Valid() with { Nif = null });
        result.IsValid.Should().BeTrue();
    }

    // Email validation
    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@")]
    public void Invalid_email_fails(string email)
    {
        var result = _validator.Validate(Valid() with { Email = email });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateClientCommand.Email));
    }

    [Fact]
    public void Valid_email_passes()
    {
        var result = _validator.Validate(Valid() with { Email = "cliente@empresa.pt" });
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Null_email_passes()
    {
        var result = _validator.Validate(Valid() with { Email = null });
        result.IsValid.Should().BeTrue();
    }

    // Address validation
    [Fact]
    public void Valid_address_passes()
    {
        var address = new CreateAddressCommand("Rua das Flores 1", "Lisboa", "1000-001");
        var result = _validator.Validate(Valid() with { Address = address });
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("1000001")]   // missing dash
    [InlineData("10000-01")]  // wrong format
    [InlineData("ABCD-EFG")]  // non-digits
    public void Invalid_postal_code_fails(string postalCode)
    {
        var address = new CreateAddressCommand("Rua A", "Lisboa", postalCode);
        var result = _validator.Validate(Valid() with { Address = address });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("PostalCode"));
    }

    [Fact]
    public void Address_with_empty_street_fails()
    {
        var address = new CreateAddressCommand("", "Lisboa", "1000-001");
        var result = _validator.Validate(Valid() with { Address = address });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Street"));
    }

    [Fact]
    public void Null_address_passes()
    {
        var result = _validator.Validate(Valid() with { Address = null });
        result.IsValid.Should().BeTrue();
    }
}
