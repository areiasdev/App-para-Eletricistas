using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TecnicoApp.Domain.Entities;

namespace TecnicoApp.Infrastructure.Persistence.Configurations;

public class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
{
    public void Configure(EntityTypeBuilder<TeamMember> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Role).HasConversion<string>();
        builder.Property(t => t.InviteEmail).IsRequired().HasMaxLength(256);
        builder.HasIndex(t => new { t.OwnerId, t.InviteEmail }).IsUnique();
        builder.HasQueryFilter(t => !t.IsDeleted);

        builder.HasOne(t => t.Owner)
               .WithMany()
               .HasForeignKey(t => t.OwnerId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Member)
               .WithMany()
               .HasForeignKey(t => t.MemberId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
