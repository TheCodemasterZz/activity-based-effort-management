using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Employees;
using MediatR;

namespace EforTakip.Application.Employees.Commands.CreateEmployee;

public sealed class CreateEmployeeCommandHandler(IRepository<Employee> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateEmployeeCommand, Guid>
{
    public async Task<Guid> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = Employee.Create(request.Name, request.Email, request.WorkCalendarId);

        await repository.AddAsync(employee, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return employee.Id;
    }
}
