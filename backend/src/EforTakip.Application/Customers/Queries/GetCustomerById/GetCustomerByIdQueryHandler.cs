using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Customers.Dtos;
using EforTakip.Domain.Customers;
using EforTakip.Domain.Exceptions;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Customers.Queries.GetCustomerById;

public sealed class GetCustomerByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetCustomerByIdQuery, CustomerDto>
{
    public async Task<CustomerDto> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var customer = await db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), request.CustomerId);

        return customer.Adapt<CustomerDto>();
    }
}
