using FluentValidation;

namespace EforTakip.Application.Projects.Commands.UpdateProjectTask;

public sealed class UpdateProjectTaskCommandValidator : AbstractValidator<UpdateProjectTaskCommand>
{
    public UpdateProjectTaskCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Görev adı zorunludur.")
            .MaximumLength(200).WithMessage("Görev adı en fazla 200 karakter olabilir.");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("Bitiş tarihi başlangıç tarihinden önce olamaz.");

        RuleFor(x => x.EstimatedEffortHours)
            .GreaterThanOrEqualTo(0).WithMessage("Tahmini efor negatif olamaz.");
    }
}
