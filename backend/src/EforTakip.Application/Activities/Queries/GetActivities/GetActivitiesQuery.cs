using EforTakip.Application.Activities.Dtos;
using EforTakip.Application.Common.Models;
using MediatR;

namespace EforTakip.Application.Activities.Queries.GetActivities;

public sealed class GetActivitiesQuery : PaginationParams, IRequest<PagedResult<ActivityDto>>
{
    public string? NameFilter { get; set; }

    /// <summary>Doluysa sadece bu aktivitenin alt aktiviteleri (L2) döner.</summary>
    public Guid? ParentActivityId { get; set; }

    /// <summary>True ise sadece üst seviye (L1, ParentActivityId'si olmayan) aktiviteler döner.</summary>
    public bool OnlyTopLevel { get; set; }
}
