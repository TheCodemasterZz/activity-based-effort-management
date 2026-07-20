using EforTakip.Domain.ValueStreams;
using EforTakip.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EforTakip.Persistence.Configurations;

public sealed class ValueStreamStageConfiguration : IEntityTypeConfiguration<ValueStreamStage>
{
    public void Configure(EntityTypeBuilder<ValueStreamStage> builder)
    {
        builder.ToTable("ValueStreamStages");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Order)
            .IsRequired();

        builder.HasData(SoftwareDeliverySeedData.Stages.Select(stage => new
        {
            Id = SoftwareDeliverySeedData.StageId(stage.Order),
            ValueStreamId = SoftwareDeliverySeedData.ValueStreamId,
            Name = stage.Name,
            Order = stage.Order,
        }));
    }
}
