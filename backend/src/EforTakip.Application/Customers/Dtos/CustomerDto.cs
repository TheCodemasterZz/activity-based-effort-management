namespace EforTakip.Application.Customers.Dtos;

public sealed class CustomerDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
}
