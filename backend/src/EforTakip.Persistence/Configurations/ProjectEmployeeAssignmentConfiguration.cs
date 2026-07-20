using EforTakip.Domain.Employees;
using EforTakip.Domain.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EforTakip.Persistence.Configurations;

public sealed class ProjectEmployeeAssignmentConfiguration : IEntityTypeConfiguration<ProjectEmployeeAssignment>
{
    public void Configure(EntityTypeBuilder<ProjectEmployeeAssignment> builder)
    {
        builder.ToTable("ProjectEmployees");
        builder.HasKey(a => a.Id);

        builder.HasIndex(a => new { a.ProjectId, a.EmployeeId }).IsUnique();

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
