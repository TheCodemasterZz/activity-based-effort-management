using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.ValueStreams;
using MediatR;

namespace EforTakip.Application.ValueStreams.Commands.CreateValueStream;

public sealed class CreateValueStreamCommandHandler(IValueStreamRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateValueStreamCommand, Guid>
{
    public async Task<Guid> Handle(CreateValueStreamCommand request, CancellationToken cancellationToken)
    {
        var valueStream = ValueStream.Create(request.Name, request.Description);

        await repository.AddAsync(valueStream, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return valueStream.Id;
    }
}
