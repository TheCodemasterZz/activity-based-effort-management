using EforTakip.Domain.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EforTakip.Persistence.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(2000);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.IsActive)
            .IsRequired();

        // Soft delete: pasife alınmış projeler tüm sorgulardan (liste, GetById) otomatik
        // hariç tutulur; veri fiziksel olarak silinmez.
        builder.HasQueryFilter(p => p.IsActive);

        builder.HasMany(p => p.CustomerAssignments)
            .WithOne()
            .HasForeignKey(a => a.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Project.CustomerAssignments))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(p => p.EmployeeAssignments)
            .WithOne()
            .HasForeignKey(a => a.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Project.EmployeeAssignments))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
