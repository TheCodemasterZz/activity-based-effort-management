using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Projects;
using EforTakip.Domain.Users;
using MediatR;

namespace EforTakip.Application.Projects.Commands.AssignUserToProject;

public sealed class AssignUserToProjectCommandHandler(
    IProjectRepository projectRepository,
    IRepository<User> userRepository,
    IApplicationDbContext db,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AssignUserToProjectCommand>
{
    public async Task Handle(AssignUserToProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Project), request.ProjectId);

        _ = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        var assignment = project.AssignUser(request.UserId);

        db.ProjectUserAssignments.Add(assignment);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
