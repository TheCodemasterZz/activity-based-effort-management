using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Projects;
using MediatR;

namespace EforTakip.Application.Projects.Commands.DeleteProjectIssue;

public sealed class DeleteProjectIssueCommandHandler(IRepository<ProjectIssue> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteProjectIssueCommand>
{
    public async Task Handle(DeleteProjectIssueCommand request, CancellationToken cancellationToken)
    {
        var issue = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ProjectIssue), request.Id);

        repository.Remove(issue);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
