using EforTakip.Domain.Holidays;
using EforTakip.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EforTakip.Persistence.Configurations;

public sealed class HolidayConfiguration : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> builder)
    {
        builder.ToTable("Holidays");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(h => h.Date)
            .IsRequired();

        builder.HasIndex(h => h.Date).IsUnique();

        builder.HasData(SoftwareDeliverySeedData.Holidays.Select((h, i) => new
        {
            Id = SoftwareDeliverySeedData.HolidayId(i + 1),
            Date = new DateOnly(SoftwareDeliverySeedData.HolidayYear, h.Month, h.Day),
            Name = h.Name,
        }));
    }
}
