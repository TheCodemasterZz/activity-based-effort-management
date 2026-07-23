using FluentValidation;

namespace EforTakip.Application.Roles.Commands.RevokePermission;

public sealed class RevokePermissionCommandValidator : AbstractValidator<RevokePermissionCommand>
{
    public RevokePermissionCommandValidator()
    {
        RuleFor(x => x.PermissionKey).NotEmpty().WithMessage("İzin anahtarı zorunludur.");
    }
}
