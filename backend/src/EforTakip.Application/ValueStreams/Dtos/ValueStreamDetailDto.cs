namespace EforTakip.Application.ValueStreams.Dtos;

public sealed class ValueStreamDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public IReadOnlyCollection<ValueStreamStageDto> Stages { get; init; } = [];
}
