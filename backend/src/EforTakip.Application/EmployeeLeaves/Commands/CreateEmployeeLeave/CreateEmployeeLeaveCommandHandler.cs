using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.EmployeeLeaves;
using EforTakip.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.EmployeeLeaves.Commands.CreateEmployeeLeave;

public sealed class CreateEmployeeLeaveCommandHandler(IApplicationDbContext db, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateEmployeeLeaveCommand, Guid>
{
    public async Task<Guid> Handle(CreateEmployeeLeaveCommand request, CancellationToken cancellationToken)
    {
        var hasOverlap = await db.EmployeeLeaves.AnyAsync(
            l => l.EmployeeId == request.EmployeeId
                && l.StartDate <= request.EndDate && l.EndDate >= request.StartDate,
            cancellationToken);

        if (hasOverlap)
            throw new BusinessRuleValidationException("Bu tarih aralığında çalışanın zaten bir izin kaydı var.");

        var leave = EmployeeLeave.Create(
            request.EmployeeId, request.StartDate, request.EndDate,
            request.IsFullDay, request.StartTime, request.EndTime, request.Description);

        db.EmployeeLeaves.Add(leave);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return leave.Id;
    }
}
