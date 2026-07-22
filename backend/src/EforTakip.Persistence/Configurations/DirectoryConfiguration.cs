using EforTakip.Domain.Directories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Persistence.Configurations;

public sealed class DirectoryConfiguration : IEntityTypeConfiguration<Directory>
{
    public void Configure(EntityTypeBuilder<Directory> builder)
    {
        builder.ToTable("Directories");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name).IsRequired().HasMaxLength(200);
        builder.Property(d => d.Source).IsRequired().HasConversion<string>().HasMaxLength(30);
        builder.Property(d => d.Permission).IsRequired().HasConversion<string>().HasMaxLength(30);
        builder.Property(d => d.SyncSchedule).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(d => d.DirectoryType).HasMaxLength(100);
        builder.Property(d => d.Hostname).HasMaxLength(255);
        builder.Property(d => d.BindUsername).HasMaxLength(255);
        builder.Property(d => d.BindPasswordEncrypted).HasMaxLength(1024);
        builder.Property(d => d.BaseDn).HasMaxLength(512);
        builder.Property(d => d.AdditionalUserDn).HasMaxLength(512);
        builder.Property(d => d.AdditionalGroupDn).HasMaxLength(512);
        builder.Property(d => d.UserObjectClass).HasMaxLength(100);
        builder.Property(d => d.UserObjectFilter).HasMaxLength(1024);
        builder.Property(d => d.UsernameAttribute).HasMaxLength(100);
        builder.Property(d => d.UsernameRdnAttribute).HasMaxLength(100);
        builder.Property(d => d.FirstNameAttribute).HasMaxLength(100);
        builder.Property(d => d.LastNameAttribute).HasMaxLength(100);
        builder.Property(d => d.DisplayNameAttribute).HasMaxLength(100);
        builder.Property(d => d.EmailAttribute).HasMaxLength(100);
        builder.Property(d => d.UniqueIdAttribute).HasMaxLength(100);
    }
}
