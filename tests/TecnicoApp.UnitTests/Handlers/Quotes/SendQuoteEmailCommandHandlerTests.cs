using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Quotes.Commands.SendQuoteEmail;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Quotes;

public class SendQuoteEmailCommandHandlerTests
{
    [Fact]
    public async Task Handle_issuer_details_come_from_team_owner_not_technician_who_sends()
    {
        using var db = TestDb.Create();

        var owner = new User
        {
            Email = "owner@x.pt", PasswordHash = "h", FullName = "Dona da Empresa",
            CompanyName = "Empresa Lda", Nif = "123456789", Phone = "912345678"
        };
        var technician = new User
        {
            Email = "tech@x.pt", PasswordHash = "h", FullName = "Técnico Vazio",
            OwnerId = owner.Id, Role = UserRole.Technician
            // CompanyName/Nif/Phone intentionally blank — a technician's own profile usually is
        };
        var client = new Client { Name = "Cliente A", Email = "cliente@x.pt", UserId = owner.Id };
        var quote = new Quote { Number = "ORC-2026-0001", ClientId = client.Id, UserId = owner.Id, Status = QuoteStatus.Draft };
        db.Users.AddRange(owner, technician);
        db.Clients.Add(client);
        db.Quotes.Add(quote);
        await db.SaveChangesAsync(CancellationToken.None);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(technician.Id);

        var pdfService = Substitute.For<IPdfService>();
        pdfService.GenerateQuotePdf(Arg.Any<QuotePdfData>()).Returns([1, 2, 3]);

        var emailService = Substitute.For<IEmailService>();
        var logger = Substitute.For<ILogger<SendQuoteEmailCommandHandler>>();

        var handler = new SendQuoteEmailCommandHandler(db, currentUser, pdfService, emailService, logger);

        var result = await handler.Handle(new SendQuoteEmailCommand(quote.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        pdfService.Received(1).GenerateQuotePdf(Arg.Is<QuotePdfData>(d =>
            d.IssuerName == owner.FullName &&
            d.IssuerCompany == owner.CompanyName &&
            d.IssuerNif == owner.Nif));
    }

    [Fact]
    public async Task Handle_quote_from_a_different_owner_is_forbidden()
    {
        using var db = TestDb.Create();

        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var otherOwner = new User { Email = "other@x.pt", PasswordHash = "h", FullName = "Other" };
        var foreignClient = new Client { Name = "Cliente alheio", Email = "c@x.pt", UserId = otherOwner.Id };
        var foreignQuote = new Quote { Number = "ORC-2026-0001", ClientId = foreignClient.Id, UserId = otherOwner.Id };
        db.Users.AddRange(owner, otherOwner);
        db.Clients.Add(foreignClient);
        db.Quotes.Add(foreignQuote);
        await db.SaveChangesAsync(CancellationToken.None);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(owner.Id);

        var pdfService = Substitute.For<IPdfService>();
        var emailService = Substitute.For<IEmailService>();
        var logger = Substitute.For<ILogger<SendQuoteEmailCommandHandler>>();

        var handler = new SendQuoteEmailCommandHandler(db, currentUser, pdfService, emailService, logger);

        var result = await handler.Handle(new SendQuoteEmailCommand(foreignQuote.Id), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
        await emailService.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }
}
