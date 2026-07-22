using EforTakip.Domain.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EforTakip.Persistence.Configurations;

public sealed class ConfidenceScoreSettingsConfiguration : IEntityTypeConfiguration<ConfidenceScoreSettings>
{
    public void Configure(EntityTypeBuilder<ConfidenceScoreSettings> builder)
    {
        builder.ToTable("ConfidenceScoreSettings");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.DuplicateSimilarityThreshold).HasPrecision(4, 3);
        builder.Property(s => s.LongDurationHoursThreshold).HasPrecision(6, 2);
        builder.Property(s => s.ShortDurationHoursThreshold).HasPrecision(6, 2);
        builder.Property(s => s.DailyTotalSuspiciousHours).HasPrecision(6, 2);

        builder.Property(s => s.GenericPhrasesCsv)
            .IsRequired()
            .HasMaxLength(2000);
    }
}
