using EforTakip.Application.Common.Models;
using EforTakip.Application.Users.Dtos;
using MediatR;

namespace EforTakip.Application.Users.Queries.GetUsers;

public sealed class GetUsersQuery : PaginationParams, IRequest<PagedResult<UserDto>>
{
    public Guid? DirectoryId { get; set; }
    public string? SearchTerm { get; set; }
    public bool? OnlyActive { get; set; }
    public bool? OnlyMissingWorkCalendar { get; set; }
}
