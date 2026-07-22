using MediatR;

namespace EforTakip.Application.Directories.Commands.UpdateAttributeMapping;

public sealed record UpdateAttributeMappingCommand(
    Guid Id,
    string AdAttributeName,
    string SystemFieldName,
    string FieldType,
    bool IsSynced,
    int SortOrder) : IRequest;
