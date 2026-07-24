using EforTakip.Domain.Users;
using EforTakip.Domain.Projects;
using EforTakip.Domain.WorkLogApprovals;
using EforTakip.Domain.WorkLogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DomainActivity = EforTakip.Domain.Activities.Activity;

namespace EforTakip.Persistence.Configurations;

public sealed class WorkLogConfiguration : IEntityTypeConfiguration<WorkLog>
{
    public void Configure(EntityTypeBuilder<WorkLog> builder)
    {
        builder.ToTable("WorkLogs");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Hours)
            .HasColumnType("decimal(5,2)")
            .IsRequired();

        builder.Property(w => w.WorkDate)
            .IsRequired();

        builder.Property(w => w.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(w => w.EntryType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(w => new { w.UserId, w.EntryType, w.WorkDate });

        builder.HasOne<User>().WithMany().HasForeignKey(w => w.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Project>().WithMany().HasForeignKey(w => w.ProjectId).OnDelete(DeleteBehavior.Restrict);

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
