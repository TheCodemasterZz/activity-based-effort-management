using EforTakip.Domain.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EforTakip.Persistence.Configurations;

public sealed class ProjectTaskConfiguration : IEntityTypeConfiguration<ProjectTask>
{
    public void Configure(EntityTypeBuilder<ProjectTask> builder)
    {
        builder.ToTable("ProjectTasks");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.EstimatedEffortHours)
            .HasPrecision(9, 2)
            .IsRequired();

        builder.Property(t => t.BaselineEffortHours)
            .HasPrecision(9, 2)
            .IsRequired();

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.IsMilestone)
            .IsRequired();

        builder.HasIndex(t => t.ProjectId);

        // Self-FK'ler: WBS hiyerarşisi (ParentTaskId) ve basit finish-to-start bağımlılık
        // (DependsOnTaskId). Restrict — bir görev silinirken alt/bağımlı görevlerin sessizce
        // kopması istenmiyor (silme komutu zaten hard-delete, kazara zincirleme kayıp riski var).
        builder.HasOne<ProjectTask>()
            .WithMany()
            .HasForeignKey(t => t.ParentTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ProjectTask>()
            .WithMany()
            .HasForeignKey(t => t.DependsOnTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<EforTakip.Domain.Employees.Employee>()
            .WithMany()
            .HasForeignKey(t => t.AssignedEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
