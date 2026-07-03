using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TecnicoApp.Domain.Entities;

namespace TecnicoApp.Infrastructure.Persistence.Configurations;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Nif).HasMaxLength(9);
        builder.Property(c => c.Email).HasMaxLength(256);
        builder.Property(c => c.Phone).HasMaxLength(20);
        builder.Property(c => c.Notes).HasMaxLength(2000);

        builder.OwnsOne(c => c.Address, a =>
        {
            a.Property(x => x.Street).HasMaxLength(300);
            a.Property(x => x.City).HasMaxLength(100);
            a.Property(x => x.PostalCode).HasMaxLength(8);
            a.Property(x => x.Country).HasMaxLength(100).HasDefaultValue("Portugal");
        });

        builder.Property(c => c.PortalTokenHash).HasMaxLength(128);

        builder.HasIndex(c => new { c.UserId, c.Name });
        builder.HasQueryFilter(c => !c.IsDeleted);

        builder.HasOne(c => c.User)
               .WithMany(u => u.Clients)
               .HasForeignKey(c => c.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
