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
    public string? BrandColor { get; set; }
    public string? Iban { get; set; }
    public string? BankName { get; set; }

    /// <summary>Company labour rate (€/h, excl. VAT) used when invoicing a job's hours.</summary>
    public decimal? DefaultHourlyRate { get; set; }
    public string? RefreshTokenHash { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
    public string? PasswordResetTokenHash { get; set; }
    public DateTime? PasswordResetTokenExpiresAt { get; set; }

    /// <summary>
    /// null = this user IS an owner. Non-null = this user is a member of another user's account.
    /// </summary>
    public Guid? OwnerId { get; set; }
    public UserRole Role { get; set; } = UserRole.Owner;

    public ICollection<Client> Clients { get; set; } = [];
    public ICollection<Quote> Quotes { get; set; } = [];
    public ICollection<Invoice> Invoices { get; set; } = [];
}
