using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TecnicoApp.Domain.Entities;

namespace TecnicoApp.Infrastructure.Persistence.Configurations;

public class QuoteLineConfiguration : IEntityTypeConfiguration<QuoteLine>
{
    public void Configure(EntityTypeBuilder<QuoteLine> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Description).IsRequired().HasMaxLength(500);
        builder.Property(l => l.Quantity).HasColumnType("decimal(10,3)");
        // 4 decimals: trade price lists quote per metre/unit below the cent (e.g. cable at 0,4575 €/m).
        builder.Property(l => l.UnitPrice).HasColumnType("decimal(12,4)");
        builder.Property(l => l.VatRate).HasColumnType("decimal(5,2)");
        builder.Property(l => l.Unit).IsRequired().HasMaxLength(10).HasDefaultValue("un");
        builder.HasQueryFilter(l => !l.IsDeleted);
    }
}
