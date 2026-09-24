using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TecnicoApp.Application.Common.Documents;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Quotes.Commands.CreateQuote;
using TecnicoApp.Application.Features.Quotes.Commands.DuplicateQuote;
using TecnicoApp.Application.Features.Uploads;
using TecnicoApp.Application.Common.Security;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.Domain.ValueObjects;
using TecnicoApp.Infrastructure.Services;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Quotes;

public class QuoteImprovementsTests
{
    [Fact]
    public async Task Lines_keep_entry_order_and_unit()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "o@x.pt", PasswordHash = "h", FullName = "O" };
        var client = new Client { Name = "C", UserId = owner.Id };
        db.Users.Add(owner); db.Clients.Add(client);
        await db.SaveChangesAsync(CancellationToken.None);
        var cu = Substitute.For<ICurrentUserService>(); cu.UserId.Returns(owner.Id);

        var result = await new CreateQuoteCommandHandler(db, cu, Substitute.For<ILogger<CreateQuoteCommandHandler>>())
            .Handle(new CreateQuoteCommand(client.Id, null, null, null,
            [
                new CreateQuoteLineRequest("Cabo", 25, 0.45m, 23m, "m"),
                new CreateQuoteLineRequest("Mão de obra", 3, 35m, 23m, "h"),
                new CreateQuoteLineRequest("Tomada", 4, 6m),
            ]), CancellationToken.None);

        result.Value.Lines.Select(l => (l.Description, l.Unit)).Should().Equal(
            ("Cabo", "m"), ("Mão de obra", "h"), ("Tomada", "un"));
    }

    [Fact]
    public async Task Duplicate_creates_a_new_draft_with_same_lines_and_new_number()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "o@x.pt", PasswordHash = "h", FullName = "O" };
        var client = new Client { Name = "C", UserId = owner.Id };
        var year = DateTime.UtcNow.Year;
        var source = new Quote
        {
            Number = $"ORC-{year}-0001", ClientId = client.Id, UserId = owner.Id, Status = QuoteStatus.Rejected,
            Lines = [new QuoteLine { Description = "Quadro", Quantity = 1, UnitPrice = 300, Unit = "vg" }],
        };
        db.Users.Add(owner); db.Clients.Add(client); db.Quotes.Add(source);
        await db.SaveChangesAsync(CancellationToken.None);
        var cu = Substitute.For<ICurrentUserService>(); cu.UserId.Returns(owner.Id);

        var result = await new DuplicateQuoteCommandHandler(db, cu, Substitute.For<ILogger<DuplicateQuoteCommandHandler>>())
            .Handle(new DuplicateQuoteCommand(source.Id), CancellationToken.None);

        result.Value.Number.Should().Be($"ORC-{year}-0002");
        result.Value.Status.Should().Be(QuoteStatus.Draft);
        result.Value.Lines.Should().ContainSingle(l => l.Description == "Quadro" && l.Unit == "vg");
        db.Quotes.Count().Should().Be(2);
    }

    [Fact]
    public void Vat_breakdown_groups_by_rate_and_adds_up_to_the_vat_total()
    {
        var lines = new[]
        {
            new QuoteLine { Description = "Material", Quantity = 1, UnitPrice = 100, VatRate = 23 },
            new QuoteLine { Description = "Obra", Quantity = 2, UnitPrice = 50, VatRate = 6 },
            new QuoteLine { Description = "Outro", Quantity = 1, UnitPrice = 10, VatRate = 23 },
        };

        var breakdown = TecnicoApp.Domain.Common.DocumentMath.VatBreakdown(lines);

        breakdown.Should().Equal((23m, 110m, 25.30m), (6m, 100m, 6m));
        breakdown.Sum(b => b.Vat).Should().Be(TecnicoApp.Domain.Common.DocumentMath.VatTotal(lines));
    }

    [Fact]
    public void Address_is_formatted_for_documents()
    {
        PdfDataFactory.FormatAddress(new Address("Rua das Flores 12", "Porto", "4000-123")).Should().Be("Rua das Flores 12, 4000-123 Porto");
        PdfDataFactory.FormatAddress(null).Should().BeNull();
    }

    [Theory]
    [InlineData("/uploads/photos/abc/def.jpg", true)]
    [InlineData("https://exemplo.pt/foto.jpg", true)]
    [InlineData("/uploads/photos/../../appsettings.json", false)]
    [InlineData("http://exemplo.pt/foto.jpg", false)]
    [InlineData("/etc/passwd", false)]
    public void Photo_urls_are_uploads_or_https(string url, bool valid) =>
        ImageFiles.IsValidPhotoUrl(url).Should().Be(valid);

    [Fact]
    public void Photo_upload_rejects_files_that_are_not_really_images()
    {
        var validator = new UploadPhotoCommandValidator();
        validator.Validate(new UploadPhotoCommand("<script>"u8.ToArray(), "image/jpeg")).IsValid.Should().BeFalse();
        validator.Validate(new UploadPhotoCommand([0xFF, 0xD8, 0xFF, 0xE0], "image/jpeg")).IsValid.Should().BeTrue();
    }
}
