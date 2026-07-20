using EforTakip.Domain.Common;

namespace EforTakip.Domain.ValueStreams;

public sealed class ValueStreamStage : Entity
{
    public Guid ValueStreamId { get; private set; }
    public string Name { get; private set; } = default!;
    public int Order { get; private set; }

    private ValueStreamStage()
    {
        // EF Core
    }

    internal static ValueStreamStage Create(Guid valueStreamId, string name, int order)
        => new()
        {
            ValueStreamId = valueStreamId,
            Name = name.Trim(),
            Order = order
        };
}
