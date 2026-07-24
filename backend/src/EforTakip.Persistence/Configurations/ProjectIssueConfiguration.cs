using EforTakip.Domain.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EforTakip.Persistence.Configurations;

public sealed class ProjectIssueConfiguration : IEntityTypeConfiguration<ProjectIssue>
{
    public void Configure(EntityTypeBuilder<ProjectIssue> builder)
    {
        builder.ToTable("ProjectIssues");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.Description)
            .HasMaxLength(2000);

        builder.Property(i => i.Priority)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(i => i.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(i => i.Resolution)
            .HasMaxLength(2000);

        builder.HasIndex(i => i.ProjectId);

        builder.HasOne<EforTakip.Domain.Users.User>()
            .WithMany()
            .HasForeignKey(i => i.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
