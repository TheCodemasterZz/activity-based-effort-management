using EforTakip.Domain.Directories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EforTakip.Persistence.Configurations;

public sealed class DirectoryUserAttributeConfiguration : IEntityTypeConfiguration<DirectoryUserAttribute>
{
    public void Configure(EntityTypeBuilder<DirectoryUserAttribute> builder)
    {
        builder.ToTable("DirectoryUserAttributes");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Value).HasMaxLength(2000);

        builder.HasIndex(a => new { a.DirectoryUserId, a.AttributeMappingId }).IsUnique();

        builder.HasOne<DirectoryAttributeMapping>()
            .WithMany()
            .HasForeignKey(a => a.AttributeMappingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
