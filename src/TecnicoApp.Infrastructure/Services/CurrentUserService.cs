using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TecnicoApp.Application.Common.Interfaces;

namespace TecnicoApp.Infrastructure.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid UserId
    {
        get
        {
            var value = Principal?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(value, out var id) ? id : Guid.Empty;
        }
    }

    public string Email
        => Principal?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? string.Empty;

    public bool IsAuthenticated
        => Principal?.Identity?.IsAuthenticated ?? false;
}
