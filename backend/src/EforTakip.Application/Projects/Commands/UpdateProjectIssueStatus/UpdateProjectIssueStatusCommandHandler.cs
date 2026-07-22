using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Projects;
using MediatR;

namespace EforTakip.Application.Projects.Commands.UpdateProjectIssueStatus;

public sealed class UpdateProjectIssueStatusCommandHandler(IRepository<ProjectIssue> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProjectIssueStatusCommand>
{
    public async Task Handle(UpdateProjectIssueStatusCommand request, CancellationToken cancellationToken)
    {
        var issue = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ProjectIssue), request.Id);

        issue.SetStatus(request.Status);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
