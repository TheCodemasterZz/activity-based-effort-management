using FluentValidation;

namespace EforTakip.Application.Holidays.Commands.CreateHoliday;

public sealed class CreateHolidayCommandValidator : AbstractValidator<CreateHolidayCommand>
{
    public CreateHolidayCommandValidator()
    {
        RuleFor(x => x.Date).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tatil adı zorunludur.")
            .MaximumLength(200).WithMessage("Tatil adı en fazla 200 karakter olabilir.");
    }
}
