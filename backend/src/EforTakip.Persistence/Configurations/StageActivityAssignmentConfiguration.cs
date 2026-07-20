using EforTakip.Domain.ValueStreams;
using EforTakip.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DomainActivity = EforTakip.Domain.Activities.Activity;

namespace EforTakip.Persistence.Configurations;

public sealed class StageActivityAssignmentConfiguration : IEntityTypeConfiguration<StageActivityAssignment>
{
    public void Configure(EntityTypeBuilder<StageActivityAssignment> builder)
    {
        builder.ToTable("StageActivities");
        builder.HasKey(a => a.Id);

        builder.HasIndex(a => new { a.ValueStreamStageId, a.ActivityId }).IsUnique();

        builder.HasOne<ValueStreamStage>()
            .WithMany()
            .HasForeignKey(a => a.ValueStreamStageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<DomainActivity>()
            .WithMany()
            .HasForeignKey(a => a.ActivityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(BuildSoftwareDeliveryAssignments());
    }

    private static IEnumerable<object> BuildSoftwareDeliveryAssignments()
    {
        var stageOrderByName = SoftwareDeliverySeedData.Stages.ToDictionary(s => s.Name, s => s.Order);
        var assignmentIndex = 0;

        foreach (var (stageName, l1Names) in SoftwareDeliverySeedData.StageActivityMap)
        {
            var stageId = SoftwareDeliverySeedData.StageId(stageOrderByName[stageName]);

            foreach (var l1Name in l1Names)
            {
                assignmentIndex++;
                var (_, activityId) = SoftwareDeliverySeedData.L1Lookup[l1Name];

                yield return new
                {
                    Id = SoftwareDeliverySeedData.AssignmentId(assignmentIndex),
                    ValueStreamStageId = stageId,
                    ActivityId = activityId,
                };
            }
        }
    }
}
