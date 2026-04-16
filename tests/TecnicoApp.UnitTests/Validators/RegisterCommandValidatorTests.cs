using FluentAssertions;
using TecnicoApp.Application.Features.Auth.Commands.Register;
using Xunit;

namespace TecnicoApp.UnitTests.Validators;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    private static RegisterCommand Valid() =>
        new("João Silva", "joao@example.com", "Senha123");

    [Fact]
    public void Valid_command_passes()
    {
        var result = _validator.Validate(Valid());
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_fullName_fails(string name)
    {
        var result = _validator.Validate(Valid() with { FullName = name });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.FullName));
    }

    [Fact]
    public void FullName_over_200_chars_fails()
    {
        var result = _validator.Validate(Valid() with { FullName = new string('a', 201) });
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("missing@")]
    [InlineData("@nodomain.com")]
    public void Invalid_email_fails(string email)
    {
        var result = _validator.Validate(Valid() with { Email = email });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.Email));
    }

    [Fact]
    public void Valid_email_passes()
    {
        var result = _validator.Validate(Valid() with { Email = "tecnico@empresa.pt" });
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]           // empty
    [InlineData("short1")]     // too short (< 8 chars)
    [InlineData("semmaius1")]  // no uppercase
    [InlineData("SemNumero")]  // no digit
    public void Weak_password_fails(string password)
    {
        var result = _validator.Validate(Valid() with { Password = password });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.Password));
    }

    [Theory]
    [InlineData("Valida12")]
    [InlineData("Password9")]
    [InlineData("Abc12345")]
    public void Strong_password_passes(string password)
    {
        var result = _validator.Validate(Valid() with { Password = password });
        result.IsValid.Should().BeTrue();
    }
}
