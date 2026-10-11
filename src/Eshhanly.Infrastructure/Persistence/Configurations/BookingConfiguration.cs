using Eshhanly.Domain.Bookings;
using Eshhanly.Domain.Stations;
using Eshhanly.Domain.Vehicles;
using Eshhanly.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eshhanly.Infrastructure.Persistence.Configurations;

internal sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    /// <summary>
    /// Name of the exclusion constraint (created in the InitialCreate migration) that makes two active bookings
    /// on the same connector with overlapping [start_at, blocked_until) impossible. Mapped to BookingErrors.SlotTaken.
    /// </summary>
    public const string NoOverlapConstraint = "ex_bookings_connector_no_overlap";

    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings", t =>
        {
            t.HasCheckConstraint("ck_bookings_slot", "end_at > start_at AND blocked_until >= end_at");
            t.HasCheckConstraint("ck_bookings_amounts", "estimated_total_minor >= 0 AND (final_total_minor IS NULL OR final_total_minor >= 0)");
        });

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Number).HasMaxLength(Booking.NumberLength).IsRequired();
        builder.Property(b => b.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(b => b.CancellationReason).HasMaxLength(Booking.CancellationReasonMaxLength);

        builder.OwnsOne(b => b.Quote, quote =>
        {
            quote.ToJson("quote");
            quote.OwnsMany(q => q.Lines);
        });
        builder.Navigation(b => b.Quote).IsRequired();

        builder.HasIndex(b => b.Number).IsUnique();
        builder.HasIndex(b => new { b.GuestId, b.Status });
        builder.HasIndex(b => new { b.StationId, b.StartAt });
        builder.HasIndex(b => new { b.ConnectorId, b.StartAt });
        builder.HasIndex(b => b.ExpiresAt).HasFilter("expires_at IS NOT NULL"); // expiry background job

        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(b => b.GuestId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Station>().WithMany().HasForeignKey(b => b.StationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Connector>().WithMany().HasForeignKey(b => b.ConnectorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Vehicle>().WithMany().HasForeignKey(b => b.VehicleId).OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(b => b.Slot);
    }
}
