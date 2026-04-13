namespace Aerarium.Api.Services;

using System.Security.Claims;
using Aerarium.Application.Common;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public string UserId =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("User is not authenticated.");
}
