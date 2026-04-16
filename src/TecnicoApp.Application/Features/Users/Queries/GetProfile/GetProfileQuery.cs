using Ardalis.Result;
using MediatR;
using TecnicoApp.Application.Features.Users.DTOs;

namespace TecnicoApp.Application.Features.Users.Queries.GetProfile;

public record GetProfileQuery : IRequest<Result<ProfileDto>>;
