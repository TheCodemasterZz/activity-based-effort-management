using EforTakip.Domain.WorkLogs;
using FluentValidation;

namespace EforTakip.Application.WorkLogs.Commands.LogWork;

public sealed class LogWorkCommandValidator : AbstractValidator<LogWorkCommand>
{
    public LogWorkCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.ActivityL1Id).NotEmpty();
        RuleFor(x => x.ActivityL2Id).NotEmpty().WithMessage("Activity L2 seçimi zorunludur.");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("Bitiş tarihi başlangıç tarihinden önce olamaz.");

        // Gerçekleşen (Actual) efor gelecekte loglanamaz; planlanan (Planned) efor için ise
        // gelecek tarih zaten planlamanın asıl amacı olduğundan bu kural uygulanmaz.
        RuleFor(x => x.EndDate)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow)).WithMessage("Bitiş tarihi gelecekte olamaz.")
            .When(x => x.EntryType == WorkLogEntryType.Actual);

        RuleFor(x => x.Hours)
            .GreaterThan(0).WithMessage("Efor saati 0'dan büyük olmalıdır.")
            .LessThanOrEqualTo(24).WithMessage("Efor saati 24'ten büyük olamaz.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Açıklama zorunludur.")
            .MaximumLength(1000).WithMessage("Açıklama en fazla 1000 karakter olabilir.");
    }
}
