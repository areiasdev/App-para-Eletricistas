using FluentAssertions;
using TecnicoApp.Application.Features.Interventions.Commands.CreateIntervention;
using Xunit;

namespace TecnicoApp.UnitTests.Validators;

public class CreateInterventionCommandValidatorTests
{
    private readonly CreateInterventionCommandValidator _validator = new();

    private static CreateInterventionCommand Valid() =>
        new("Revisão anual", null, Guid.NewGuid(), null, null, [], null, null, null);

    [Fact]
    public void Valid_minimal_command_passes()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_title_fails(string title)
    {
        var result = _validator.Validate(Valid() with { Title = title });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateInterventionCommand.Title));
    }

    [Fact]
    public void Title_over_300_chars_fails()
    {
        var result = _validator.Validate(Valid() with { Title = new string('a', 301) });
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Empty_clientId_fails()
    {
        var result = _validator.Validate(Valid() with { ClientId = Guid.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateInterventionCommand.ClientId));
    }

    [Fact]
    public void Description_over_5000_chars_fails()
    {
        var result = _validator.Validate(Valid() with { Description = new string('x', 5001) });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateInterventionCommand.Description));
    }

    [Fact]
    public void Description_exactly_5000_chars_passes()
    {
        var result = _validator.Validate(Valid() with { Description = new string('x', 5000) });
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Http_photo_url_fails()
    {
        var result = _validator.Validate(Valid() with { Photos = ["http://example.com/foto.jpg"] });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("HTTPS"));
    }

    [Fact]
    public void Https_photo_url_passes()
    {
        var result = _validator.Validate(Valid() with { Photos = ["https://example.com/foto.jpg"] });
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void More_than_20_photos_fails()
    {
        var photos = Enumerable.Range(1, 21)
            .Select(i => $"https://example.com/foto{i}.jpg")
            .ToList();
        var result = _validator.Validate(Valid() with { Photos = photos });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("20"));
    }
}
