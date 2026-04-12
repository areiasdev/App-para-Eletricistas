using Ardalis.Result;
using MediatR;
using TecnicoApp.Application.Features.Auth.DTOs;

namespace TecnicoApp.Application.Features.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string RefreshToken) : IRequest<Result<AuthResponseDto>>;
