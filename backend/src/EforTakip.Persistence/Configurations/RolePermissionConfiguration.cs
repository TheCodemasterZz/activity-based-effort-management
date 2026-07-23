using EforTakip.Domain.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EforTakip.Persistence.Configurations;

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.PermissionKey).IsRequired().HasMaxLength(100);

        builder.HasIndex(p => new { p.RoleId, p.PermissionKey }).IsUnique();
    }
}
