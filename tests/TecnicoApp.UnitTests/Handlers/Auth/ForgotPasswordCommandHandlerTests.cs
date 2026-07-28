using FluentAssertions;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Auth.Commands.ForgotPassword;
using TecnicoApp.Domain.Entities;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Auth;

public class ForgotPasswordCommandHandlerTests
{
    [Fact]
    public async Task Handle_existing_email_stores_a_reset_token()
    {
        using var db = TestDb.Create();

        var user = new User { Email = "user@x.pt", PasswordHash = "h", FullName = "User" };
        db.Users.Add(user);
        await db.SaveChangesAsync(CancellationToken.None);

        var emailService = Substitute.For<IEmailService>();
        var appSettings = Substitute.For<IAppSettings>();
        appSettings.BaseUrl.Returns("https://app.example.com");

        var handler = new ForgotPasswordCommandHandler(db, emailService, appSettings);

        var result = await handler.Handle(new ForgotPasswordCommand("user@x.pt"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var saved = db.Users.Single();
        saved.PasswordResetTokenHash.Should().NotBeNullOrEmpty();
        saved.PasswordResetTokenExpiresAt.Should().NotBeNull();
        await emailService.Received(1).SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_nonexistent_email_still_returns_success_to_avoid_user_enumeration()
    {
        using var db = TestDb.Create();

        var emailService = Substitute.For<IEmailService>();
        var appSettings = Substitute.For<IAppSettings>();

        var handler = new ForgotPasswordCommandHandler(db, emailService, appSettings);

        var result = await handler.Handle(new ForgotPasswordCommand("ghost@x.pt"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await emailService.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }
}
