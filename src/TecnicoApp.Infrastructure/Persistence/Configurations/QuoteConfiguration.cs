using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TecnicoApp.Domain.Entities;

namespace TecnicoApp.Infrastructure.Persistence.Configurations;

public class QuoteConfiguration : IEntityTypeConfiguration<Quote>
{
    public void Configure(EntityTypeBuilder<Quote> builder)
    {
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Number).IsRequired().HasMaxLength(20);
        builder.Property(q => q.Status).HasConversion<string>();
        builder.Property(q => q.Discount).HasColumnType("decimal(10,2)");
        builder.Property(q => q.Notes).HasMaxLength(2000);

        builder.HasMany(q => q.Lines)
               .WithOne(l => l.Quote)
               .HasForeignKey(l => l.QuoteId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(q => new { q.UserId, q.CreatedAt });
        builder.HasIndex(q => q.Number).IsUnique();
        builder.HasQueryFilter(q => !q.IsDeleted);

        // Propriedades calculadas — não persistidas
        builder.Ignore(q => q.SubTotal);
        builder.Ignore(q => q.VatTotal);
        builder.Ignore(q => q.Total);
    }
}
