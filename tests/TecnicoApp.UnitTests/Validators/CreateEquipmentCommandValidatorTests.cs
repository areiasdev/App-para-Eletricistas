using FluentAssertions;
using TecnicoApp.Application.Features.Equipment.Commands.CreateEquipment;
using Xunit;

namespace TecnicoApp.UnitTests.Validators;

public class CreateEquipmentCommandValidatorTests
{
    private readonly CreateEquipmentCommandValidator _validator = new();

    private static CreateEquipmentCommand Valid() =>
        new(Guid.NewGuid(), "Ar condicionado", null, null, null, null, null, null, null);

    [Fact]
    public void Valid_minimal_command_passes()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_clientId_fails()
    {
        var result = _validator.Validate(Valid() with { ClientId = Guid.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateEquipmentCommand.ClientId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_type_fails(string type)
    {
        var result = _validator.Validate(Valid() with { Type = type });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateEquipmentCommand.Type));
    }

    [Fact]
    public void Type_over_100_chars_fails()
    {
        var result = _validator.Validate(Valid() with { Type = new string('x', 101) });
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Http_photo_url_fails()
    {
        var photos = new List<string> { "http://example.com/foto.jpg" };
        var result = _validator.Validate(Valid() with { Photos = photos });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("HTTPS"));
    }

    [Fact]
    public void Https_photo_url_passes()
    {
        var photos = new List<string> { "https://example.com/foto.jpg" };
        var result = _validator.Validate(Valid() with { Photos = photos });
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Localhost_photo_url_passes()
    {
        var photos = new List<string> { "http://localhost/foto.jpg" };
        var result = _validator.Validate(Valid() with { Photos = photos });
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

    [Fact]
    public void Exactly_20_photos_passes()
    {
        var photos = Enumerable.Range(1, 20)
            .Select(i => $"https://example.com/foto{i}.jpg")
            .ToList();
        var result = _validator.Validate(Valid() with { Photos = photos });
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Null_photos_passes()
    {
        var result = _validator.Validate(Valid() with { Photos = null });
        result.IsValid.Should().BeTrue();
    }
}
