using EforTakip.Application.Employees.Dtos;
using EforTakip.Domain.Employees;
using Mapster;

namespace EforTakip.Application.Employees;

public sealed class EmployeeMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Employee, EmployeeDto>();
    }
}
