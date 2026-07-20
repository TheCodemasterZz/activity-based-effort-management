using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.ValueStreams;
using MediatR;

namespace EforTakip.Application.ValueStreams.Commands.AddStage;

public sealed class AddStageCommandHandler(
    IValueStreamRepository repository, IApplicationDbContext db, IUnitOfWork unitOfWork)
    : IRequestHandler<AddStageCommand, Guid>
{
    public async Task<Guid> Handle(AddStageCommand request, CancellationToken cancellationToken)
    {
        var valueStream = await repository.GetByIdAsync(request.ValueStreamId, cancellationToken)
            ?? throw new NotFoundException(nameof(ValueStream), request.ValueStreamId);

        var stage = valueStream.AddStage(request.Name, request.Order);

        // Bkz. LogEffort/AssignCustomerToProject: client-generated Guid anahtarlı yeni
        // child kayıtlar EF Core tarafından "Modified" işaretlenmesin diye açıkça eklenir.
        db.ValueStreamStages.Add(stage);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return stage.Id;
    }
}
