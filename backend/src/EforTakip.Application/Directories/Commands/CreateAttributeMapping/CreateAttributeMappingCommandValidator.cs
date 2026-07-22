using FluentValidation;

namespace EforTakip.Application.Directories.Commands.CreateAttributeMapping;

public sealed class CreateAttributeMappingCommandValidator : AbstractValidator<CreateAttributeMappingCommand>
{
    public CreateAttributeMappingCommandValidator()
    {
        RuleFor(x => x.DirectoryId)
            .NotEmpty().WithMessage("Dizin kimliği zorunludur.");
        RuleFor(x => x.AdAttributeName)
            .NotEmpty().WithMessage("AD alan adı zorunludur.")
            .MaximumLength(150);
        RuleFor(x => x.SystemFieldName)
            .NotEmpty().WithMessage("Sistem alan adı zorunludur.")
            .MaximumLength(150);
        RuleFor(x => x.FieldType)
            .NotEmpty().WithMessage("Alan tipi zorunludur.")
            .MaximumLength(50);
    }
}
