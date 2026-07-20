namespace EforTakip.Persistence.Seed;

/// <summary>
/// Sistemde hazır bulunan sabit mesai takvimleri (gerçek/sabit referans veri — HasData ile
/// hem Test Mode'a hem migration'a aynı kaynaktan uygulanır, bkz. <see cref="SoftwareDeliverySeedData"/>).
/// </summary>
public static class WorkCalendarSeedData
{
    public static readonly Guid StandardCalendarId = Id(0);
    public static readonly Guid FlexCalendarId = Id(1);

    public const string StandardCalendarName = "Standart Ofis Mesaisi";
    public const string FlexCalendarName = "Esnek Vardiya";

    public readonly record struct DaySeed(DayOfWeek DayOfWeek, bool IsWorkingDay, TimeOnly? StartTime, TimeOnly? EndTime);

    public static readonly DaySeed[] StandardCalendarDays =
    [
        new(DayOfWeek.Monday, true, new TimeOnly(9, 0), new TimeOnly(18, 0)),
        new(DayOfWeek.Tuesday, true, new TimeOnly(9, 0), new TimeOnly(18, 0)),
        new(DayOfWeek.Wednesday, true, new TimeOnly(9, 0), new TimeOnly(18, 0)),
        new(DayOfWeek.Thursday, true, new TimeOnly(9, 0), new TimeOnly(18, 0)),
        new(DayOfWeek.Friday, true, new TimeOnly(9, 0), new TimeOnly(18, 0)),
        new(DayOfWeek.Saturday, false, null, null),
        new(DayOfWeek.Sunday, false, null, null),
    ];

    public static readonly DaySeed[] FlexCalendarDays =
    [
        new(DayOfWeek.Monday, true, new TimeOnly(9, 0), new TimeOnly(17, 0)),
        new(DayOfWeek.Tuesday, true, new TimeOnly(9, 0), new TimeOnly(17, 0)),
        new(DayOfWeek.Wednesday, true, new TimeOnly(9, 0), new TimeOnly(17, 0)),
        new(DayOfWeek.Thursday, true, new TimeOnly(9, 0), new TimeOnly(17, 0)),
        new(DayOfWeek.Friday, true, new TimeOnly(9, 0), new TimeOnly(17, 0)),
        new(DayOfWeek.Saturday, true, new TimeOnly(10, 0), new TimeOnly(14, 0)),
        new(DayOfWeek.Sunday, false, null, null),
    ];

    private static Guid Id(int index) => Guid.Parse($"00000000-0000-0000-0006-{index:D12}");

    public static Guid DayId(int calendarIndex, int dayIndex) => Guid.Parse($"00000000-0000-0000-0007-{(calendarIndex * 10 + dayIndex):D12}");
}
