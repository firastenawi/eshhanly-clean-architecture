using Eshhanly.Domain.Billing;
using Eshhanly.Domain.Bookings;
using Eshhanly.Domain.Hosts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eshhanly.Infrastructure.Persistence.Configurations;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments", t => t.HasCheckConstraint(
            "ck_payments_amounts",
            "amount_held_minor >= 0 AND amount_captured_minor >= 0 AND amount_refunded_minor BETWEEN 0 AND amount_captured_minor"));

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(p => p.ProviderPaymentId).HasMaxLength(100);
        builder.Property(p => p.FailureReason).HasMaxLength(500);

        builder.HasIndex(p => p.BookingId).IsUnique();
        builder.HasIndex(p => p.HoldExpiresAt).HasFilter("hold_expires_at IS NOT NULL");
        builder.HasOne<Booking>().WithMany().HasForeignKey(p => p.BookingId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Transactions).WithOne().HasForeignKey(t => t.PaymentId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Transactions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("payment_transactions");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.IdempotencyKey).HasMaxLength(PaymentTransaction.IdempotencyKeyMaxLength).IsRequired();
        builder.Property(t => t.ProviderReference).HasMaxLength(100);
        builder.HasIndex(t => t.IdempotencyKey).IsUnique();
        builder.HasIndex(t => t.PaymentId);
    }
}

internal sealed class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable("ledger_entries", t => t.HasCheckConstraint(
            "ck_ledger_entries_one_side",
            "debit_minor >= 0 AND credit_minor >= 0 AND (debit_minor = 0 OR credit_minor = 0)"));

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).UseIdentityAlwaysColumn();
        builder.Property(e => e.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(e => e.Description).HasMaxLength(LedgerEntry.DescriptionMaxLength).IsRequired();

        builder.HasIndex(e => e.TransactionGroupId);
        builder.HasIndex(e => new { e.HostId, e.Account });
        builder.HasIndex(e => e.PaymentId);
        builder.HasIndex(e => e.PayoutId);

        builder.HasOne<HostProfile>().WithMany().HasForeignKey(e => e.HostId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Payment>().WithMany().HasForeignKey(e => e.PaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Payout>().WithMany().HasForeignKey(e => e.PayoutId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PayoutConfiguration : IEntityTypeConfiguration<Payout>
{
    public void Configure(EntityTypeBuilder<Payout> builder)
    {
        builder.ToTable("payouts", t => t.HasCheckConstraint("ck_payouts_amount", "amount_minor > 0 AND period_end >= period_start"));
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(p => p.ProviderReference).HasMaxLength(100);
        builder.Property(p => p.FailureReason).HasMaxLength(500);
        builder.HasIndex(p => new { p.HostId, p.Status });
        builder.HasOne<HostProfile>().WithMany().HasForeignKey(p => p.HostId).OnDelete(DeleteBehavior.Restrict);
    }
}
