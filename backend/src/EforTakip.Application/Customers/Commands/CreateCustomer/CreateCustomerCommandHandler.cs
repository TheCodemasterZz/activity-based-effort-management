using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Customers;
using MediatR;

namespace EforTakip.Application.Customers.Commands.CreateCustomer;

public sealed class CreateCustomerCommandHandler(IRepository<Customer> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCustomerCommand, Guid>
{
    public async Task<Guid> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = Customer.Create(request.Name);

        await repository.AddAsync(customer, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return customer.Id;
    }
}
