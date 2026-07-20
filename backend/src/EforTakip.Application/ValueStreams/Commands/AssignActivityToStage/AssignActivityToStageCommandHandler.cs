using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Activities;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.ValueStreams;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.ValueStreams.Commands.AssignActivityToStage;

public sealed class AssignActivityToStageCommandHandler(
    IApplicationDbContext db, IRepository<Activity> activityRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<AssignActivityToStageCommand>
{
    public async Task Handle(AssignActivityToStageCommand request, CancellationToken cancellationToken)
    {
        var stageExists = await db.ValueStreamStages
            .AnyAsync(s => s.Id == request.ValueStreamStageId, cancellationToken);
        if (!stageExists)
            throw new NotFoundException(nameof(ValueStreamStage), request.ValueStreamStageId);

        var activity = await activityRepository.GetByIdAsync(request.ActivityId, cancellationToken)
            ?? throw new NotFoundException(nameof(Activity), request.ActivityId);

        if (activity.ParentActivityId is not null)
            throw new BusinessRuleValidationException("Sadece üst seviye (L1) aktiviteler bir aşamaya atanabilir.");

        var alreadyAssigned = await db.StageActivityAssignments
            .AnyAsync(a => a.ValueStreamStageId == request.ValueStreamStageId && a.ActivityId == request.ActivityId,
                cancellationToken);
        if (alreadyAssigned)
            throw new BusinessRuleValidationException("Bu aktivite zaten bu aşamaya atanmış.");

        var assignment = StageActivityAssignment.Create(request.ValueStreamStageId, request.ActivityId);
        db.StageActivityAssignments.Add(assignment);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
