using EforTakip.Domain.Directories;
using MediatR;

namespace EforTakip.Application.Directories.Commands.UpdateDirectory;

public sealed record UpdateDirectoryCommand(
    Guid Id,
    string Name,
    DirectorySource Source,
    string? DirectoryType,
    string? Hostname,
    int Port,
    bool UseSsl,
    string? BindUsername,
    string? BindPassword,
    string? BaseDn,
    string? AdditionalUserDn,
    string? AdditionalGroupDn,
    DirectoryPermission Permission,
    string? UserObjectClass,
    string? UserObjectFilter,
    string? UsernameAttribute,
    string? UsernameRdnAttribute,
    string? FirstNameAttribute,
    string? LastNameAttribute,
    string? DisplayNameAttribute,
    string? EmailAttribute,
    string? UniqueIdAttribute,
    SyncScheduleKind SyncSchedule) : IRequest;
