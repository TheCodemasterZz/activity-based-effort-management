using FluentValidation;

namespace EforTakip.Application.Projects.Commands.AssignEmployeeToProject;

public sealed class AssignEmployeeToProjectCommandValidator : AbstractValidator<AssignEmployeeToProjectCommand>
{
    public AssignEmployeeToProjectCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
    }
}
