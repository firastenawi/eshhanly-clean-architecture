using Eshhanly.Domain.Billing;
using Eshhanly.Domain.Bookings;
using Eshhanly.Domain.Charging;
using Eshhanly.Domain.Hosts;
using Eshhanly.Domain.Platform;
using Eshhanly.Domain.Reviews;
using Eshhanly.Domain.Stations;
using Eshhanly.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace Eshhanly.Application.Common.Interfaces;

/// <summary>Unit of work exposed to use cases. Child entities (connectors, pricing rules, ...) are reached through their aggregate.</summary>
public interface IApplicationDbContext
{
    DbSet<HostProfile> HostProfiles { get; }
    DbSet<Vehicle> Vehicles { get; }
    DbSet<Station> Stations { get; }
    DbSet<Booking> Bookings { get; }
    DbSet<ChargingSession> ChargingSessions { get; }
    DbSet<MeterValue> MeterValues { get; }
    DbSet<Payment> Payments { get; }
    DbSet<LedgerEntry> LedgerEntries { get; }
    DbSet<Payout> Payouts { get; }
    DbSet<Review> Reviews { get; }
    DbSet<PlatformSetting> PlatformSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
