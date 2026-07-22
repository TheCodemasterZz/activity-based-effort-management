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

        builder.Property(p => p.StartDate);
        builder.Property(p => p.EndDate);

        builder.Property(p => p.HealthStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.Sponsor)
            .HasMaxLength(200);

        builder.Property(p => p.Priority)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.StrategicGoal)
            .HasMaxLength(500);

        builder.HasOne<EforTakip.Domain.Employees.Employee>()
            .WithMany()
            .HasForeignKey(p => p.ProjectManagerEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

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
