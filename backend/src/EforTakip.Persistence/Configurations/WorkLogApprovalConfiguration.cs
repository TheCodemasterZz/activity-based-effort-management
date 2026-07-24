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
        builder.Property(a => a.Description).HasMaxLength(1000);
        builder.Property(a => a.ApprovedAtUtc).IsRequired();

        builder.Property(a => a.EntryType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(a => new { a.UserId, a.EntryType, a.PeriodStart, a.PeriodEnd });

        builder.HasOne<Domain.Users.User>()
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
