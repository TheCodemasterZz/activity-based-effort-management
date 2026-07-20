using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Holidays;
using MediatR;

namespace EforTakip.Application.Holidays.Commands.CreateHoliday;

public sealed class CreateHolidayCommandHandler(IRepository<Holiday> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateHolidayCommand, Guid>
{
    public async Task<Guid> Handle(CreateHolidayCommand request, CancellationToken cancellationToken)
    {
        var holiday = Holiday.Create(request.Date, request.Name);

        await repository.AddAsync(holiday, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return holiday.Id;
    }
}
