using EforTakip.Application.Customers.Dtos;
using EforTakip.Domain.Customers;
using Mapster;

namespace EforTakip.Application.Customers;

public sealed class CustomerMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Customer, CustomerDto>();
    }
}
