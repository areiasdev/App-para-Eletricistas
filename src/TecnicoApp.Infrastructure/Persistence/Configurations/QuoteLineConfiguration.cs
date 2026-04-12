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
        builder.Property(l => l.UnitPrice).HasColumnType("decimal(10,2)");
        builder.Property(l => l.VatRate).HasColumnType("decimal(5,2)");
        builder.HasQueryFilter(l => !l.IsDeleted);
    }
}
