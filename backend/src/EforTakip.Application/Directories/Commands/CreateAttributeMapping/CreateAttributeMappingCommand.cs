using MediatR;

namespace EforTakip.Application.Directories.Commands.CreateAttributeMapping;

public sealed record CreateAttributeMappingCommand(
    Guid DirectoryId,
    string AdAttributeName,
    string SystemFieldName,
    string FieldType,
    bool IsSynced,
    int SortOrder) : IRequest<Guid>;
