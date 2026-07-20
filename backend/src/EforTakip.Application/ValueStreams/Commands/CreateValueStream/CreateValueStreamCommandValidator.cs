using FluentValidation;

namespace EforTakip.Application.ValueStreams.Commands.CreateValueStream;

public sealed class CreateValueStreamCommandValidator : AbstractValidator<CreateValueStreamCommand>
{
    public CreateValueStreamCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Değer akışı adı zorunludur.")
            .MaximumLength(200).WithMessage("Değer akışı adı en fazla 200 karakter olabilir.");
    }
}
