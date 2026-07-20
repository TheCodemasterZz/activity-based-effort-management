using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Common.Models;
using EforTakip.Application.ValueStreams.Dtos;
using EforTakip.Domain.ValueStreams;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.ValueStreams.Queries.GetValueStreams;

public sealed class GetValueStreamsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetValueStreamsQuery, PagedResult<ValueStreamDto>>
{
    public async Task<PagedResult<ValueStreamDto>> Handle(GetValueStreamsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<ValueStream> query = db.ValueStreams.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.NameFilter))
        {
            var nameFilter = request.NameFilter.ToLower();
            query = query.Where(v => v.Name.ToLower().Contains(nameFilter));
        }

        query = request.SortBy switch
        {
            "name" => request.Descending ? query.OrderByDescending(v => v.Name) : query.OrderBy(v => v.Name),
            _ => query.OrderByDescending(v => v.Id)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectToType<ValueStreamDto>()
            .ToListAsync(cancellationToken);

        return new PagedResult<ValueStreamDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
