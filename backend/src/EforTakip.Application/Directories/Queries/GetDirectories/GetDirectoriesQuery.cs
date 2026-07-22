using EforTakip.Application.Common.Models;
using EforTakip.Application.Directories.Dtos;
using MediatR;

namespace EforTakip.Application.Directories.Queries.GetDirectories;

public sealed class GetDirectoriesQuery : PaginationParams, IRequest<PagedResult<DirectoryDto>>
{
    public string? NameFilter { get; set; }
}
