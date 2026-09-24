using System.Text;
using FluentAssertions;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Invoices.Queries.ExportInvoices;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Invoices;

public class ExportInvoicesCsvQueryHandlerTests
{
    [Fact]
    public async Task Exports_invoices_in_range_with_pt_formatting_and_formula_protection()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var client = new Client { Name = "=HYPERLINK(\"x\")", Nif = "123456789", UserId = owner.Id };
        db.Users.Add(owner);
        db.Clients.Add(client);
        db.Invoices.Add(new Invoice
        {
            Number = "FT-2026-0001", ClientId = client.Id, UserId = owner.Id, Status = InvoiceStatus.Paid,
            IssuedAt = new DateTime(2026, 3, 10), DueDate = new DateTime(2026, 4, 9),
            Lines = [new InvoiceLine { Description = "Serviço", Quantity = 1, UnitPrice = 1234.5m }],
        });
        await db.SaveChangesAsync(CancellationToken.None);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(owner.Id);
        var result = await new ExportInvoicesCsvQueryHandler(db, currentUser)
            .Handle(new ExportInvoicesCsvQuery(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31)), CancellationToken.None);

        var csv = Encoding.UTF8.GetString(result.Value);
        csv.Should().Contain("\"FT-2026-0001\";10/03/2026;09/04/2026;Paga;");
        csv.Should().Contain("1234,50;283,94;0,00;1518,44");
        csv.Should().Contain("\"'=HYPERLINK(\"\"x\"\")\"", "formulas in client names must not execute in Excel");
    }

    [Fact]
    public async Task Technician_cannot_export()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var tech = new User { Email = "t@x.pt", PasswordHash = "h", FullName = "T", OwnerId = owner.Id, Role = UserRole.Technician };
        db.Users.AddRange(owner, tech);
        await db.SaveChangesAsync(CancellationToken.None);
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(tech.Id);

        var result = await new ExportInvoicesCsvQueryHandler(db, currentUser).Handle(new ExportInvoicesCsvQuery(null, null), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
    }
}
