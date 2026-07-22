using EforTakip.Domain.Directories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Persistence.Configurations;

public sealed class DirectoryAttributeMappingConfiguration : IEntityTypeConfiguration<DirectoryAttributeMapping>
{
    public void Configure(EntityTypeBuilder<DirectoryAttributeMapping> builder)
    {
        builder.ToTable("DirectoryAttributeMappings");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.AdAttributeName).IsRequired().HasMaxLength(150);
        builder.Property(m => m.SystemFieldName).IsRequired().HasMaxLength(150);
        builder.Property(m => m.FieldType).IsRequired().HasMaxLength(50);

        builder.HasIndex(m => new { m.DirectoryId, m.AdAttributeName }).IsUnique();

        builder.HasOne<Directory>()
            .WithMany()
            .HasForeignKey(m => m.DirectoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
