using EforTakip.Domain.Common;

namespace EforTakip.Domain.ValueStreams;

public sealed class StageActivityAssignment : Entity
{
    public Guid ValueStreamStageId { get; private set; }
    public Guid ActivityId { get; private set; }

    private StageActivityAssignment()
    {
        // EF Core
    }

    public static StageActivityAssignment Create(Guid valueStreamStageId, Guid activityId)
        => new()
        {
            ValueStreamStageId = valueStreamStageId,
            ActivityId = activityId
        };
}
