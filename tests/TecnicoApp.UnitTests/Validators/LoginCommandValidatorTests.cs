using FluentAssertions;
using TecnicoApp.Application.Features.Auth.Commands.Login;
using Xunit;

namespace TecnicoApp.UnitTests.Validators;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    private static LoginCommand Valid() => new("joao@example.com", "qualquerpassword");

    [Fact]
    public void Valid_command_passes()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-email")]
    public void Invalid_email_fails(string email)
    {
        var result = _validator.Validate(Valid() with { Email = email });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginCommand.Email));
    }

    [Fact]
    public void Empty_password_fails()
    {
        var result = _validator.Validate(Valid() with { Password = "" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginCommand.Password));
    }
}
