using EforTakip.Domain.Employees;
using EforTakip.Domain.EmployeeLeaves;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EforTakip.Persistence.Configurations;

public sealed class EmployeeLeaveConfiguration : IEntityTypeConfiguration<EmployeeLeave>
{
    public void Configure(EntityTypeBuilder<EmployeeLeave> builder)
    {
        builder.ToTable("EmployeeLeaves");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.StartDate).IsRequired();
        builder.Property(l => l.EndDate).IsRequired();
        builder.Property(l => l.IsFullDay).IsRequired();
        builder.Property(l => l.Description).HasMaxLength(500);

        builder.HasIndex(l => new { l.EmployeeId, l.StartDate, l.EndDate });

        builder.HasOne<Employee>().WithMany().HasForeignKey(l => l.EmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}
