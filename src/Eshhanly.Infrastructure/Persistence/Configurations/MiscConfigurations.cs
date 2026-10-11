using Eshhanly.Domain.Bookings;
using Eshhanly.Domain.Platform;
using Eshhanly.Domain.Reviews;
using Eshhanly.Infrastructure.Identity;
using Eshhanly.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eshhanly.Infrastructure.Persistence.Configurations;

internal sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("reviews", t => t.HasCheckConstraint("ck_reviews_rating", "rating BETWEEN 1 AND 5"));
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Comment).HasMaxLength(Review.CommentMaxLength);
        builder.HasIndex(r => new { r.BookingId, r.AuthorId }).IsUnique();
        builder.HasIndex(r => new { r.Target, r.TargetId });
        builder.HasOne<Booking>().WithMany().HasForeignKey(r => r.BookingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(r => r.AuthorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}

internal sealed class PlatformSettingConfiguration : IEntityTypeConfiguration<PlatformSetting>
{
    public void Configure(EntityTypeBuilder<PlatformSetting> builder)
    {
        builder.ToTable("platform_settings");
        builder.HasKey(s => s.Key);
        builder.Property(s => s.Key).HasMaxLength(PlatformSetting.KeyMaxLength);
        builder.Property(s => s.Value).HasMaxLength(PlatformSetting.ValueMaxLength).IsRequired();

        builder.HasData(
            new PlatformSetting(PlatformSettingKeys.CommissionRate, "0.15"),
            new PlatformSetting(PlatformSettingKeys.DefaultCurrency, "SYP"));
    }
}

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.Type).HasMaxLength(300).IsRequired();
        builder.Property(m => m.Payload).HasColumnType("jsonb").IsRequired();
        builder.Property(m => m.Error).HasMaxLength(2000);
        builder.HasIndex(m => m.OccurredAt).HasFilter("processed_at IS NULL");
    }
}
