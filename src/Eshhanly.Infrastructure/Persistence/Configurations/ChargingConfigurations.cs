using Eshhanly.Domain.Bookings;
using Eshhanly.Domain.Charging;
using Eshhanly.Domain.Stations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eshhanly.Infrastructure.Persistence.Configurations;

internal sealed class ChargingSessionConfiguration : IEntityTypeConfiguration<ChargingSession>
{
    public void Configure(EntityTypeBuilder<ChargingSession> builder)
    {
        builder.ToTable("charging_sessions", t =>
            t.HasCheckConstraint("ck_charging_sessions_meter", "meter_stop_wh IS NULL OR meter_stop_wh >= meter_start_wh"));

        builder.HasKey(s => s.Id);
        builder.Property(s => s.StopReason).HasMaxLength(200);
        builder.HasIndex(s => s.BookingId).IsUnique();
        builder.HasIndex(s => new { s.ConnectorId, s.Status });

        builder.HasOne<Booking>().WithMany().HasForeignKey(s => s.BookingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Connector>().WithMany().HasForeignKey(s => s.ConnectorId).OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(s => s.EnergyWh);
    }
}

internal sealed class MeterValueConfiguration : IEntityTypeConfiguration<MeterValue>
{
    public void Configure(EntityTypeBuilder<MeterValue> builder)
    {
        builder.ToTable("meter_values");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).UseIdentityAlwaysColumn();
        builder.HasIndex(m => new { m.SessionId, m.Timestamp });
        builder.HasOne<ChargingSession>().WithMany().HasForeignKey(m => m.SessionId).OnDelete(DeleteBehavior.Cascade);
    }
}
