using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Directories.Dtos;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Directories.Queries.GetDirectoryUserById;

public sealed class GetDirectoryUserByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetDirectoryUserByIdQuery, DirectoryUserDetailDto>
{
    public async Task<DirectoryUserDetailDto> Handle(
        GetDirectoryUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await db.DirectoryUsers
            .AsNoTracking()
            .Include(u => u.Attributes)
            .FirstOrDefaultAsync(u => u.Id == request.DirectoryUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(DirectoryUser), request.DirectoryUserId);

        var directoryName = await db.Directories
            .AsNoTracking()
            .Where(d => d.Id == user.DirectoryId)
            .Select(d => d.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var mappings = await db.DirectoryAttributeMappings
            .AsNoTracking()
            .OrderBy(m => m.SortOrder)
            .ToListAsync(cancellationToken);

        var valuesByMappingId = user.Attributes.ToDictionary(a => a.AttributeMappingId, a => a.Value);

        var attributes = mappings
            .Where(m => valuesByMappingId.ContainsKey(m.Id))
            .Select(m => new DirectoryUserAttributeValueDto
            {
                SystemFieldName = m.SystemFieldName,
                AdAttributeName = m.AdAttributeName,
                FieldType = m.FieldType,
                Value = valuesByMappingId[m.Id]
            })
            .ToList();

        return new DirectoryUserDetailDto
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
