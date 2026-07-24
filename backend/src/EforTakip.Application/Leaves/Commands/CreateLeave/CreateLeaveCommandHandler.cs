using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Leaves;
using EforTakip.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Leaves.Commands.CreateLeave;

public sealed class CreateLeaveCommandHandler(IApplicationDbContext db, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateLeaveCommand, Guid>
{
    public async Task<Guid> Handle(CreateLeaveCommand request, CancellationToken cancellationToken)
    {
        var hasOverlap = await db.Leaves.AnyAsync(
            l => l.UserId == request.UserId
                && l.StartDate <= request.EndDate && l.EndDate >= request.StartDate,
            cancellationToken);

        if (hasOverlap)
            throw new BusinessRuleValidationException("Bu tarih aralığında çalışanın zaten bir izin kaydı var.");

        var leave = Leave.Create(
            request.UserId, request.StartDate, request.EndDate,
            request.IsFullDay, request.StartTime, request.EndTime, request.Description);

        db.Leaves.Add(leave);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return leave.Id;
    }
}
