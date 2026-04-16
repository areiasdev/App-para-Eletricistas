using Ardalis.Result;
using MediatR;
using TecnicoApp.Application.Features.Users.DTOs;

namespace TecnicoApp.Application.Features.Users.Commands.UpdateProfile;

public record UpdateProfileCommand(
    string FullName,
    string? CompanyName,
    string? Nif,
    string? Phone,
    string? LogoUrl
) : IRequest<Result<ProfileDto>>;
