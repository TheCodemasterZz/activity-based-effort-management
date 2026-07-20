using MediatR;

namespace EforTakip.Application.Holidays.Commands.CreateHoliday;

public sealed record CreateHolidayCommand(DateOnly Date, string Name) : IRequest<Guid>;
