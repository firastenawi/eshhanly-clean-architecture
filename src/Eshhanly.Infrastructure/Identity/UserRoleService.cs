using Eshhanly.Application.Common.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Eshhanly.Infrastructure.Identity;

internal sealed class UserRoleService(UserManager<ApplicationUser> userManager) : IUserRoleService
{
    public async Task AddToRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException($"User {userId} not found.");

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }
    }
}
