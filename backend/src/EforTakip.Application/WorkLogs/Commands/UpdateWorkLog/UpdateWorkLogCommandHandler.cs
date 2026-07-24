using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Projects;
using EforTakip.Application.WorkLogApprovals;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.WorkLogs;
using MediatR;
using DomainActivity = EforTakip.Domain.Activities.Activity;

namespace EforTakip.Application.WorkLogs.Commands.UpdateWorkLog;

public sealed class UpdateWorkLogCommandHandler(
    IRepository<WorkLog> workLogRepository,
    IProjectRepository projectRepository,
    IRepository<DomainActivity> activityRepository,
    IApplicationDbContext db,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateWorkLogCommand>
{
    public async Task Handle(UpdateWorkLogCommand request, CancellationToken cancellationToken)
    {
        var log = await workLogRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(WorkLog), request.Id);

        await WorkLogValidationHelper.ValidateAsync(
            projectRepository, activityRepository, db,
            request.ProjectId, request.UserId,
            request.ActivityL1Id, request.ActivityL2Id, cancellationToken);

        await WorkLogApprovalGuard.EnsureRangeNotApprovedAsync(
            db, request.UserId, request.WorkDate, request.WorkDate, log.EntryType, cancellationToken);

        log.Update(
            request.UserId, request.ProjectId,
            request.ActivityL1Id, request.ActivityL2Id, request.WorkDate, request.Hours, request.Description);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
