using FluentValidation;

namespace EforTakip.Application.Roles.Commands.GrantPermission;

public sealed class GrantPermissionCommandValidator : AbstractValidator<GrantPermissionCommand>
{
    public GrantPermissionCommandValidator()
    {
        RuleFor(x => x.PermissionKey).NotEmpty().WithMessage("İzin anahtarı zorunludur.");
    }
}
