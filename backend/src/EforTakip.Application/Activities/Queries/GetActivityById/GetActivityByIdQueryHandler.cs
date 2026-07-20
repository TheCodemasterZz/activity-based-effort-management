using EforTakip.Application.Activities.Dtos;
using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Exceptions;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using DomainActivity = EforTakip.Domain.Activities.Activity;

namespace EforTakip.Application.Activities.Queries.GetActivityById;

public sealed class GetActivityByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetActivityByIdQuery, ActivityDto>
{
    public async Task<ActivityDto> Handle(GetActivityByIdQuery request, CancellationToken cancellationToken)
    {
        var activity = await db.Activities
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.ActivityId, cancellationToken)
            ?? throw new NotFoundException(nameof(DomainActivity), request.ActivityId);

        return activity.Adapt<ActivityDto>();
    }
}
