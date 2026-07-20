using FluentValidation;

namespace EforTakip.Application.WorkCalendars.Commands.CreateWorkCalendar;

public sealed class CreateWorkCalendarCommandValidator : AbstractValidator<CreateWorkCalendarCommand>
{
    public CreateWorkCalendarCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Mesai takvimi adı zorunludur.")
            .MaximumLength(200).WithMessage("Mesai takvimi adı en fazla 200 karakter olabilir.");

        RuleFor(x => x.Days)
            .Must(days => days.Select(d => d.DayOfWeek).Distinct().Count() == 7)
            .WithMessage("Haftanın 7 günü için de tam olarak bir kayıt gönderilmelidir.");

        RuleForEach(x => x.Days).ChildRules(day =>
        {
            day.RuleFor(d => d.EndTime)
                .GreaterThan(d => d.StartTime)
                .When(d => d.IsWorkingDay)
                .WithMessage("Bitiş saati başlangıç saatinden sonra olmalıdır.");
        });
    }
}
