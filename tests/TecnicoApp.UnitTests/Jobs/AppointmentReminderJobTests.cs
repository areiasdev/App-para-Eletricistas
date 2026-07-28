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

public class AppointmentReminderJobTests
{
    private static User NewOwner() => new()
    {
        Email = $"owner-{Guid.NewGuid()}@x.pt", PasswordHash = "h", FullName = "Owner", Role = UserRole.Owner
    };

    private static Client NewClient(Guid ownerId, bool whatsAppOptIn = true, bool phoneVerified = true, string? phone = "912345678") => new()
    {
        Name = "Cliente", Phone = phone, UserId = ownerId, WhatsAppOptIn = whatsAppOptIn, PhoneVerified = phoneVerified
    };

    [Fact]
    public async Task RunAsync_intervention_scheduled_for_tomorrow_sends_whatsapp_reminder()
    {
        using var db = TestDb.Create();
        var owner = NewOwner();
        var client = NewClient(owner.Id);
        var intervention = new Intervention
        {
            Title = "Reparação de quadro elétrico", ClientId = client.Id, UserId = owner.Id,
            Status = InterventionStatus.Scheduled, ScheduledAt = DateTime.UtcNow.Date.AddDays(1).AddHours(10),
        };
        db.Users.Add(owner);
        db.Clients.Add(client);
        db.Interventions.Add(intervention);
        await db.SaveChangesAsync(CancellationToken.None);

        var notificationService = Substitute.For<INotificationService>();
        var logger = Substitute.For<ILogger<AppointmentReminderJob>>();
        var job = new AppointmentReminderJob(db, notificationService, logger);

        await job.RunAsync();

        await notificationService.Received(1).SendWhatsAppAsync(
            "912345678", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_intervention_scheduled_far_outside_window_is_skipped()
    {
        using var db = TestDb.Create();
        var owner = NewOwner();
        var client = NewClient(owner.Id);
        var intervention = new Intervention
        {
            Title = "Manutenção geral", ClientId = client.Id, UserId = owner.Id,
            Status = InterventionStatus.Scheduled, ScheduledAt = DateTime.UtcNow.Date.AddDays(7),
        };
        db.Users.Add(owner);
        db.Clients.Add(client);
        db.Interventions.Add(intervention);
        await db.SaveChangesAsync(CancellationToken.None);

        var notificationService = Substitute.For<INotificationService>();
        var logger = Substitute.For<ILogger<AppointmentReminderJob>>();
        var job = new AppointmentReminderJob(db, notificationService, logger);

        await job.RunAsync();

        await notificationService.DidNotReceive().SendWhatsAppAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_non_scheduled_intervention_is_skipped()
    {
        using var db = TestDb.Create();
        var owner = NewOwner();
        var client = NewClient(owner.Id);
        var intervention = new Intervention
        {
            Title = "Já concluída", ClientId = client.Id, UserId = owner.Id,
            Status = InterventionStatus.Completed, ScheduledAt = DateTime.UtcNow.Date.AddDays(1).AddHours(10),
        };
        db.Users.Add(owner);
        db.Clients.Add(client);
        db.Interventions.Add(intervention);
        await db.SaveChangesAsync(CancellationToken.None);

        var notificationService = Substitute.For<INotificationService>();
        var logger = Substitute.For<ILogger<AppointmentReminderJob>>();
        var job = new AppointmentReminderJob(db, notificationService, logger);

        await job.RunAsync();

        await notificationService.DidNotReceive().SendWhatsAppAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_opted_out_client_is_skipped()
    {
        using var db = TestDb.Create();
        var owner = NewOwner();
        var client = NewClient(owner.Id, whatsAppOptIn: false);
        var intervention = new Intervention
        {
            Title = "Instalação", ClientId = client.Id, UserId = owner.Id,
            Status = InterventionStatus.Scheduled, ScheduledAt = DateTime.UtcNow.Date.AddDays(1).AddHours(10),
        };
        db.Users.Add(owner);
        db.Clients.Add(client);
        db.Interventions.Add(intervention);
        await db.SaveChangesAsync(CancellationToken.None);

        var notificationService = Substitute.For<INotificationService>();
        var logger = Substitute.For<ILogger<AppointmentReminderJob>>();
        var job = new AppointmentReminderJob(db, notificationService, logger);

        await job.RunAsync();

        await notificationService.DidNotReceive().SendWhatsAppAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_one_failing_send_does_not_stop_the_batch()
    {
        using var db = TestDb.Create();
        var owner = NewOwner();
        var failingClient = NewClient(owner.Id, phone: "911111111");
        var okClient = NewClient(owner.Id, phone: "922222222");
        var failingIntervention = new Intervention
        {
            Title = "Intervenção A", ClientId = failingClient.Id, UserId = owner.Id,
            Status = InterventionStatus.Scheduled, ScheduledAt = DateTime.UtcNow.Date.AddDays(1).AddHours(9),
        };
        var okIntervention = new Intervention
        {
            Title = "Intervenção B", ClientId = okClient.Id, UserId = owner.Id,
            Status = InterventionStatus.Scheduled, ScheduledAt = DateTime.UtcNow.Date.AddDays(1).AddHours(15),
        };
        db.Users.Add(owner);
        db.Clients.AddRange(failingClient, okClient);
        db.Interventions.AddRange(failingIntervention, okIntervention);
        await db.SaveChangesAsync(CancellationToken.None);

        var notificationService = Substitute.For<INotificationService>();
        notificationService
            .SendWhatsAppAsync("911111111", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new Exception("Twilio down")));
        var logger = Substitute.For<ILogger<AppointmentReminderJob>>();
        var job = new AppointmentReminderJob(db, notificationService, logger);

        await job.RunAsync();

        await notificationService.Received(1).SendWhatsAppAsync(
            "922222222", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
