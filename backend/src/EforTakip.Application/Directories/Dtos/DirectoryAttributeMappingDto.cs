namespace EforTakip.Application.Directories.Dtos;

public sealed class DirectoryAttributeMappingDto
{
    public Guid Id { get; init; }
    public string AdAttributeName { get; init; } = default!;
    public string SystemFieldName { get; init; } = default!;
    public string FieldType { get; init; } = default!;
    public bool IsSynced { get; init; }
    public int SortOrder { get; init; }
}
