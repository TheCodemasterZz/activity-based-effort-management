using FluentValidation;

namespace EforTakip.Application.Projects.Commands.AssignUserToProject;

public sealed class AssignUserToProjectCommandValidator : AbstractValidator<AssignUserToProjectCommand>
{
    public AssignUserToProjectCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
