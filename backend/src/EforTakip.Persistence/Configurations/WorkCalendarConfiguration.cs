using EforTakip.Domain.WorkCalendars;
using EforTakip.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EforTakip.Persistence.Configurations;

public sealed class WorkCalendarConfiguration : IEntityTypeConfiguration<WorkCalendar>
{
    public void Configure(EntityTypeBuilder<WorkCalendar> builder)
    {
        builder.ToTable("WorkCalendars");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasMany(c => c.Days)
            .WithOne()
            .HasForeignKey(d => d.WorkCalendarId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(WorkCalendar.Days))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasData(
            new { Id = WorkCalendarSeedData.StandardCalendarId, Name = WorkCalendarSeedData.StandardCalendarName },
            new { Id = WorkCalendarSeedData.FlexCalendarId, Name = WorkCalendarSeedData.FlexCalendarName });
    }
}
