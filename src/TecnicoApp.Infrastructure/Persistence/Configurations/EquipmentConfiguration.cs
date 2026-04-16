using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TecnicoApp.Domain.Entities;

namespace TecnicoApp.Infrastructure.Persistence.Configurations;

public class EquipmentConfiguration : IEntityTypeConfiguration<Equipment>
{
    private static readonly JsonSerializerOptions JsonOpts = new();

    public void Configure(EntityTypeBuilder<Equipment> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Type).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Brand).HasMaxLength(100);
        builder.Property(e => e.Model).HasMaxLength(100);
        builder.Property(e => e.SerialNumber).HasMaxLength(100);
        builder.Property(e => e.Notes).HasMaxLength(2000);

        // List<string> requires explicit value converter + comparer for jsonb
        builder.Property(e => e.Photos)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOpts),
                v => JsonSerializer.Deserialize<List<string>>(v, JsonOpts) ?? new List<string>())
            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                (a, b) => a != null && b != null && a.SequenceEqual(b),
                v => v.Aggregate(0, (h, s) => HashCode.Combine(h, s.GetHashCode())),
                v => v.ToList()));

        builder.HasIndex(e => new { e.ClientId, e.NextMaintenance });
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasOne(e => e.Client)
               .WithMany(c => c.Equipment)
               .HasForeignKey(e => e.ClientId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
