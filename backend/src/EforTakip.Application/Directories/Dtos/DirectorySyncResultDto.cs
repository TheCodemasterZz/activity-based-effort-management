namespace EforTakip.Application.Directories.Dtos;

public sealed class DirectorySyncResultDto
{
    public Guid DirectoryId { get; init; }
    public string DirectoryName { get; init; } = default!;
    public int Added { get; init; }
    public int Updated { get; init; }
    public int Deactivated { get; init; }
    public int TotalFromDirectory { get; init; }
    public DateTime SyncedAtUtc { get; init; }
}
