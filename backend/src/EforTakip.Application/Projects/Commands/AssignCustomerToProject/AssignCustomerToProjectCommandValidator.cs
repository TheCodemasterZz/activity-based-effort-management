using FluentValidation;

namespace EforTakip.Application.Projects.Commands.AssignCustomerToProject;

public sealed class AssignCustomerToProjectCommandValidator : AbstractValidator<AssignCustomerToProjectCommand>
{
    public AssignCustomerToProjectCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
    }
}
