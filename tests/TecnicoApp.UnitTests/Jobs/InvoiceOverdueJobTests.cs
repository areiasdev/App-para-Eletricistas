using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.Infrastructure.Jobs;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Jobs;

public class InvoiceOverdueJobTests
{
    [Fact]
    public async Task RunAsync_marks_only_issued_invoices_past_due_as_overdue()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var client = new Client { Name = "Cliente", UserId = owner.Id };
        db.Users.Add(owner);
        db.Clients.Add(client);

        Invoice Make(string number, InvoiceStatus status, int dueInDays) => new()
        {
            Number = number, Status = status, ClientId = client.Id, UserId = owner.Id,
            IssuedAt = DateTime.UtcNow.AddDays(-40), DueDate = DateTime.UtcNow.Date.AddDays(dueInDays),
        };
        db.Invoices.AddRange(
            Make("FT-1", InvoiceStatus.Issued, -1),
            Make("FT-2", InvoiceStatus.Issued, 5),
            Make("FT-3", InvoiceStatus.Paid, -10),
            Make("FT-4", InvoiceStatus.Cancelled, -10));
        await db.SaveChangesAsync(CancellationToken.None);

        await new InvoiceOverdueJob(db, Substitute.For<ILogger<InvoiceOverdueJob>>()).RunAsync();

        db.Invoices.ToDictionary(i => i.Number, i => i.Status).Should().BeEquivalentTo(new Dictionary<string, InvoiceStatus>
        {
            ["FT-1"] = InvoiceStatus.Overdue,
            ["FT-2"] = InvoiceStatus.Issued,
            ["FT-3"] = InvoiceStatus.Paid,
            ["FT-4"] = InvoiceStatus.Cancelled,
        });
    }
}
