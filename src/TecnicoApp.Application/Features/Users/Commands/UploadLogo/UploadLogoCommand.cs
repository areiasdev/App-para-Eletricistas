using Ardalis.Result;
using MediatR;
using TecnicoApp.Application.Features.Users.DTOs;

namespace TecnicoApp.Application.Features.Users.Commands.UploadLogo;

// Application layer stays free of ASP.NET Core's IFormFile — the controller extracts the
// raw bytes/content-type before dispatching this command.
public record UploadLogoCommand(
    byte[] FileContent,
    string ContentType
) : IRequest<Result<ProfileDto>>;
