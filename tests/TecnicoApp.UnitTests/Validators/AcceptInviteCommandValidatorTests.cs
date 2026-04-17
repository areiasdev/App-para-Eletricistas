using FluentAssertions;
using TecnicoApp.Application.Features.Team.Commands.AcceptInvite;
using Xunit;

namespace TecnicoApp.UnitTests.Validators;

public class AcceptInviteCommandValidatorTests
{
    private readonly AcceptInviteCommandValidator _validator = new();

    private static AcceptInviteCommand Valid() =>
        new("abc123token", "João Silva", "Password1");

    [Fact]
    public void Valid_command_passes()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_token_fails(string token)
    {
        var result = _validator.Validate(Valid() with { Token = token });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AcceptInviteCommand.Token));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_fullName_fails(string name)
    {
        var result = _validator.Validate(Valid() with { FullName = name });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AcceptInviteCommand.FullName));
    }

    [Fact]
    public void FullName_over_200_chars_fails()
    {
        var result = _validator.Validate(Valid() with { FullName = new string('a', 201) });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AcceptInviteCommand.FullName));
    }

    [Fact]
    public void Password_under_8_chars_fails()
    {
        var result = _validator.Validate(Valid() with { NewPassword = "short" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AcceptInviteCommand.NewPassword));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_password_fails(string pwd)
    {
        var result = _validator.Validate(Valid() with { NewPassword = pwd });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AcceptInviteCommand.NewPassword));
    }

    [Fact]
    public void Password_exactly_8_chars_passes()
    {
        _validator.Validate(Valid() with { NewPassword = "12345678" }).IsValid.Should().BeTrue();
    }
}
