using System.Security.Claims;
using Eshhanly.Application.Common.Interfaces;

namespace Eshhanly.Web.Services;

/// <summary>Reads the signed-in user from the HTTP context (cookie or JWT — wired in Step 5).</summary>
internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public bool IsInRole(string role) => Principal?.IsInRole(role) == true;
}
