using MediatR;

namespace EforTakip.Application.Customers.Commands.CreateCustomer;

public sealed record CreateCustomerCommand(string Name) : IRequest<Guid>;
