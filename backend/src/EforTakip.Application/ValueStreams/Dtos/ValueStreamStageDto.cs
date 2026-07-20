namespace EforTakip.Application.ValueStreams.Dtos;

public sealed class ValueStreamStageDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public int Order { get; init; }
}
