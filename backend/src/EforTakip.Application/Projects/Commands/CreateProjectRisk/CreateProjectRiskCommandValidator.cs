using FluentValidation;

namespace EforTakip.Application.Projects.Commands.CreateProjectRisk;

public sealed class CreateProjectRiskCommandValidator : AbstractValidator<CreateProjectRiskCommand>
{
    public CreateProjectRiskCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Risk başlığı zorunludur.")
            .MaximumLength(200).WithMessage("Risk başlığı en fazla 200 karakter olabilir.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Açıklama en fazla 2000 karakter olabilir.");

        RuleFor(x => x.Probability)
            .InclusiveBetween(1, 5).WithMessage("Olasılık 1 ile 5 arasında olmalıdır.");

        RuleFor(x => x.Impact)
            .InclusiveBetween(1, 5).WithMessage("Etki 1 ile 5 arasında olmalıdır.");

        RuleFor(x => x.MitigationPlan)
            .MaximumLength(2000).WithMessage("Azaltım planı en fazla 2000 karakter olabilir.");
    }
}
