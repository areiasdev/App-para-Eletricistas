using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Domain.Entities;

namespace TecnicoApp.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    private readonly ICurrentUserService? _currentUser;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService? currentUser = null)
        : base(options)
    {
        _currentUser = currentUser;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<QuoteLine> QuoteLines => Set<QuoteLine>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<Intervention> Interventions => Set<Intervention>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var auditableTypes = new HashSet<string>
        {
            nameof(Client), nameof(Equipment), nameof(Intervention),
            nameof(Quote), nameof(TeamMember)
        };

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.ModifiedAt = DateTime.UtcNow;

            var typeName = entry.Entity.GetType().Name;
            if (!auditableTypes.Contains(typeName)) continue;

            var action = entry.State switch
            {
                EntityState.Added    => "Created",
                EntityState.Modified => "Updated",
                EntityState.Deleted  => "Deleted",
                _                    => null
            };
            if (action is null) continue;

            string? changes = null;
            if (entry.State == EntityState.Modified)
            {
                // Exclude audit-noise fields and sensitive credential fields
                var sensitiveFields = new HashSet<string>
                {
                    "ModifiedAt", "ModifiedBy",
                    "PasswordHash", "RefreshToken", "RefreshTokenExpiresAt",
                    "PasswordResetTokenHash", "PasswordResetTokenExpiresAt",
                    "InviteTokenHash"
                };
                var modified = entry.Properties
                    .Where(p => p.IsModified && !sensitiveFields.Contains(p.Metadata.Name))
                    .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
                if (modified.Count > 0)
                    changes = JsonSerializer.Serialize(modified);
            }

            AuditLogs.Add(new AuditLog
            {
                EntityType = typeName,
                EntityId   = entry.Entity.Id.ToString(),
                Action     = action,
                UserId     = _currentUser?.UserId,
                UserEmail  = _currentUser?.Email,
                Changes    = changes,
            });
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
