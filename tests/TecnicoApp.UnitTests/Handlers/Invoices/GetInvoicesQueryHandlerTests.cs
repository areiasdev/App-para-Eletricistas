using FluentAssertions;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Invoices.Queries.GetInvoices;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.Infrastructure.Persistence;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Invoices;

public class GetInvoicesQueryHandlerTests
{
    private static ICurrentUserService AsUser(User user)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(user.Id);
        currentUser.Email.Returns(user.Email);
        return currentUser;
    }

    private static Invoice MakeInvoice(
        User owner, Client client, string number, InvoiceStatus status, DateTime createdAt, decimal? discount = null) =>
        new()
        {
            Number = number, ClientId = client.Id, UserId = owner.Id, Status = status,
            IssuedAt = createdAt, DueDate = createdAt.AddDays(30), CreatedAt = createdAt, Discount = discount,
            Lines = [new InvoiceLine { Description = "Serviço", Quantity = 1, UnitPrice = 100m, VatRate = 23m }],
        };

    [Fact]
    public async Task Handle_team_member_sees_only_own_tenants_invoices()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner", Role = UserRole.Owner };
        var technician = new User
        {
            Email = "tech@x.pt", PasswordHash = "h", FullName = "Tech",
            OwnerId = owner.Id, Role = UserRole.Technician
        };
        var otherOwner = new User { Email = "other@x.pt", PasswordHash = "h", FullName = "Other", Role = UserRole.Owner };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        var otherClient = new Client { Name = "Cliente de outra empresa", UserId = otherOwner.Id };

        var ownInvoice = MakeInvoice(owner, client, "FT-2026-0001", InvoiceStatus.Issued, DateTime.UtcNow);
        var otherInvoice = MakeInvoice(otherOwner, otherClient, "FT-2026-9001", InvoiceStatus.Issued, DateTime.UtcNow);

        db.Users.AddRange(owner, technician, otherOwner);
        db.Clients.AddRange(client, otherClient);
        db.Invoices.AddRange(ownInvoice, otherInvoice);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetInvoicesQueryHandler(db, AsUser(technician));
        var result = await handler.Handle(new GetInvoicesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(i => i.Id == ownInvoice.Id);
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_filters_by_status()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner", Role = UserRole.Owner };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };

        var paid = MakeInvoice(owner, client, "FT-2026-0001", InvoiceStatus.Paid, DateTime.UtcNow);
        var issued = MakeInvoice(owner, client, "FT-2026-0002", InvoiceStatus.Issued, DateTime.UtcNow);
        var cancelled = MakeInvoice(owner, client, "FT-2026-0003", InvoiceStatus.Cancelled, DateTime.UtcNow);

        db.Users.Add(owner);
        db.Clients.Add(client);
        db.Invoices.AddRange(paid, issued, cancelled);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetInvoicesQueryHandler(db, AsUser(owner));
        var result = await handler.Handle(new GetInvoicesQuery(Status: InvoiceStatus.Paid), CancellationToken.None);

        result.Value.Items.Should().ContainSingle(i => i.Id == paid.Id);
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_orders_by_created_at_descending_paginates_and_computes_total_with_discount()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner", Role = UserRole.Owner };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };

        var oldest = MakeInvoice(owner, client, "FT-2026-0001", InvoiceStatus.Issued, DateTime.UtcNow.AddDays(-2));
        // Line total = 1 * 100 * 1.23 = 123; with a 10 discount the DTO total should be 113.
        var middle = MakeInvoice(owner, client, "FT-2026-0002", InvoiceStatus.Issued, DateTime.UtcNow.AddDays(-1), discount: 10m);
        var newest = MakeInvoice(owner, client, "FT-2026-0003", InvoiceStatus.Issued, DateTime.UtcNow);

        db.Users.Add(owner);
        db.Clients.Add(client);
        db.Invoices.AddRange(oldest, middle, newest);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetInvoicesQueryHandler(db, AsUser(owner));

        var firstPage = await handler.Handle(new GetInvoicesQuery(Page: 1, PageSize: 2), CancellationToken.None);
        firstPage.Value.TotalCount.Should().Be(3);
        firstPage.Value.Items.Should().HaveCount(2);
        firstPage.Value.Items.Select(i => i.Id).Should().ContainInOrder(newest.Id, middle.Id);
        firstPage.Value.Items.Single(i => i.Id == middle.Id).Total.Should().Be(113m);

        var secondPage = await handler.Handle(new GetInvoicesQuery(Page: 2, PageSize: 2), CancellationToken.None);
        secondPage.Value.Items.Should().ContainSingle(i => i.Id == oldest.Id);
    }
}
