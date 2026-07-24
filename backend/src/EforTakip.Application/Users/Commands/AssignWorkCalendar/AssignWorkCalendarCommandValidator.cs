using FluentValidation;

namespace EforTakip.Application.Users.Commands.AssignWorkCalendar;

public sealed class AssignWorkCalendarCommandValidator : AbstractValidator<AssignWorkCalendarCommand>
{
    public AssignWorkCalendarCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("Kullanıcı seçilmelidir.");
        RuleFor(x => x.WorkCalendarId).NotEmpty().WithMessage("Mesai takvimi seçilmelidir.");
    }
}
