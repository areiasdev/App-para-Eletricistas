using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Domain.Entities;

public class User : BaseEntity
{
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string FullName { get; set; }
    public string? CompanyName { get; set; }
    public string? Nif { get; set; }
    public string? Phone { get; set; }
    public string? LogoUrl { get; set; }
    public Plan Plan { get; set; } = Plan.Free;
    public string? StripeCustomerId { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }

    public ICollection<Client> Clients { get; set; } = [];
    public ICollection<Quote> Quotes { get; set; } = [];
}
