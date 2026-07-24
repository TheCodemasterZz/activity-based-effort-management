using FluentValidation;

namespace EforTakip.Application.Leaves.Commands.CreateLeave;

public sealed class CreateLeaveCommandValidator : AbstractValidator<CreateLeaveCommand>
{
    public CreateLeaveCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("Bitiş tarihi başlangıç tarihinden önce olamaz.");

        RuleFor(x => x.StartDate)
            .Equal(x => x.EndDate)
            .When(x => !x.IsFullDay)
            .WithMessage("Kısmi (saatlik) izin yalnızca tek bir günü kapsayabilir.");

        RuleFor(x => x.StartTime)
            .NotNull()
            .When(x => !x.IsFullDay)
            .WithMessage("Kısmi izin için başlangıç saati zorunludur.");

        RuleFor(x => x.EndTime)
            .NotNull()
            .When(x => !x.IsFullDay)
            .WithMessage("Kısmi izin için bitiş saati zorunludur.");

        RuleFor(x => x)
            .Must(x => x.StartTime is null || x.EndTime is null || x.StartTime < x.EndTime)
            .WithMessage("Bitiş saati başlangıç saatinden sonra olmalıdır.")
            .OverridePropertyName("EndTime");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Açıklama en fazla 500 karakter olabilir.");
    }
}
