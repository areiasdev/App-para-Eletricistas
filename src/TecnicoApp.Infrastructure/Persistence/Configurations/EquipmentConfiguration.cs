using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TecnicoApp.Domain.Entities;

namespace TecnicoApp.Infrastructure.Persistence.Configurations;

public class EquipmentConfiguration : IEntityTypeConfiguration<Equipment>
{
    public void Configure(EntityTypeBuilder<Equipment> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Type).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Brand).HasMaxLength(100);
        builder.Property(e => e.Model).HasMaxLength(100);
        builder.Property(e => e.SerialNumber).HasMaxLength(100);
        builder.Property(e => e.Notes).HasMaxLength(2000);
        builder.Property(e => e.Photos).HasColumnType("jsonb");

        builder.HasIndex(e => new { e.ClientId, e.NextMaintenance });
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasOne(e => e.Client)
               .WithMany(c => c.Equipment)
               .HasForeignKey(e => e.ClientId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
