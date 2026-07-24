using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Projects;
using EforTakip.Application.WorkLogApprovals;
using EforTakip.Domain.WorkLogs;
using MediatR;
using DomainActivity = EforTakip.Domain.Activities.Activity;

namespace EforTakip.Application.WorkLogs.Commands.LogWork;

public sealed class LogWorkCommandHandler(
    IProjectRepository projectRepository,
    IRepository<DomainActivity> activityRepository,
    IApplicationDbContext db,
    IUnitOfWork unitOfWork)
    : IRequestHandler<LogWorkCommand, IReadOnlyCollection<Guid>>
{
    public async Task<IReadOnlyCollection<Guid>> Handle(LogWorkCommand request, CancellationToken cancellationToken)
    {
        await WorkLogValidationHelper.ValidateAsync(
            projectRepository, activityRepository,
            request.ProjectId, request.UserId,
            request.ActivityL1Id, request.ActivityL2Id, cancellationToken);

        await WorkLogApprovalGuard.EnsureRangeNotApprovedAsync(
            db, request.UserId, request.StartDate, request.EndDate, request.EntryType, cancellationToken);

        var logs = new List<WorkLog>();
        for (var date = request.StartDate; date <= request.EndDate; date = date.AddDays(1))
        {
            logs.Add(WorkLog.Create(
                request.UserId, request.ProjectId,
                request.ActivityL1Id, request.ActivityL2Id, date, request.Hours, request.Description,
                request.EntryType));
        }

        db.WorkLogs.AddRange(logs);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return logs.Select(l => l.Id).ToList();
    }
}
