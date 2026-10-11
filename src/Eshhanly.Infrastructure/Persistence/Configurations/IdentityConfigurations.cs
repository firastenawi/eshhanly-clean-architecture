using Eshhanly.Application.Common.Security;
using Eshhanly.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eshhanly.Infrastructure.Persistence.Configurations;

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("users");
        builder.Property(u => u.FullName).HasMaxLength(ApplicationUser.FullNameMaxLength).IsRequired();
        builder.Property(u => u.PreferredLanguage).HasMaxLength(5).IsRequired();
        builder.HasIndex(u => u.PhoneNumber).IsUnique().HasFilter("phone_number IS NOT NULL");
    }
}

internal sealed class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    // Fixed ids/stamps keep the seed deterministic across migrations.
    private static readonly (string Name, Guid Id)[] SeedRoles =
    [
        (Roles.Guest, new Guid("01928f3a-0000-7000-8000-000000000001")),
        (Roles.Host, new Guid("01928f3a-0000-7000-8000-000000000002")),
        (Roles.Admin, new Guid("01928f3a-0000-7000-8000-000000000003")),
    ];

    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.ToTable("roles");
        builder.HasData(SeedRoles.Select(r => new ApplicationRole
        {
            Id = r.Id,
            Name = r.Name,
            NormalizedName = r.Name.ToUpperInvariant(),
            ConcurrencyStamp = r.Id.ToString(),
        }));
    }
}

internal sealed class IdentityTablesConfiguration :
    IEntityTypeConfiguration<IdentityUserRole<Guid>>,
    IEntityTypeConfiguration<IdentityUserClaim<Guid>>,
    IEntityTypeConfiguration<IdentityUserLogin<Guid>>,
    IEntityTypeConfiguration<IdentityUserToken<Guid>>,
    IEntityTypeConfiguration<IdentityRoleClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserRole<Guid>> builder) => builder.ToTable("user_roles");
    public void Configure(EntityTypeBuilder<IdentityUserClaim<Guid>> builder) => builder.ToTable("user_claims");
    public void Configure(EntityTypeBuilder<IdentityUserLogin<Guid>> builder) => builder.ToTable("user_logins");
    public void Configure(EntityTypeBuilder<IdentityUserToken<Guid>> builder) => builder.ToTable("user_tokens");
    public void Configure(EntityTypeBuilder<IdentityRoleClaim<Guid>> builder) => builder.ToTable("role_claims");
}

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.TokenHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(t => t.DeviceInfo).HasMaxLength(200);
        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => t.UserId);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
