using Ardalis.Result;
using MediatR;

namespace TecnicoApp.Application.Features.Auth.Commands.ForgotPassword;

public record ForgotPasswordCommand(string Email) : IRequest<Result>;
