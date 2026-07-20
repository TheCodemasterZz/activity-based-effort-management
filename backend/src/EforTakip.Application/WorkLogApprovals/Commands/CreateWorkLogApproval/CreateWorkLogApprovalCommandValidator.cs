using System;
using FluentValidation;

namespace EforTakip.Application.WorkLogApprovals.Commands.CreateWorkLogApproval;

public sealed class CreateWorkLogApprovalCommandValidator : AbstractValidator<CreateWorkLogApprovalCommand>
{
    public CreateWorkLogApprovalCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();

        RuleFor(x => x.PeriodStart)
            .Must(d => d.DayOfWeek == DayOfWeek.Monday)
            .WithMessage("Onay dönemi Pazartesi gününden başlamalıdır.");

        RuleFor(x => x.PeriodEnd)
            .Equal(x => x.PeriodStart.AddDays(6))
            .WithMessage("Onay dönemi tam bir hafta (Pazartesi–Pazar) olmalıdır.");
    }
}
