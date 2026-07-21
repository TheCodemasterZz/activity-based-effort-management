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
            request.ProjectId, request.CustomerId, request.EmployeeId,
            request.ActivityL1Id, request.ActivityL2Id, cancellationToken);

        await WorkLogApprovalGuard.EnsureRangeNotApprovedAsync(
            db, request.EmployeeId, request.StartDate, request.EndDate, request.EntryType, cancellationToken);

        var logs = new List<EmployeeWorkLog>();
        for (var date = request.StartDate; date <= request.EndDate; date = date.AddDays(1))
        {
            logs.Add(EmployeeWorkLog.Create(
                request.EmployeeId, request.ProjectId, request.CustomerId,
                request.ActivityL1Id, request.ActivityL2Id, date, request.Hours, request.Description,
                request.EntryType));
        }

        db.EmployeeWorkLogs.AddRange(logs);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return logs.Select(l => l.Id).ToList();
    }
}
