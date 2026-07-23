using FluentValidation;

namespace EforTakip.Application.Roles.Commands.AssignRoleToUser;

public sealed class AssignRoleToUserCommandValidator : AbstractValidator<AssignRoleToUserCommand>
{
    public AssignRoleToUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("Kullanıcı belirtilmelidir.");
        RuleFor(x => x.RoleId).NotEmpty().WithMessage("Rol belirtilmelidir.");
    }
}
