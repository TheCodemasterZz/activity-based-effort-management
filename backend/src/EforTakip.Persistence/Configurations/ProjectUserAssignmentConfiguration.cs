using EforTakip.Domain.Projects;
using EforTakip.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EforTakip.Persistence.Configurations;

public sealed class ProjectUserAssignmentConfiguration : IEntityTypeConfiguration<ProjectUserAssignment>
{
    public void Configure(EntityTypeBuilder<ProjectUserAssignment> builder)
    {
        builder.ToTable("ProjectUsers");
        builder.HasKey(a => a.Id);

        builder.HasIndex(a => new { a.ProjectId, a.UserId }).IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
