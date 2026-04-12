using Ardalis.Result;
using MediatR;
using TecnicoApp.Application.Features.Auth.DTOs;

namespace TecnicoApp.Application.Features.Auth.Commands.Login;

public record LoginCommand(
    string Email,
    string Password
) : IRequest<Result<AuthResponseDto>>;
