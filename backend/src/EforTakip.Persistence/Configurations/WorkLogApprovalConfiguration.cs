using EforTakip.Domain.WorkLogApprovals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EforTakip.Persistence.Configurations;

public sealed class WorkLogApprovalConfiguration : IEntityTypeConfiguration<WorkLogApproval>
{
    public void Configure(EntityTypeBuilder<WorkLogApproval> builder)
    {
        builder.ToTable("WorkLogApprovals");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.PeriodType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.PeriodStart).IsRequired();
        builder.Property(a => a.PeriodEnd).IsRequired();
        builder.Property(a => a.ApprovedAtUtc).IsRequired();

        builder.HasIndex(a => new { a.EmployeeId, a.PeriodStart, a.PeriodEnd });
    }
}
