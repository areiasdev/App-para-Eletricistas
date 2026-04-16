using FluentAssertions;
using TecnicoApp.Application.Features.Equipment.Commands.UpdateEquipment;
using Xunit;

namespace TecnicoApp.UnitTests.Validators;

public class UpdateEquipmentCommandValidatorTests
{
    private readonly UpdateEquipmentCommandValidator _validator = new();

    private static UpdateEquipmentCommand Valid() =>
        new(Guid.NewGuid(), "Ar condicionado", null, null, null, null, null, null, null);

    [Fact]
    public void Valid_command_passes()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_id_fails()
    {
        var result = _validator.Validate(Valid() with { Id = Guid.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateEquipmentCommand.Id));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_type_fails(string type)
    {
        var result = _validator.Validate(Valid() with { Type = type });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateEquipmentCommand.Type));
    }

    [Fact]
    public void Brand_over_100_chars_fails()
    {
        var result = _validator.Validate(Valid() with { Brand = new string('x', 101) });
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Http_photo_fails()
    {
        var result = _validator.Validate(Valid() with { Photos = ["http://example.com/foto.jpg"] });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("HTTPS"));
    }

    [Fact]
    public void Https_photo_passes()
    {
        var result = _validator.Validate(Valid() with { Photos = ["https://example.com/foto.jpg"] });
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void More_than_20_photos_fails()
    {
        var photos = Enumerable.Range(1, 21)
            .Select(i => $"https://example.com/f{i}.jpg")
            .ToList();
        var result = _validator.Validate(Valid() with { Photos = photos });
        result.IsValid.Should().BeFalse();
    }
}
