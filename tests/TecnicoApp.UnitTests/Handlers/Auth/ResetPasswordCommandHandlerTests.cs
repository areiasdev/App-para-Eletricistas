using FluentAssertions;
using TecnicoApp.Application.Features.Auth.Commands.ResetPassword;
using TecnicoApp.Domain.Entities;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Auth;

public class ResetPasswordCommandHandlerTests
{
    [Fact]
    public async Task Handle_valid_token_updates_password_and_clears_all_tokens()
    {
        using var db = TestDb.Create();
        var user = new User
        {
            Email = "user@x.pt",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("old-password"),
            FullName = "User",
            PasswordResetTokenHash = BCrypt.Net.BCrypt.HashPassword("valid-reset-token"),
            PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1),
            RefreshTokenHash = "hash-of-active-session",
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(1),
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new ResetPasswordCommandHandler(db);

        var result = await handler.Handle(
            new ResetPasswordCommand("user@x.pt", "valid-reset-token", "new-password"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var saved = db.Users.Single();
        BCrypt.Net.BCrypt.Verify("new-password", saved.PasswordHash).Should().BeTrue();
        saved.PasswordResetTokenHash.Should().BeNull();
        saved.PasswordResetTokenExpiresAt.Should().BeNull();
        // Resetting the password must also kill any active session tied to the old credentials.
        saved.RefreshTokenHash.Should().BeNull();
        saved.RefreshTokenExpiresAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_expired_token_returns_invalid_and_does_not_change_password()
    {
        using var db = TestDb.Create();
        var user = new User
        {
            Email = "user@x.pt",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("old-password"),
            FullName = "User",
            PasswordResetTokenHash = BCrypt.Net.BCrypt.HashPassword("expired-token"),
            PasswordResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(-1),
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new ResetPasswordCommandHandler(db);

        var result = await handler.Handle(
            new ResetPasswordCommand("user@x.pt", "expired-token", "new-password"),
            CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Invalid);
        var saved = db.Users.Single();
        BCrypt.Net.BCrypt.Verify("old-password", saved.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_wrong_token_returns_invalid_and_does_not_change_password()
    {
        using var db = TestDb.Create();
        var user = new User
        {
            Email = "user@x.pt",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("old-password"),
            FullName = "User",
            PasswordResetTokenHash = BCrypt.Net.BCrypt.HashPassword("correct-token"),
            PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1),
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new ResetPasswordCommandHandler(db);

        var result = await handler.Handle(
            new ResetPasswordCommand("user@x.pt", "wrong-token", "new-password"),
            CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Invalid);
        var saved = db.Users.Single();
        BCrypt.Net.BCrypt.Verify("old-password", saved.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_unknown_email_returns_same_invalid_status_as_wrong_token()
    {
        // Both cases must be indistinguishable to the caller — otherwise the endpoint
        // becomes a user-enumeration oracle (the handler always runs BCrypt.Verify against
        // a dummy hash when no user is found, to keep timing consistent).
        using var db = TestDb.Create();

        var handler = new ResetPasswordCommandHandler(db);

        var result = await handler.Handle(
            new ResetPasswordCommand("ghost@x.pt", "whatever-token", "new-password"),
            CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Invalid);
    }
}
