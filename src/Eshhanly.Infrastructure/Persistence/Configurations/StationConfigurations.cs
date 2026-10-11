using Eshhanly.Domain.Hosts;
using Eshhanly.Domain.Stations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eshhanly.Infrastructure.Persistence.Configurations;

internal sealed class StationConfiguration : IEntityTypeConfiguration<Station>
{
    public void Configure(EntityTypeBuilder<Station> builder)
    {
        builder.ToTable("stations", t =>
        {
            t.HasCheckConstraint("ck_stations_booking_limits", "min_booking_minutes >= 15 AND min_booking_minutes <= max_booking_minutes AND max_booking_minutes <= 1440");
            t.HasCheckConstraint("ck_stations_buffer", "buffer_minutes BETWEEN 0 AND 120");
        });

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Title).HasMaxLength(Station.TitleMaxLength).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(Station.DescriptionMaxLength);
        builder.Property(s => s.AccessInstructions).HasMaxLength(Station.AccessInstructionsMaxLength);
        builder.Property(s => s.TimeZoneId).HasMaxLength(64).IsRequired();
        builder.Property(s => s.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(s => s.OcppChargeBoxId).HasMaxLength(64);
        builder.Property(s => s.RatingAverage).HasPrecision(3, 2);

        // geography (not geometry): ST_DWithin works in metres on the WGS84 spheroid.
        builder.Property(s => s.Location).HasColumnType($"geography (point, {Station.GeoSrid})").IsRequired();
        builder.HasIndex(s => s.Location).HasMethod("gist");

        builder.ComplexProperty(s => s.Address, address =>
        {
            address.Property(a => a.Line).HasColumnName("address_line").HasMaxLength(Address.LineMaxLength).IsRequired();
            address.Property(a => a.City).HasColumnName("address_city").HasMaxLength(Address.CityMaxLength).IsRequired();
            address.Property(a => a.Country).HasColumnName("address_country").HasMaxLength(2).IsFixedLength().IsRequired();
        });

        builder.HasIndex(s => new { s.HostId, s.Status });
        builder.HasIndex(s => s.OcppChargeBoxId).IsUnique().HasFilter("ocpp_charge_box_id IS NOT NULL");

        builder.HasOne<HostProfile>().WithMany().HasForeignKey(s => s.HostId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Connectors).WithOne().HasForeignKey(c => c.StationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(s => s.PricingRules).WithOne().HasForeignKey(r => r.StationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(s => s.AvailabilityRules).WithOne().HasForeignKey(r => r.StationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(s => s.AvailabilityOverrides).WithOne().HasForeignKey(o => o.StationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(s => s.Photos).WithOne().HasForeignKey(p => p.StationId).OnDelete(DeleteBehavior.Cascade);

        foreach (var navigation in new[] { nameof(Station.Connectors), nameof(Station.PricingRules), nameof(Station.AvailabilityRules), nameof(Station.AvailabilityOverrides), nameof(Station.Photos) })
        {
            builder.Navigation(navigation).UsePropertyAccessMode(PropertyAccessMode.Field);
        }

        builder.Ignore(s => s.IsBookable);
        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}

internal sealed class ConnectorConfiguration : IEntityTypeConfiguration<Connector>
{
    public void Configure(EntityTypeBuilder<Connector> builder)
    {
        builder.ToTable("connectors", t => t.HasCheckConstraint("ck_connectors_power", "power_kw >= 0 AND power_kw <= 400"));
        builder.HasKey(c => c.Id);
        builder.Property(c => c.PowerKw).HasPrecision(5, 1);
        builder.HasIndex(c => new { c.StationId, c.OcppConnectorId }).IsUnique();
        builder.Ignore(c => c.IsVirtual);
    }
}

internal sealed class PricingRuleConfiguration : IEntityTypeConfiguration<PricingRule>
{
    public void Configure(EntityTypeBuilder<PricingRule> builder)
    {
        builder.ToTable("pricing_rules", t =>
        {
            t.HasCheckConstraint("ck_pricing_rules_price", "unit_price_minor > 0");
            t.HasCheckConstraint("ck_pricing_rules_window", "(start_time IS NULL AND end_time IS NULL) OR (start_time < end_time)");
        });
        builder.HasKey(r => r.Id);
        builder.HasIndex(r => r.StationId);
    }
}

internal sealed class AvailabilityRuleConfiguration : IEntityTypeConfiguration<AvailabilityRule>
{
    public void Configure(EntityTypeBuilder<AvailabilityRule> builder)
    {
        builder.ToTable("availability_rules");
        builder.HasKey(r => r.Id);
        builder.HasIndex(r => new { r.StationId, r.DayOfWeek });
    }
}

internal sealed class AvailabilityOverrideConfiguration : IEntityTypeConfiguration<AvailabilityOverride>
{
    public void Configure(EntityTypeBuilder<AvailabilityOverride> builder)
    {
        builder.ToTable("availability_overrides", t => t.HasCheckConstraint("ck_availability_overrides_period", "end_at > start_at"));
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Reason).HasMaxLength(AvailabilityOverride.ReasonMaxLength);
        builder.HasIndex(o => new { o.StationId, o.StartAt });
    }
}

internal sealed class StationPhotoConfiguration : IEntityTypeConfiguration<StationPhoto>
{
    public void Configure(EntityTypeBuilder<StationPhoto> builder)
    {
        builder.ToTable("station_photos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Url).HasMaxLength(StationPhoto.UrlMaxLength).IsRequired();
        builder.HasIndex(p => p.StationId);
    }
}
