using Eshhanly.Domain.Hosts;
using Eshhanly.Domain.Vehicles;
using Eshhanly.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eshhanly.Infrastructure.Persistence.Configurations;

internal sealed class HostProfileConfiguration : IEntityTypeConfiguration<HostProfile>
{
    public void Configure(EntityTypeBuilder<HostProfile> builder)
    {
        builder.ToTable("host_profiles", t =>
            t.HasCheckConstraint("ck_host_profiles_commission", "commission_rate_override IS NULL OR commission_rate_override BETWEEN 0 AND 1"));

        builder.HasKey(h => h.Id);
        builder.Property(h => h.DisplayName).HasMaxLength(HostProfile.DisplayNameMaxLength).IsRequired();
        builder.Property(h => h.PayoutReference).HasMaxLength(500);
        builder.Property(h => h.CommissionRateOverride).HasPrecision(5, 4);
        builder.Property(h => h.RatingAverage).HasPrecision(3, 2);

        builder.HasIndex(h => h.UserId).IsUnique();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(h => h.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("vehicles");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Make).HasMaxLength(50).IsRequired();
        builder.Property(v => v.Model).HasMaxLength(50).IsRequired();
        builder.Property(v => v.PlateNumber).HasMaxLength(20);
        builder.Property(v => v.BatteryKwh).HasPrecision(6, 1);
        builder.Property(v => v.MaxAcKw).HasPrecision(6, 1);
        builder.Property(v => v.MaxDcKw).HasPrecision(6, 1);

        builder.HasIndex(v => v.OwnerId);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(v => v.OwnerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(v => !v.IsDeleted);
    }
}
