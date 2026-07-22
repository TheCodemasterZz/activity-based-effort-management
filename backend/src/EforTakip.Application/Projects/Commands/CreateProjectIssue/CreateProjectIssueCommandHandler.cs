using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Projects;
using MediatR;

namespace EforTakip.Application.Projects.Commands.CreateProjectIssue;

public sealed class CreateProjectIssueCommandHandler(
    IProjectRepository projectRepository, IRepository<ProjectIssue> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateProjectIssueCommand, Guid>
{
    public async Task<Guid> Handle(CreateProjectIssueCommand request, CancellationToken cancellationToken)
    {
        _ = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Project), request.ProjectId);

        var issue = ProjectIssue.Create(
            request.ProjectId, request.Title, request.Description, request.Priority,
            request.OwnerEmployeeId, request.DueDate);

        await repository.AddAsync(issue, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return issue.Id;
    }
}
