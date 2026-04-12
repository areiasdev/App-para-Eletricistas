using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TecnicoApp.Domain.Entities;

namespace TecnicoApp.Infrastructure.Persistence.Configurations;

public class InterventionConfiguration : IEntityTypeConfiguration<Intervention>
{
    public void Configure(EntityTypeBuilder<Intervention> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Title).IsRequired().HasMaxLength(300);
        builder.Property(i => i.Description).HasMaxLength(5000);
        builder.Property(i => i.Status).HasConversion<string>();
        builder.Property(i => i.TechnicianNotes).HasMaxLength(5000);
        builder.Property(i => i.Photos).HasColumnType("jsonb");

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
