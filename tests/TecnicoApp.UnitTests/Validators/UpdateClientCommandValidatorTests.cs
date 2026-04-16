using FluentAssertions;
using TecnicoApp.Application.Features.Clients.Commands.CreateClient;
using TecnicoApp.Application.Features.Clients.Commands.UpdateClient;
using Xunit;

namespace TecnicoApp.UnitTests.Validators;

public class UpdateClientCommandValidatorTests
{
    private readonly UpdateClientCommandValidator _validator = new();

    private static UpdateClientCommand Valid() =>
        new(Guid.NewGuid(), "Empresa XPTO", null, null, null, null, null);

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
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateClientCommand.ClientId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_name_fails(string name)
    {
        var result = _validator.Validate(Valid() with { Name = name });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateClientCommand.Name));
    }

    [Theory]
    [InlineData("12345678")]   // 8 digits
    [InlineData("1234567890")] // 10 digits
    [InlineData("12345678a")]  // non-digit
    public void Invalid_nif_fails(string nif)
    {
        var result = _validator.Validate(Valid() with { Nif = nif });
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Valid_nif_passes()
    {
        _validator.Validate(Valid() with { Nif = "123456789" }).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@")]
    public void Invalid_email_fails(string email)
    {
        var result = _validator.Validate(Valid() with { Email = email });
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Valid_postal_code_passes()
    {
        var address = new CreateAddressCommand("Rua A", "Porto", "4000-001");
        _validator.Validate(Valid() with { Address = address }).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Invalid_postal_code_fails()
    {
        var address = new CreateAddressCommand("Rua A", "Porto", "4000001");
        var result = _validator.Validate(Valid() with { Address = address });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("PostalCode"));
    }
}
