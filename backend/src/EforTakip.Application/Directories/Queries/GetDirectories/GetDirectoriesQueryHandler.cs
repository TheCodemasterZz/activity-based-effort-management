using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Common.Models;
using EforTakip.Application.Directories.Dtos;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Directories.Queries.GetDirectories;

public sealed class GetDirectoriesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetDirectoriesQuery, PagedResult<DirectoryDto>>
{
    public async Task<PagedResult<DirectoryDto>> Handle(GetDirectoriesQuery request, CancellationToken cancellationToken)
    {
        var query = db.Directories.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.NameFilter))
        {
            var nameFilter = request.NameFilter.ToLower();
            query = query.Where(d => d.Name.ToLower().Contains(nameFilter));
        }

        query = query.OrderBy(d => d.SortOrder).ThenBy(d => d.Name);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectToType<DirectoryDto>()
            .ToListAsync(cancellationToken);

        return new PagedResult<DirectoryDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
