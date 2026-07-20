using EforTakip.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DomainActivity = EforTakip.Domain.Activities.Activity;

namespace EforTakip.Persistence.Configurations;

public sealed class ActivityConfiguration : IEntityTypeConfiguration<DomainActivity>
{
    public void Configure(EntityTypeBuilder<DomainActivity> builder)
    {
        builder.ToTable("Activities");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Description)
            .HasMaxLength(2000);

        builder.HasOne<DomainActivity>()
            .WithMany()
            .HasForeignKey(a => a.ParentActivityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(BuildSoftwareDeliveryActivities());
    }

    private static IEnumerable<object> BuildSoftwareDeliveryActivities()
    {
        var catalog = SoftwareDeliverySeedData.ActivityCatalog;

        for (var l1Index = 0; l1Index < catalog.Length; l1Index++)
        {
            var (l1Name, l2Names) = catalog[l1Index];

            yield return new
            {
                Id = SoftwareDeliverySeedData.L1Id(l1Index),
                Name = l1Name,
                Description = (string?)null,
                ParentActivityId = (Guid?)null,
            };

            for (var l2Index = 0; l2Index < l2Names.Length; l2Index++)
            {
                yield return new
                {
                    Id = SoftwareDeliverySeedData.L2Id(l1Index, l2Index),
                    Name = l2Names[l2Index],
                    Description = (string?)null,
                    ParentActivityId = (Guid?)SoftwareDeliverySeedData.L1Id(l1Index),
                };
            }
        }
    }
}
