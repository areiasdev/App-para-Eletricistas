using FluentAssertions;
using TecnicoApp.Application.Features.Team.Commands.InviteTeamMember;
using TecnicoApp.Domain.Enums;
using Xunit;

namespace TecnicoApp.UnitTests.Validators;

public class InviteTeamMemberCommandValidatorTests
{
    private readonly InviteTeamMemberCommandValidator _validator = new();

    private static InviteTeamMemberCommand Valid() =>
        new("tecnico@empresa.pt", UserRole.Technician);

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
        result.Errors.Should().Contain(e => e.PropertyName == nameof(InviteTeamMemberCommand.Email));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@")]
    [InlineData("@nodomain")]
    public void Invalid_email_format_fails(string email)
    {
        var result = _validator.Validate(Valid() with { Email = email });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("inválido") || e.PropertyName == nameof(InviteTeamMemberCommand.Email));
    }

    [Fact]
    public void Email_over_256_chars_fails()
    {
        var longEmail = new string('a', 249) + "@test.pt"; // 249 + 8 = 257 chars
        var result = _validator.Validate(Valid() with { Email = longEmail });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(InviteTeamMemberCommand.Email));
    }

    [Theory]
    [InlineData(UserRole.Technician)]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.Commercial)]
    public void Valid_roles_pass(UserRole role)
    {
        _validator.Validate(Valid() with { Role = role }).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Owner_role_is_rejected()
    {
        var result = _validator.Validate(Valid() with { Role = UserRole.Owner });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(InviteTeamMemberCommand.Role));
    }
}
