using EforTakip.Domain.WorkCalendars;
using EforTakip.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EforTakip.Persistence.Configurations;

public sealed class WorkCalendarDayConfiguration : IEntityTypeConfiguration<WorkCalendarDay>
{
    public void Configure(EntityTypeBuilder<WorkCalendarDay> builder)
    {
        builder.ToTable("WorkCalendarDays");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.DayOfWeek).IsRequired();
        builder.Property(d => d.IsWorkingDay).IsRequired();

        builder.HasIndex(d => new { d.WorkCalendarId, d.DayOfWeek }).IsUnique();

        builder.HasData(BuildSeedDays());
    }

    private static IEnumerable<object> BuildSeedDays()
    {
        return BuildCalendarDays(0, WorkCalendarSeedData.StandardCalendarId, WorkCalendarSeedData.StandardCalendarDays)
            .Concat(BuildCalendarDays(1, WorkCalendarSeedData.FlexCalendarId, WorkCalendarSeedData.FlexCalendarDays));
    }

    private static IEnumerable<object> BuildCalendarDays(
        int calendarIndex, Guid calendarId, WorkCalendarSeedData.DaySeed[] days)
    {
        for (var i = 0; i < days.Length; i++)
        {
            var day = days[i];
            yield return new
            {
                Id = WorkCalendarSeedData.DayId(calendarIndex, i),
                WorkCalendarId = calendarId,
                DayOfWeek = day.DayOfWeek,
                IsWorkingDay = day.IsWorkingDay,
                StartTime = day.StartTime,
                EndTime = day.EndTime,
            };
        }
    }
}
