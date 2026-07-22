using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Directories.Dtos;
using EforTakip.Domain.Directories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Directories.Queries.GetOrgChart;

public sealed class GetOrgChartQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetOrgChartQuery, OrgChartResultDto>
{
    public async Task<OrgChartResultDto> Handle(GetOrgChartQuery request, CancellationToken cancellationToken)
    {
        var managerMapping = await db.DirectoryAttributeMappings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.FieldType == DirectoryAttributeMapping.UserReferenceFieldType, cancellationToken);

        if (managerMapping is null)
            return new OrgChartResultDto { HasManagerMapping = false, Nodes = [] };

        var photoMapping = await db.DirectoryAttributeMappings
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.FieldType == DirectoryAttributeMapping.PhotoFieldType, cancellationToken);

        var users = await db.DirectoryUsers
            .AsNoTracking()
            .Include(u => u.Attributes)
            .Where(u => u.DirectoryId == request.DirectoryId && u.IsActive)
            .ToListAsync(cancellationToken);

        var nodes = users
            .Select(u =>
            {
                var managerAttribute = u.Attributes
                    .FirstOrDefault(a => a.AttributeMappingId == managerMapping.Id);

                return new OrgChartNodeDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    DisplayName = u.DisplayName ?? u.Username,
                    ManagerId = managerAttribute?.ReferencedDirectoryUserId,
                    UnresolvedManagerName = managerAttribute?.ReferencedDirectoryUserId is null
                        ? managerAttribute?.Value
                        : null,
                    PhotoBase64 = photoMapping is null
                        ? null
                        : u.Attributes.FirstOrDefault(a => a.AttributeMappingId == photoMapping.Id)?.Value
                };
            })
            .ToList();

        return new OrgChartResultDto { HasManagerMapping = true, Nodes = nodes };
    }
}
