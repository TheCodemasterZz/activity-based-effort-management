using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.WorkLogApprovals;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.WorkLogApprovals.Commands.CreateWorkLogApproval;

public sealed class CreateWorkLogApprovalCommandHandler(IApplicationDbContext db, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateWorkLogApprovalCommand, Guid>
{
    public async Task<Guid> Handle(CreateWorkLogApprovalCommand request, CancellationToken cancellationToken)
    {
        var hasOverlap = await db.WorkLogApprovals.AnyAsync(
            a => a.EmployeeId == request.EmployeeId
                && a.PeriodStart <= request.PeriodEnd && a.PeriodEnd >= request.PeriodStart,
            cancellationToken);

        if (hasOverlap)
            throw new BusinessRuleValidationException("Bu dönemin bir kısmı zaten onaylanmış.");

        var approval = WorkLogApproval.Create(
            request.EmployeeId, request.PeriodType, request.PeriodStart, request.PeriodEnd, request.Description);

        var logsToApprove = await db.EmployeeWorkLogs
            .Where(l => l.EmployeeId == request.EmployeeId
                && l.WorkDate >= request.PeriodStart && l.WorkDate <= request.PeriodEnd
                && l.ApprovalId == null)
            .ToListAsync(cancellationToken);

        foreach (var log in logsToApprove)
            log.MarkApproved(approval.Id);

        db.WorkLogApprovals.Add(approval);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return approval.Id;
    }
}
