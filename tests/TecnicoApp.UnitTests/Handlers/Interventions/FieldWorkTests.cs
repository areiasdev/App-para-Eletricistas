using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Interventions.Commands.SignIntervention;
using TecnicoApp.Application.Features.Interventions.Commands.UpdateInterventionStatus;
using TecnicoApp.Application.Features.Invoices.Commands.CreateInvoiceFromIntervention;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.Domain.ValueObjects;
using TecnicoApp.Infrastructure.Persistence;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Interventions;

public class FieldWorkTests
{
    private const string Signature = "data:image/png;base64,iVBORw0KGgo=";

    private static ICurrentUserService As(User user)
    {
        var cu = Substitute.For<ICurrentUserService>();
        cu.UserId.Returns(user.Id);
        cu.Email.Returns(user.Email);
        return cu;
    }

    private static (User owner, Intervention job, Equipment boiler) Seed(
        AppDbContext db, InterventionStatus status = InterventionStatus.InProgress, decimal? hourlyRate = 40m)
    {
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner", DefaultHourlyRate = hourlyRate };
        var client = new Client { Name = "Cliente", UserId = owner.Id };
        var boiler = new Equipment { Type = "Quadro elétrico", ClientId = client.Id, MaintenanceIntervalMonths = 12, NextMaintenance = DateTime.UtcNow.AddDays(-3) };
        var job = new Intervention
        {
            Title = "Substituir diferencial", ClientId = client.Id, UserId = owner.Id, Status = status,
            LaborHours = 2.5m,
            Materials = [new InterventionMaterial("Diferencial 40A 30mA", 1, 30m, 45m), new InterventionMaterial("Cabo 2,5mm²", 10, 0.4m)],
            Equipment = [boiler],
        };
        db.Users.Add(owner);
        db.Clients.Add(client);
        db.Equipment.Add(boiler);
        db.Interventions.Add(job);
        db.SaveChanges();
        return (owner, job, boiler);
    }

    [Fact]
    public async Task Sign_completes_the_job_and_records_who_signed()
    {
        using var db = TestDb.Create();
        var (owner, job, _) = Seed(db);

        var result = await new SignInterventionCommandHandler(db, As(owner))
            .Handle(new SignInterventionCommand(job.Id, "Sr. Manuel", Signature), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var saved = db.Interventions.Single();
        saved.Status.Should().Be(InterventionStatus.Completed);
        saved.SignedByName.Should().Be("Sr. Manuel");
        saved.ClientSignatureUrl.Should().Be(Signature);
    }

    [Fact]
    public async Task Sign_rejects_a_fake_image()
    {
        using var db = TestDb.Create();
        var (owner, job, _) = Seed(db);

        var result = await new SignInterventionCommandHandler(db, As(owner))
            .Handle(new SignInterventionCommand(job.Id, "Sr. Manuel", "data:image/png;base64,AAAAAAAA"), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Invalid);
    }

    [Fact]
    public async Task Completing_a_job_rolls_equipment_maintenance_forward_by_its_interval()
    {
        using var db = TestDb.Create();
        var (owner, job, _) = Seed(db);

        await new UpdateInterventionStatusCommandHandler(db, As(owner))
            .Handle(new UpdateInterventionStatusCommand(job.Id, InterventionStatus.Completed), CancellationToken.None);

        db.Equipment.Single().NextMaintenance.Should().Be(DateTime.UtcNow.Date.AddMonths(12));
    }

    private static CreateInvoiceFromInterventionCommandHandler InvoiceHandler(AppDbContext db, User user)
    {
        var settings = Substitute.For<IAppSettings>();
        settings.InvoicePaymentTermDays.Returns(30);
        return new CreateInvoiceFromInterventionCommandHandler(db, As(user), settings,
            Substitute.For<ILogger<CreateInvoiceFromInterventionCommandHandler>>());
    }

    [Fact]
    public async Task Invoice_from_job_bills_hours_at_company_rate_and_materials_at_sale_price()
    {
        using var db = TestDb.Create();
        var (owner, job, _) = Seed(db, InterventionStatus.Completed);

        var result = await InvoiceHandler(db, owner).Handle(new CreateInvoiceFromInterventionCommand(job.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Errors.FirstOrDefault());
        var lines = result.Value.Lines;
        lines.Should().HaveCount(3);
        lines[0].Should().Match<TecnicoApp.Application.Features.Invoices.DTOs.InvoiceLineDto>(l => l.Quantity == 2.5m && l.UnitPrice == 40m && l.Unit == "h");
        lines[1].UnitPrice.Should().Be(45m, "the sale price is used when set");
        lines[2].UnitPrice.Should().Be(0.4m, "falls back to cost when no sale price was entered");
        db.Invoices.Single().InterventionId.Should().Be(job.Id);
    }

    [Fact]
    public async Task Invoice_from_job_twice_is_refused()
    {
        using var db = TestDb.Create();
        var (owner, job, _) = Seed(db, InterventionStatus.Completed);
        var handler = InvoiceHandler(db, owner);

        await handler.Handle(new CreateInvoiceFromInterventionCommand(job.Id), CancellationToken.None);
        var second = await handler.Handle(new CreateInvoiceFromInterventionCommand(job.Id), CancellationToken.None);

        second.IsSuccess.Should().BeFalse();
        db.Invoices.Should().ContainSingle();
    }

    [Fact]
    public async Task Invoice_from_unfinished_job_is_refused()
    {
        using var db = TestDb.Create();
        var (owner, job, _) = Seed(db, InterventionStatus.InProgress);

        var result = await InvoiceHandler(db, owner).Handle(new CreateInvoiceFromInterventionCommand(job.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Invoice_with_hours_requires_an_hourly_rate()
    {
        using var db = TestDb.Create();
        var (owner, job, _) = Seed(db, InterventionStatus.Completed, hourlyRate: null);

        var result = await InvoiceHandler(db, owner).Handle(new CreateInvoiceFromInterventionCommand(job.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("preço/hora"));
    }
}
