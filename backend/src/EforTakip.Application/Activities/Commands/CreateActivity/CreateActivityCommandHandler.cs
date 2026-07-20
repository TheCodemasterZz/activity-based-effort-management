using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Exceptions;
using MediatR;
using DomainActivity = EforTakip.Domain.Activities.Activity;

namespace EforTakip.Application.Activities.Commands.CreateActivity;

public sealed class CreateActivityCommandHandler(IRepository<DomainActivity> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateActivityCommand, Guid>
{
    public async Task<Guid> Handle(CreateActivityCommand request, CancellationToken cancellationToken)
    {
        if (request.ParentActivityId is { } parentId)
        {
            var parent = await repository.GetByIdAsync(parentId, cancellationToken)
                ?? throw new NotFoundException(nameof(DomainActivity), parentId);

            if (parent.ParentActivityId is not null)
                throw new BusinessRuleValidationException(
                    "Sadece üst seviye aktivitelerin altına alt aktivite eklenebilir.");
        }

        var activity = DomainActivity.Create(request.Name, request.Description, request.ParentActivityId);

        await repository.AddAsync(activity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return activity.Id;
    }
}
