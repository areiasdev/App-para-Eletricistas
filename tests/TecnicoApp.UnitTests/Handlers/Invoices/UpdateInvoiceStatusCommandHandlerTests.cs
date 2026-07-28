using FluentAssertions;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Invoices.Commands.UpdateInvoiceStatus;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Invoices;

public class UpdateInvoiceStatusCommandHandlerTests
{
    private static ICurrentUserService AsUser(User user)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(user.Id);
        currentUser.Email.Returns(user.Email);
        return currentUser;
    }

    private static (User owner, User technician, Client client, Invoice invoice) SeedInvoice(
        TecnicoApp.Infrastructure.Persistence.AppDbContext db, InvoiceStatus status = InvoiceStatus.Issued)
    {
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner", Role = UserRole.Owner };
        var technician = new User
        {
            Email = "tech@x.pt", PasswordHash = "h", FullName = "Tech",
            OwnerId = owner.Id, Role = UserRole.Technician
        };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        var invoice = new Invoice
        {
            Number = "FT-2026-0001", ClientId = client.Id, UserId = owner.Id, Status = status,
            IssuedAt = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(30),
        };
        db.Users.AddRange(owner, technician);
        db.Clients.Add(client);
        db.Invoices.Add(invoice);
        return (owner, technician, client, invoice);
    }

    [Fact]
    public async Task Handle_technician_is_forbidden()
    {
        using var db = TestDb.Create();
        var (_, technician, _, invoice) = SeedInvoice(db);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateInvoiceStatusCommandHandler(db, AsUser(technician));
        var result = await handler.Handle(new UpdateInvoiceStatusCommand(invoice.Id, InvoiceStatus.Paid), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
        db.Invoices.Single().Status.Should().Be(InvoiceStatus.Issued);
    }

    [Theory]
    [InlineData(InvoiceStatus.Issued, InvoiceStatus.Paid, true)]
    [InlineData(InvoiceStatus.Issued, InvoiceStatus.Cancelled, true)]
    [InlineData(InvoiceStatus.Issued, InvoiceStatus.Overdue, true)]
    [InlineData(InvoiceStatus.Overdue, InvoiceStatus.Paid, true)]
    [InlineData(InvoiceStatus.Overdue, InvoiceStatus.Cancelled, true)]
    [InlineData(InvoiceStatus.Paid, InvoiceStatus.Issued, false)]
    [InlineData(InvoiceStatus.Cancelled, InvoiceStatus.Paid, false)]
    [InlineData(InvoiceStatus.Issued, InvoiceStatus.Issued, false)]
    public async Task Handle_transitions_are_validated(InvoiceStatus from, InvoiceStatus to, bool expectedValid)
    {
        using var db = TestDb.Create();
        var (owner, _, _, invoice) = SeedInvoice(db, from);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateInvoiceStatusCommandHandler(db, AsUser(owner));
        var result = await handler.Handle(new UpdateInvoiceStatusCommand(invoice.Id, to), CancellationToken.None);

        result.IsSuccess.Should().Be(expectedValid);
        if (!expectedValid)
            db.Invoices.Single().Status.Should().Be(from);
    }

    [Fact]
    public async Task Handle_marking_paid_sets_paid_at()
    {
        using var db = TestDb.Create();
        var (owner, _, _, invoice) = SeedInvoice(db);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateInvoiceStatusCommandHandler(db, AsUser(owner));
        var result = await handler.Handle(new UpdateInvoiceStatusCommand(invoice.Id, InvoiceStatus.Paid), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var saved = db.Invoices.Single();
        saved.Status.Should().Be(InvoiceStatus.Paid);
        saved.PaidAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_marking_cancelled_does_not_set_paid_at()
    {
        using var db = TestDb.Create();
        var (owner, _, _, invoice) = SeedInvoice(db);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateInvoiceStatusCommandHandler(db, AsUser(owner));
        var result = await handler.Handle(new UpdateInvoiceStatusCommand(invoice.Id, InvoiceStatus.Cancelled), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var saved = db.Invoices.Single();
        saved.Status.Should().Be(InvoiceStatus.Cancelled);
        saved.PaidAt.Should().BeNull();
    }
}
