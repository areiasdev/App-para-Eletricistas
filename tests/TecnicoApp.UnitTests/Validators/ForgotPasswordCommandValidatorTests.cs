using FluentAssertions;
using TecnicoApp.Application.Features.Auth.Commands.ForgotPassword;
using Xunit;

namespace TecnicoApp.UnitTests.Validators;

public class ForgotPasswordCommandValidatorTests
{
    private readonly ForgotPasswordCommandValidator _validator = new();

    [Fact]
    public void Valid_email_passes()
    {
        _validator.Validate(new ForgotPasswordCommand("user@exemplo.pt")).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_email_fails(string email)
    {
        var result = _validator.Validate(new ForgotPasswordCommand(email));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ForgotPasswordCommand.Email));
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing@")]
    [InlineData("@nodomain.pt")]
    public void Invalid_email_format_fails(string email)
    {
        var result = _validator.Validate(new ForgotPasswordCommand(email));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ForgotPasswordCommand.Email));
    }
}
