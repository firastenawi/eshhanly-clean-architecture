using Eshhanly.Application.Common.Interfaces;
using Eshhanly.Infrastructure.BackgroundJobs;
using Eshhanly.Infrastructure.Identity;
using Eshhanly.Infrastructure.Persistence;
using Eshhanly.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Eshhanly.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'Database' is not configured.");

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<AuditableEntityInterceptor>();
        services.AddSingleton<DomainEventsToOutboxInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) => options
            .UseNpgsql(connectionString, npgsql =>
            {
                npgsql.UseNetTopologySuite();
                npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgsql.MigrationsHistoryTable("__ef_migrations_history");
                npgsql.EnableRetryOnFailure();
                npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery); // aggregates load several child collections
            })
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(
                sp.GetRequiredService<AuditableEntityInterceptor>(),
                sp.GetRequiredService<DomainEventsToOutboxInterceptor>()));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // Identity API endpoints = Identity + bearer tokens (mobile/API) + cookies (Blazor web), incl. refresh & 2FA.
        services.AddIdentityApiEndpoints<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.SignIn.RequireConfirmedAccount = false; // turned on with email/SMS confirmation in Step 4
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        // Keys that sign tokens/cookies live in the database, so restarts and redeploys don't log everyone out.
        services.AddDataProtection()
            .SetApplicationName("Eshhanly")
            .PersistKeysToDbContext<ApplicationDbContext>();

        services.AddScoped<IUserRoleService, UserRoleService>();
        services.AddHostedService<BookingTimeoutsWorker>();

        services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>("database");

        return services;
    }
}
