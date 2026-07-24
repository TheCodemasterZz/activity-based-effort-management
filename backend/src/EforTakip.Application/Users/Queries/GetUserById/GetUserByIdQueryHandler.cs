using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Users.Dtos;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Users;
using EforTakip.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Users.Queries.GetUserById;

public sealed class GetUserByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetUserByIdQuery, UserDetailDto>
{
    public async Task<UserDetailDto> Handle(
        GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.Attributes)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        var directoryName = await db.Directories
            .AsNoTracking()
            .Where(d => d.Id == user.DirectoryId)
            .Select(d => d.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var mappings = await db.DirectoryAttributeMappings
            .AsNoTracking()
            .OrderBy(m => m.SortOrder)
            .ToListAsync(cancellationToken);

        var attributesByMappingId = user.Attributes.ToDictionary(a => a.AttributeMappingId);

        var attributes = mappings
            .Where(m => attributesByMappingId.ContainsKey(m.Id))
            .Select(m => new UserAttributeValueDto
            {
                SystemFieldName = m.SystemFieldName,
                AdAttributeName = m.AdAttributeName,
                FieldType = m.FieldType,
                Value = attributesByMappingId[m.Id].Value,
                ReferencedUserId = attributesByMappingId[m.Id].ReferencedUserId
            })
            .ToList();

        return new UserDetailDto
        {
            Id = user.Id,
            DirectoryId = user.DirectoryId,
            DirectoryName = directoryName,
            Source = user.Source,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DisplayName = user.DisplayName,
            Email = user.Email,
            IsActive = user.IsActive,
            LastSyncedUtc = user.LastSyncedUtc,
            Attributes = attributes
        };
    }
}
