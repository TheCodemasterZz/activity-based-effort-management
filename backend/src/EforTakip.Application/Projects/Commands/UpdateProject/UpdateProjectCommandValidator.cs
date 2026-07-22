using FluentValidation;

namespace EforTakip.Application.Projects.Commands.UpdateProject;

public sealed class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Proje adı zorunludur.")
            .MaximumLength(200).WithMessage("Proje adı en fazla 200 karakter olabilir.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Açıklama en fazla 2000 karakter olabilir.");

        RuleFor(x => x.Sponsor)
            .MaximumLength(200).WithMessage("Sponsor adı en fazla 200 karakter olabilir.");

        RuleFor(x => x.StrategicGoal)
            .MaximumLength(500).WithMessage("Stratejik hedef en fazla 500 karakter olabilir.");
    }
}
