using EforTakip.Domain.Users;
using EforTakip.Domain.Leaves;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EforTakip.Persistence.Configurations;

public sealed class LeaveConfiguration : IEntityTypeConfiguration<Leave>
{
    public void Configure(EntityTypeBuilder<Leave> builder)
    {
        builder.ToTable("Leaves");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.StartDate).IsRequired();
        builder.Property(l => l.EndDate).IsRequired();
        builder.Property(l => l.IsFullDay).IsRequired();
        builder.Property(l => l.Description).HasMaxLength(500);

        builder.HasIndex(l => new { l.UserId, l.StartDate, l.EndDate });

        builder.HasOne<User>().WithMany().HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
