using FluentAssertions;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Users.Commands.UpdateProfile;
using TecnicoApp.Application.Features.Users.Commands.UploadLogo;
using TecnicoApp.Application.Features.Users.Queries.GetProfile;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Users;

// Company branding (name/NIF/phone/logo/brand color) belongs to the team owner, not
// whoever happens to be logged in — a technician's own row has none of this.
public class ProfileBrandingTests
{
    private static ICurrentUserService AsUser(User user)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(user.Id);
        currentUser.Email.Returns(user.Email);
        return currentUser;
    }

    [Fact]
    public async Task GetProfile_team_member_sees_owner_company_data_but_own_identity()
    {
        using var db = TestDb.Create();
        var owner = new User
        {
            Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner",
            CompanyName = "Empresa Lda", BrandColor = "#123456",
        };
        var technician = new User
        {
            Email = "tech@x.pt", PasswordHash = "h", FullName = "Técnico",
            OwnerId = owner.Id, Role = UserRole.Technician,
        };
        db.Users.AddRange(owner, technician);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetProfileQueryHandler(db, AsUser(technician));
        var result = await handler.Handle(new GetProfileQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FullName.Should().Be("Técnico", "identity fields are always the acting user's own");
        result.Value.CompanyName.Should().Be("Empresa Lda", "company fields always come from the owner");
        result.Value.BrandColor.Should().Be("#123456");
    }

    [Fact]
    public async Task UpdateProfile_technician_can_rename_self_but_not_company()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner", CompanyName = "Original Lda" };
        var technician = new User
        {
            Email = "tech@x.pt", PasswordHash = "h", FullName = "Técnico",
            OwnerId = owner.Id, Role = UserRole.Technician,
        };
        db.Users.AddRange(owner, technician);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateProfileCommandHandler(db, AsUser(technician));
        var result = await handler.Handle(
            new UpdateProfileCommand("Novo Nome", "Empresa Hackeada", null, null, "#ff0000", null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FullName.Should().Be("Novo Nome");
        result.Value.CompanyName.Should().Be("Original Lda", "a technician's company-field submission is silently ignored");
        db.Users.Single(u => u.Id == owner.Id).CompanyName.Should().Be("Original Lda");
    }

    [Fact]
    public async Task UpdateProfile_owner_can_change_company_data()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner", CompanyName = "Original Lda" };
        db.Users.Add(owner);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateProfileCommandHandler(db, AsUser(owner));
        var result = await handler.Handle(
            new UpdateProfileCommand("Owner", "Nova Empresa Lda", "123456789", "912345678", "#ff0000", null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CompanyName.Should().Be("Nova Empresa Lda");
        result.Value.BrandColor.Should().Be("#ff0000");
    }

    [Fact]
    public async Task UploadLogo_technician_is_forbidden()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var technician = new User
        {
            Email = "tech@x.pt", PasswordHash = "h", FullName = "Técnico",
            OwnerId = owner.Id, Role = UserRole.Technician,
        };
        db.Users.AddRange(owner, technician);
        await db.SaveChangesAsync(CancellationToken.None);

        var fileStorage = Substitute.For<IFileStorageService>();
        var handler = new UploadLogoCommandHandler(db, AsUser(technician), fileStorage);

        var result = await handler.Handle(new UploadLogoCommand([1, 2, 3], "image/png"), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
        await fileStorage.DidNotReceive().SaveLogoAsync(Arg.Any<Guid>(), Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadLogo_admin_saves_it_under_the_owner_id_not_their_own()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var admin = new User
        {
            Email = "admin@x.pt", PasswordHash = "h", FullName = "Admin",
            OwnerId = owner.Id, Role = UserRole.Admin,
        };
        db.Users.AddRange(owner, admin);
        await db.SaveChangesAsync(CancellationToken.None);

        var fileStorage = Substitute.For<IFileStorageService>();
        fileStorage.SaveLogoAsync(owner.Id, Arg.Any<byte[]>(), ".png", Arg.Any<CancellationToken>())
            .Returns("/uploads/logos/owner.png");

        var handler = new UploadLogoCommandHandler(db, AsUser(admin), fileStorage);
        var result = await handler.Handle(new UploadLogoCommand([1, 2, 3], "image/png"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.LogoUrl.Should().Be("/uploads/logos/owner.png");
        db.Users.Single(u => u.Id == owner.Id).LogoUrl.Should().Be("/uploads/logos/owner.png");
        await fileStorage.Received(1).SaveLogoAsync(owner.Id, Arg.Any<byte[]>(), ".png", Arg.Any<CancellationToken>());
    }
}
