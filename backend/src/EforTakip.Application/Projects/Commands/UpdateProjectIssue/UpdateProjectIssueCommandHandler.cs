using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Projects;
using MediatR;

namespace EforTakip.Application.Projects.Commands.UpdateProjectIssue;

public sealed class UpdateProjectIssueCommandHandler(IRepository<ProjectIssue> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProjectIssueCommand>
{
    public async Task Handle(UpdateProjectIssueCommand request, CancellationToken cancellationToken)
    {
        var issue = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ProjectIssue), request.Id);

        issue.Update(
            request.Title, request.Description, request.Priority,
            request.OwnerUserId, request.DueDate, request.Resolution);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
