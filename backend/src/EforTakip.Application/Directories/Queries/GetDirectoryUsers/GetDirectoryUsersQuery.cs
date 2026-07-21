using EforTakip.Application.Common.Models;
using EforTakip.Application.Directories.Dtos;
using MediatR;

namespace EforTakip.Application.Directories.Queries.GetDirectoryUsers;

public sealed class GetDirectoryUsersQuery : PaginationParams, IRequest<PagedResult<DirectoryUserDto>>
{
    public Guid? DirectoryId { get; set; }
    public string? SearchTerm { get; set; }
    public bool? OnlyActive { get; set; }
}
