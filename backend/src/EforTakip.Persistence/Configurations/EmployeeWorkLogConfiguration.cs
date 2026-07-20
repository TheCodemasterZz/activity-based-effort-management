using EforTakip.Domain.Customers;
using EforTakip.Domain.Employees;
using EforTakip.Domain.Projects;
using EforTakip.Domain.WorkLogApprovals;
using EforTakip.Domain.WorkLogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DomainActivity = EforTakip.Domain.Activities.Activity;

namespace EforTakip.Persistence.Configurations;

public sealed class EmployeeWorkLogConfiguration : IEntityTypeConfiguration<EmployeeWorkLog>
{
    public void Configure(EntityTypeBuilder<EmployeeWorkLog> builder)
    {
        builder.ToTable("EmployeeWorkLogs");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Hours)
            .HasColumnType("decimal(5,2)")
            .IsRequired();

        builder.Property(w => w.WorkDate)
            .IsRequired();

        builder.Property(w => w.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.HasIndex(w => new { w.EmployeeId, w.WorkDate });

        builder.HasOne<Employee>().WithMany().HasForeignKey(w => w.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Project>().WithMany().HasForeignKey(w => w.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Customer>().WithMany().HasForeignKey(w => w.CustomerId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<DomainActivity>()
            .WithMany()
            .HasForeignKey(w => w.ActivityL1Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<DomainActivity>()
            .WithMany()
            .HasForeignKey(w => w.ActivityL2Id)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<WorkLogApproval>()
            .WithMany()
            .HasForeignKey(w => w.ApprovalId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
