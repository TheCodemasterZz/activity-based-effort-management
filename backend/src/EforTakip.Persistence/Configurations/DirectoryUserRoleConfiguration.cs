using EforTakip.Domain.Directories;
using EforTakip.Domain.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EforTakip.Persistence.Configurations;

public sealed class DirectoryUserRoleConfiguration : IEntityTypeConfiguration<DirectoryUserRole>
{
    public void Configure(EntityTypeBuilder<DirectoryUserRole> builder)
    {
        builder.ToTable("DirectoryUserRoles");
        builder.HasKey(r => r.Id);

        builder.HasIndex(r => new { r.DirectoryUserId, r.RoleId }).IsUnique();

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(r => r.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
