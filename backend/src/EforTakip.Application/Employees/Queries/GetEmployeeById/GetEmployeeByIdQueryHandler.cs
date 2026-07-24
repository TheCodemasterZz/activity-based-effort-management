using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Employees.Dtos;
using EforTakip.Domain.Employees;
using EforTakip.Domain.Exceptions;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Employees.Queries.GetEmployeeById;

public sealed class GetEmployeeByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetEmployeeByIdQuery, EmployeeDto>
{
    public async Task<EmployeeDto> Handle(GetEmployeeByIdQuery request, CancellationToken cancellationToken)
    {
        var employee = await db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Employee), request.UserId);

        return employee.Adapt<EmployeeDto>();
    }
}
