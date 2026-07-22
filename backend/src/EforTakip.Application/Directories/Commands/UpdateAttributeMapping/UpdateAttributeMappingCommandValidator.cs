using FluentValidation;

namespace EforTakip.Application.Directories.Commands.UpdateAttributeMapping;

public sealed class UpdateAttributeMappingCommandValidator : AbstractValidator<UpdateAttributeMappingCommand>
{
    public UpdateAttributeMappingCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.AdAttributeName).NotEmpty().WithMessage("AD alan adı zorunludur.").MaximumLength(150);
        RuleFor(x => x.SystemFieldName).NotEmpty().WithMessage("Sistem alan adı zorunludur.").MaximumLength(150);
        RuleFor(x => x.FieldType).NotEmpty().WithMessage("Alan tipi zorunludur.").MaximumLength(50);
    }
}
