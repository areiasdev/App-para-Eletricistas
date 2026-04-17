using FluentAssertions;
using TecnicoApp.Application.Features.Auth.Commands.ResetPassword;
using Xunit;

namespace TecnicoApp.UnitTests.Validators;

public class ResetPasswordCommandValidatorTests
{
    private readonly ResetPasswordCommandValidator _validator = new();

    private static ResetPasswordCommand Valid() =>
        new("user@exemplo.pt", "valid-token-123", "Passw0rd1");

    [Fact]
    public void Valid_command_passes()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_email_fails(string email)
    {
        var result = _validator.Validate(Valid() with { Email = email });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ResetPasswordCommand.Email));
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing@")]
    public void Invalid_email_format_fails(string email)
    {
        var result = _validator.Validate(Valid() with { Email = email });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ResetPasswordCommand.Email));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_token_fails(string token)
    {
        var result = _validator.Validate(Valid() with { Token = token });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ResetPasswordCommand.Token));
    }

    [Fact]
    public void Password_under_8_chars_fails()
    {
        var result = _validator.Validate(Valid() with { NewPassword = "Ab1" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ResetPasswordCommand.NewPassword));
    }

    [Fact]
    public void Password_without_uppercase_fails()
    {
        var result = _validator.Validate(Valid() with { NewPassword = "passw0rd" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("maiúscula"));
    }

    [Fact]
    public void Password_without_digit_fails()
    {
        var result = _validator.Validate(Valid() with { NewPassword = "PasswordNoDigit" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("número"));
    }

    [Fact]
    public void Password_meeting_all_rules_passes()
    {
        _validator.Validate(Valid() with { NewPassword = "Segura123" }).IsValid.Should().BeTrue();
    }
}
