using FluentAssertions;
using TecnicoApp.Application.Features.Interventions.Commands.UpdateIntervention;
using Xunit;

namespace TecnicoApp.UnitTests.Validators;

public class UpdateInterventionCommandValidatorTests
{
    private readonly UpdateInterventionCommandValidator _validator = new();

    private static UpdateInterventionCommand Valid() =>
        new(Guid.NewGuid(), "Revisão anual", null, null, null, null, [], null, null, null);

    [Fact]
    public void Valid_minimal_command_passes()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_id_fails()
    {
        var result = _validator.Validate(Valid() with { Id = Guid.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateInterventionCommand.Id));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_title_fails(string title)
    {
        var result = _validator.Validate(Valid() with { Title = title });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateInterventionCommand.Title));
    }

    [Fact]
    public void Title_over_300_chars_fails()
    {
        var result = _validator.Validate(Valid() with { Title = new string('a', 301) });
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Http_photo_url_fails()
    {
        var result = _validator.Validate(Valid() with { Photos = ["http://example.com/foto.jpg"] });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("HTTPS"));
    }

    [Fact]
    public void More_than_20_photos_fails()
    {
        var photos = Enumerable.Range(1, 21)
            .Select(i => $"https://example.com/foto{i}.jpg")
            .ToList();
        var result = _validator.Validate(Valid() with { Photos = photos });
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void TechnicianNotes_over_5000_chars_fails()
    {
        var result = _validator.Validate(Valid() with { TechnicianNotes = new string('x', 5001) });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateInterventionCommand.TechnicianNotes));
    }

    [Fact]
    public void Duplicate_photo_urls_fail()
    {
        var photos = new List<string>
        {
            "https://example.com/foto.jpg",
            "https://example.com/foto.jpg"
        };
        var result = _validator.Validate(Valid() with { Photos = photos });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("duplicadas"));
    }

    [Fact]
    public void Unique_photo_urls_pass()
    {
        var photos = new List<string>
        {
            "https://example.com/foto1.jpg",
            "https://example.com/foto2.jpg"
        };
        _validator.Validate(Valid() with { Photos = photos }).IsValid.Should().BeTrue();
    }
}
