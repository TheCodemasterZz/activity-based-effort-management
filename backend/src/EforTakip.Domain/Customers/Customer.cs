using EforTakip.Domain.Common;
using EforTakip.Domain.Exceptions;

namespace EforTakip.Domain.Customers;

public sealed class Customer : Entity, IAggregateRoot
{
    public string Name { get; private set; } = default!;

    private Customer()
    {
        // EF Core
    }

    public static Customer Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleValidationException("Müşteri adı boş olamaz.");

        return new Customer
        {
            Name = name.Trim()
        };
    }
}
