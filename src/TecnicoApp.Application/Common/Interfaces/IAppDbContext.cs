using Microsoft.EntityFrameworkCore;
using TecnicoApp.Domain.Entities;

namespace TecnicoApp.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Client> Clients { get; }
    DbSet<Quote> Quotes { get; }
    DbSet<QuoteLine> QuoteLines { get; }
    DbSet<Equipment> Equipment { get; }
    DbSet<Intervention> Interventions { get; }
    DbSet<TeamMember> TeamMembers { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
