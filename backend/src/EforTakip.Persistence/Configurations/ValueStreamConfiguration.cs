using EforTakip.Domain.ValueStreams;
using EforTakip.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EforTakip.Persistence.Configurations;

public sealed class ValueStreamConfiguration : IEntityTypeConfiguration<ValueStream>
{
    public void Configure(EntityTypeBuilder<ValueStream> builder)
    {
        builder.ToTable("ValueStreams");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(v => v.Description)
            .HasMaxLength(2000);

        builder.HasMany(v => v.Stages)
            .WithOne()
            .HasForeignKey(s => s.ValueStreamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(ValueStream.Stages))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasData(new
        {
            Id = SoftwareDeliverySeedData.ValueStreamId,
            Name = SoftwareDeliverySeedData.ValueStreamName,
            Description = (string?)null,
        });
    }
}
