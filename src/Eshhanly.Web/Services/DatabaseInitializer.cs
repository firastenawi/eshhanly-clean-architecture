using Eshhanly.Application.Common.Security;
using Eshhanly.Infrastructure.Identity;
using Eshhanly.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Eshhanly.Web.Services;

internal static class DatabaseInitializer
{
    /// <summary>
    /// Optional startup tasks (both off unless configured):
    /// <c>Database:MigrateOnStartup</c> applies pending migrations (fine for a single instance like Render free tier);
    /// <c>Admin:Email</c> grants the Admin role to that already-registered user.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;

        if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
        {
            await services.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
        }

        if (app.Configuration["Admin:Email"] is { Length: > 0 } adminEmail)
        {
            var users = services.GetRequiredService<UserManager<ApplicationUser>>();
            if (await users.FindByEmailAsync(adminEmail) is { } admin && !await users.IsInRoleAsync(admin, Roles.Admin))
            {
                await users.AddToRoleAsync(admin, Roles.Admin);
            }
        }
    }
}
