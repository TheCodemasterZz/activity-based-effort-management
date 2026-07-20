using FluentValidation;

namespace EforTakip.Application.Activities.Commands.CreateActivity;

public sealed class CreateActivityCommandValidator : AbstractValidator<CreateActivityCommand>
{
    public CreateActivityCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Aktivite adı zorunludur.")
            .MaximumLength(200).WithMessage("Aktivite adı en fazla 200 karakter olabilir.");
    }
}
