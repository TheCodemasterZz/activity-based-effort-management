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
            a => a.UserId == request.UserId && a.EntryType == request.EntryType
                && a.PeriodStart <= request.PeriodEnd && a.PeriodEnd >= request.PeriodStart,
            cancellationToken);

        if (hasOverlap)
            throw new BusinessRuleValidationException("Bu dönemin bir kısmı zaten onaylanmış.");

        var approval = WorkLogApproval.Create(
            request.UserId, request.PeriodType, request.PeriodStart, request.PeriodEnd, request.Description,
            request.EntryType);

        var logsToApprove = await db.WorkLogs
            .Where(l => l.UserId == request.UserId && l.EntryType == request.EntryType
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
