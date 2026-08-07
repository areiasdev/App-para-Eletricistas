using FluentAssertions;
using TecnicoApp.Application.Features.Users.Commands.UploadLogo;
using Xunit;

namespace TecnicoApp.UnitTests.Validators;

public class UploadLogoCommandValidatorTests
{
    private readonly UploadLogoCommandValidator _validator = new();

    private static readonly byte[] RealPng = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0];
    private static readonly byte[] RealJpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0, 0];
    private static readonly byte[] RealWebp = [(byte)'R', (byte)'I', (byte)'F', (byte)'F', 0, 0, 0, 0, (byte)'W', (byte)'E', (byte)'B', (byte)'P'];

    [Theory]
    [InlineData("image/png")]
    [InlineData("image/jpeg")]
    [InlineData("image/webp")]
    public void Real_image_bytes_matching_the_claimed_type_pass(string contentType)
    {
        var content = contentType switch
        {
            "image/png" => RealPng,
            "image/jpeg" => RealJpeg,
            _ => RealWebp,
        };
        _validator.Validate(new UploadLogoCommand(content, contentType)).IsValid.Should().BeTrue();
    }

    // Regression: the Content-Type header is attacker-controlled (a multipart form field, not
    // verified by the browser) — a naive allowlist-only check would accept any bytes as long
    // as the header claims to be an image, letting a malformed/non-image file reach the
    // PDF-generation image decoder later.
    [Fact]
    public void Non_image_bytes_claiming_to_be_png_are_rejected()
    {
        byte[] notAnImage = [1, 2, 3, 4, 5, 6, 7, 8];
        var result = _validator.Validate(new UploadLogoCommand(notAnImage, "image/png"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void A_jpeg_mislabeled_as_png_is_rejected()
    {
        var result = _validator.Validate(new UploadLogoCommand(RealJpeg, "image/png"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Empty_content_is_rejected_before_the_signature_check_runs()
    {
        var result = _validator.Validate(new UploadLogoCommand([], "image/png"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Oversized_content_is_rejected()
    {
        var big = new byte[2 * 1024 * 1024 + 1];
        RealPng.CopyTo(big, 0);
        var result = _validator.Validate(new UploadLogoCommand(big, "image/png"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Disallowed_content_type_is_rejected()
    {
        var result = _validator.Validate(new UploadLogoCommand(RealPng, "image/svg+xml"));
        result.IsValid.Should().BeFalse();
    }
}
