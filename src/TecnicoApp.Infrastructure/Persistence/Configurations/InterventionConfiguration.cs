using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.ValueObjects;

namespace TecnicoApp.Infrastructure.Persistence.Configurations;

public class InterventionConfiguration : IEntityTypeConfiguration<Intervention>
{
    private static readonly JsonSerializerOptions JsonOpts = new();

    public void Configure(EntityTypeBuilder<Intervention> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Title).IsRequired().HasMaxLength(300);
        builder.Property(i => i.Description).HasMaxLength(5000);
        builder.Property(i => i.Status).HasConversion<string>();
        builder.Property(i => i.TechnicianNotes).HasMaxLength(5000);

        // List<string> requires explicit value converter + comparer for jsonb
        builder.Property(i => i.Photos)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOpts),
                v => JsonSerializer.Deserialize<List<string>>(v, JsonOpts) ?? new List<string>())
            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                (a, b) => a != null && b != null && a.SequenceEqual(b),
                v => v.Aggregate(0, (h, s) => HashCode.Combine(h, s.GetHashCode())),
                v => v.ToList()));

        // List<InterventionMaterial> persisted as jsonb
        builder.Property(i => i.Materials)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOpts),
                v => JsonSerializer.Deserialize<List<InterventionMaterial>>(v, JsonOpts) ?? new())
            .Metadata.SetValueComparer(new ValueComparer<List<InterventionMaterial>>(
                (a, b) => a != null && b != null && a.SequenceEqual(b),
                v => v.Aggregate(0, (h, m) => HashCode.Combine(h, m.GetHashCode())),
                v => v.ToList()));

        builder.Property(i => i.AssignedToUserId);

        builder.HasIndex(i => new { i.UserId, i.ScheduledAt });
        builder.HasQueryFilter(i => !i.IsDeleted);

        builder.HasOne(i => i.Client)
               .WithMany(c => c.Interventions)
               .HasForeignKey(i => i.ClientId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.Equipment)
               .WithMany(e => e.Interventions)
               .UsingEntity(j => j.ToTable("InterventionEquipment"));
    }
}
