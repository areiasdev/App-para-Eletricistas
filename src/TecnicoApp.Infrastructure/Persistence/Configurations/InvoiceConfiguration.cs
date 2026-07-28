using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TecnicoApp.Domain.Entities;

namespace TecnicoApp.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Number).IsRequired().HasMaxLength(20);
        builder.Property(i => i.Status).HasConversion<string>();
        builder.Property(i => i.Discount).HasColumnType("decimal(10,2)");
        builder.Property(i => i.Notes).HasMaxLength(2000);
        builder.Property(i => i.PayTokenHash).HasMaxLength(64);

        builder.HasMany(i => i.Lines)
               .WithOne(l => l.Invoice)
               .HasForeignKey(l => l.InvoiceId)
               .OnDelete(DeleteBehavior.Cascade);

        // Nullable FK — invoices may (in a future phase) exist without a source quote.
        // A quote may only ever have one non-cancelled invoice, but that uniqueness rule is
        // enforced in the command handler, not the schema, since Cancelled invoices must
        // still be able to coexist with a fresh invoice for the same quote.
        builder.HasOne(i => i.Quote)
               .WithMany()
               .HasForeignKey(i => i.QuoteId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(i => new { i.UserId, i.CreatedAt });
        builder.HasIndex(i => i.Number).IsUnique();
        builder.HasIndex(i => i.QuoteId);
        // Looked up by the Stripe webhook to find the invoice a completed Checkout Session belongs to.
        builder.HasIndex(i => i.StripeCheckoutSessionId);
        builder.HasQueryFilter(i => !i.IsDeleted);

        // Propriedades calculadas — não persistidas
        builder.Ignore(i => i.SubTotal);
        builder.Ignore(i => i.VatTotal);
        builder.Ignore(i => i.Total);
    }
}
