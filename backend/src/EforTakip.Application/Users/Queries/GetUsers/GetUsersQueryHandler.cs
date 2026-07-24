using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Common.Models;
using EforTakip.Application.Users.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Users.Queries.GetUsers;

public sealed class GetUsersQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetUsersQuery, PagedResult<UserDto>>
{
    public async Task<PagedResult<UserDto>> Handle(
        GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = db.Users.AsNoTracking();

        if (request.DirectoryId is { } directoryId)
            query = query.Where(u => u.DirectoryId == directoryId);

        if (request.OnlyActive == true)
            query = query.Where(u => u.IsActive);

        if (request.OnlyMissingWorkCalendar == true)
            query = query.Where(u => u.WorkCalendarId == null);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(u =>
                u.Username.ToLower().Contains(term) ||
                (u.DisplayName != null && u.DisplayName.ToLower().Contains(term)) ||
                (u.Email != null && u.Email.ToLower().Contains(term)));
        }

        query = query.OrderBy(u => u.Username);

        var totalCount = await query.CountAsync(cancellationToken);

        // Dizin adı için join — N+1 sorgusundan kaçınmak amacıyla tek sorguda projekte edilir.
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Join(db.Directories, u => u.DirectoryId, d => d.Id, (u, d) => new { u, DirectoryName = d.Name })
            .GroupJoin(db.WorkCalendars, x => x.u.WorkCalendarId, wc => wc.Id, (x, wcs) => new { x.u, x.DirectoryName, WorkCalendars = wcs })
            .SelectMany(x => x.WorkCalendars.DefaultIfEmpty(), (x, wc) => new UserDto
            {
                Id = x.u.Id,
                DirectoryId = x.u.DirectoryId,
                DirectoryName = x.DirectoryName,
                Source = x.u.Source,
                Username = x.u.Username,
                FirstName = x.u.FirstName,
                LastName = x.u.LastName,
                DisplayName = x.u.DisplayName,
                Email = x.u.Email,
                IsActive = x.u.IsActive,
                LastSyncedUtc = x.u.LastSyncedUtc,
                WorkCalendarId = x.u.WorkCalendarId,
                WorkCalendarName = wc != null ? wc.Name : null
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<UserDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
