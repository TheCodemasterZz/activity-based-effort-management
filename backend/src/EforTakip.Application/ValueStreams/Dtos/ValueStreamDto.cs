namespace EforTakip.Application.ValueStreams.Dtos;

public sealed class ValueStreamDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
}
