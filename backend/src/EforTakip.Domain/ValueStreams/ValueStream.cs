using EforTakip.Domain.Common;
using EforTakip.Domain.Exceptions;

namespace EforTakip.Domain.ValueStreams;

public sealed class ValueStream : Entity, IAggregateRoot
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }

    private readonly List<ValueStreamStage> _stages = [];
    public IReadOnlyCollection<ValueStreamStage> Stages => _stages.AsReadOnly();

    private ValueStream()
    {
        // EF Core
    }

    public static ValueStream Create(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleValidationException("Değer akışı adı boş olamaz.");

        return new ValueStream
        {
            Name = name.Trim(),
            Description = description
        };
    }

    public ValueStreamStage AddStage(string name, int order)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleValidationException("Aşama adı boş olamaz.");

        if (_stages.Any(s => s.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new BusinessRuleValidationException("Bu isimde bir aşama zaten var.");

        var stage = ValueStreamStage.Create(Id, name, order);
        _stages.Add(stage);
        return stage;
    }
}
