using FluentValidation;

namespace EforTakip.Application.ValueStreams.Commands.AddStage;

public sealed class AddStageCommandValidator : AbstractValidator<AddStageCommand>
{
    public AddStageCommandValidator()
    {
        RuleFor(x => x.ValueStreamId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Aşama adı zorunludur.")
            .MaximumLength(200).WithMessage("Aşama adı en fazla 200 karakter olabilir.");

        RuleFor(x => x.Order).GreaterThanOrEqualTo(0);
    }
}
