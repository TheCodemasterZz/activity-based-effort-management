using EforTakip.Application.Customers.Dtos;
using MediatR;

namespace EforTakip.Application.Customers.Queries.GetCustomerById;

public sealed record GetCustomerByIdQuery(Guid CustomerId) : IRequest<CustomerDto>;
