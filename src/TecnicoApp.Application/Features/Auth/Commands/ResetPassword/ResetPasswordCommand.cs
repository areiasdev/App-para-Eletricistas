using Ardalis.Result;
using MediatR;

namespace TecnicoApp.Application.Features.Auth.Commands.ResetPassword;

public record ResetPasswordCommand(string Email, string Token, string NewPassword) : IRequest<Result>;
