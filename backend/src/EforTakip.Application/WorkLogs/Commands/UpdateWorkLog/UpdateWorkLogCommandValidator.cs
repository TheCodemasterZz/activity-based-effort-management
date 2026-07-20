using FluentValidation;

namespace EforTakip.Application.WorkLogs.Commands.UpdateWorkLog;

public sealed class UpdateWorkLogCommandValidator : AbstractValidator<UpdateWorkLogCommand>
{
    public UpdateWorkLogCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.ActivityL1Id).NotEmpty();
        RuleFor(x => x.ActivityL2Id).NotEmpty().WithMessage("Activity L2 seçimi zorunludur.");

        RuleFor(x => x.WorkDate)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow)).WithMessage("Tarih gelecekte olamaz.");

        RuleFor(x => x.Hours)
            .GreaterThan(0).WithMessage("Efor saati 0'dan büyük olmalıdır.")
            .LessThanOrEqualTo(24).WithMessage("Efor saati 24'ten büyük olamaz.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Açıklama zorunludur.")
            .MaximumLength(1000).WithMessage("Açıklama en fazla 1000 karakter olabilir.");
    }
}
