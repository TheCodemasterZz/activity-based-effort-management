using EforTakip.Domain.Employees;
using EforTakip.Domain.WorkCalendars;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EforTakip.Persistence.Configurations;

public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Email)
            .HasMaxLength(320);

        builder.HasOne<WorkCalendar>()
            .WithMany()
            .HasForeignKey(e => e.WorkCalendarId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
