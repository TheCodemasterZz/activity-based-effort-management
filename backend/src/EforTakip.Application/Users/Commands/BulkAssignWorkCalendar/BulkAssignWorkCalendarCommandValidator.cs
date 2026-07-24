using FluentValidation;

namespace EforTakip.Application.Users.Commands.BulkAssignWorkCalendar;

public sealed class BulkAssignWorkCalendarCommandValidator : AbstractValidator<BulkAssignWorkCalendarCommand>
{
    public BulkAssignWorkCalendarCommandValidator()
    {
        RuleFor(x => x.UserIds).NotEmpty().WithMessage("En az bir kullanıcı seçilmelidir.");
        RuleFor(x => x.WorkCalendarId).NotEmpty().WithMessage("Mesai takvimi seçilmelidir.");
    }
}
