using FluentValidation;

namespace EforTakip.Application.Employees.Commands.CreateEmployee;

public sealed class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Çalışan adı zorunludur.")
            .MaximumLength(200).WithMessage("Çalışan adı en fazla 200 karakter olabilir.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.WorkCalendarId).NotEmpty().WithMessage("Çalışanın bir mesai takvimi olmalıdır.");
    }
}
