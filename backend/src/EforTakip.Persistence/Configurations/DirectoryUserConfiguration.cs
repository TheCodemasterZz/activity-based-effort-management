using EforTakip.Domain.Directories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Persistence.Configurations;

public sealed class DirectoryUserConfiguration : IEntityTypeConfiguration<DirectoryUser>
{
    public void Configure(EntityTypeBuilder<DirectoryUser> builder)
    {
        builder.ToTable("DirectoryUsers");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Username).IsRequired().HasMaxLength(150);
        builder.Property(u => u.Source).IsRequired().HasConversion<string>().HasMaxLength(30);
        builder.Property(u => u.FirstName).HasMaxLength(150);
        builder.Property(u => u.LastName).HasMaxLength(150);
        builder.Property(u => u.DisplayName).HasMaxLength(300);
        builder.Property(u => u.Email).HasMaxLength(255);
        builder.Property(u => u.ObjectGuid).HasMaxLength(100);
        builder.Property(u => u.PasswordHash).HasMaxLength(500);

        builder.HasIndex(u => u.Username).IsUnique();

        builder.HasMany(u => u.Attributes)
            .WithOne()
            .HasForeignKey(a => a.DirectoryUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(DirectoryUser.Attributes))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne<Directory>()
            .WithMany()
            .HasForeignKey(u => u.DirectoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.Roles)
            .WithOne()
            .HasForeignKey(r => r.DirectoryUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(DirectoryUser.Roles))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
