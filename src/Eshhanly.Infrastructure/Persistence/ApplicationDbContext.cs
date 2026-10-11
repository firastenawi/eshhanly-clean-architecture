using Eshhanly.Application.Common.Exceptions;
using Eshhanly.Application.Common.Interfaces;
using Eshhanly.Domain.Billing;
using Eshhanly.Domain.Bookings;
using Eshhanly.Domain.Charging;
using Eshhanly.Domain.Common;
using Eshhanly.Domain.Hosts;
using Eshhanly.Domain.Platform;
using Eshhanly.Domain.Reviews;
using Eshhanly.Domain.Stations;
using Eshhanly.Domain.Vehicles;
using Eshhanly.Infrastructure.Identity;
using Eshhanly.Infrastructure.Persistence.Configurations;
using Eshhanly.Infrastructure.Persistence.Outbox;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Eshhanly.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options), IApplicationDbContext, IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    public DbSet<HostProfile> HostProfiles => Set<HostProfile>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Station> Stations => Set<Station>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<ChargingSession> ChargingSessions => Set<ChargingSession>();
    public DbSet<MeterValue> MeterValues => Set<MeterValue>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<Payout> Payouts => Set<Payout>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<PlatformSetting> PlatformSettings => Set<PlatformSetting>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.ExclusionViolation,
            ConstraintName: BookingConfiguration.NoOverlapConstraint,
        })
        {
            throw new SlotUnavailableException("The connector is already booked for an overlapping slot.", ex);
        }
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        base.OnModelCreating(builder);

        // postgis: radius search. btree_gist: booking overlap exclusion constraint (uuid + range in one GiST index).
        builder.HasPostgresExtension("postgis");
        builder.HasPostgresExtension("btree_gist");

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        ApplyDomainConventions(builder);
    }

    /// <summary>Rules shared by every domain entity, so individual configurations don't repeat them.</summary>
    private static void ApplyDomainConventions(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes().Where(t => !t.IsOwned()))
        {
            var clrType = entityType.ClrType;

            if (typeof(Entity).IsAssignableFrom(clrType))
            {
                // Ids are generated client-side (UUIDv7). ValueGeneratedNever makes EF treat new children
                // discovered through a navigation as Added rather than Modified.
                builder.Entity(clrType).Property(nameof(Entity.Id)).ValueGeneratedNever();
            }

            if (typeof(AggregateRoot).IsAssignableFrom(clrType))
            {
                builder.Entity(clrType).Ignore(nameof(AggregateRoot.DomainEvents));

                // Optimistic concurrency on PostgreSQL's system column xmin (no extra column is created).
                builder.Entity(clrType)
                    .Property<uint>("Version")
                    .IsRowVersion()
                    .HasColumnName("xmin")
                    .HasColumnType("xid");
            }
        }
    }
}
