using FluentValidation;

namespace EforTakip.Application.Projects.Commands.UpdateProjectIssue;

public sealed class UpdateProjectIssueCommandValidator : AbstractValidator<UpdateProjectIssueCommand>
{
    public UpdateProjectIssueCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Sorun başlığı zorunludur.")
            .MaximumLength(200).WithMessage("Sorun başlığı en fazla 200 karakter olabilir.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Açıklama en fazla 2000 karakter olabilir.");

        RuleFor(x => x.Resolution)
            .MaximumLength(2000).WithMessage("Çözüm notu en fazla 2000 karakter olabilir.");
    }
}
