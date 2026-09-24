using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.Infrastructure.Jobs;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Jobs;

public class QuoteFollowUpJobTests
{
    [Fact]
    public async Task RunAsync_sends_one_digest_per_owner_and_never_repeats_a_quote()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var client = new Client { Name = "Cliente", UserId = owner.Id };
        db.Users.Add(owner);
        db.Clients.Add(client);
        Quote Make(string n, QuoteStatus s, int sentDaysAgo) => new()
        {
            Number = n, Status = s, ClientId = client.Id, UserId = owner.Id,
            EmailSentAt = DateTime.UtcNow.AddDays(-sentDaysAgo),
        };
        db.Quotes.AddRange(
            Make("ORC-1", QuoteStatus.Sent, 10),
            Make("ORC-2", QuoteStatus.Sent, 8),
            Make("ORC-3", QuoteStatus.Sent, 2),       // too recent
            Make("ORC-4", QuoteStatus.Accepted, 10)); // already answered
        await db.SaveChangesAsync(CancellationToken.None);

        var email = Substitute.For<IEmailService>();
        var job = new QuoteFollowUpJob(db, email, Substitute.For<IAppSettings>(), Substitute.For<ILogger<QuoteFollowUpJob>>());

        await job.RunAsync();
        await job.RunAsync();

        await email.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.To == "owner@x.pt" && m.HtmlBody.Contains("ORC-1") && m.HtmlBody.Contains("ORC-2") && !m.HtmlBody.Contains("ORC-3")),
            Arg.Any<CancellationToken>());
        db.Quotes.Where(q => q.FollowUpSentAt != null).Select(q => q.Number).Should().BeEquivalentTo(["ORC-1", "ORC-2"]);
    }
}
