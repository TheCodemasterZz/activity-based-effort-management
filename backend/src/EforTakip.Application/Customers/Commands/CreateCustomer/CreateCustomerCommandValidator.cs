using FluentValidation;

namespace EforTakip.Application.Customers.Commands.CreateCustomer;

public sealed class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Müşteri adı zorunludur.")
            .MaximumLength(200).WithMessage("Müşteri adı en fazla 200 karakter olabilir.");
    }
}
