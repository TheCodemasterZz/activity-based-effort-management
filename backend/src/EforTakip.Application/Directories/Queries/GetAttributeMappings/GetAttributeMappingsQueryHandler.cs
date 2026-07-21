using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Directories.Dtos;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Directories.Queries.GetAttributeMappings;

public sealed class GetAttributeMappingsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAttributeMappingsQuery, IReadOnlyCollection<DirectoryAttributeMappingDto>>
{
    public async Task<IReadOnlyCollection<DirectoryAttributeMappingDto>> Handle(
        GetAttributeMappingsQuery request, CancellationToken cancellationToken)
    {
        return await db.DirectoryAttributeMappings
            .AsNoTracking()
            .OrderBy(m => m.SortOrder)
            .ProjectToType<DirectoryAttributeMappingDto>()
            .ToListAsync(cancellationToken);
    }
}
