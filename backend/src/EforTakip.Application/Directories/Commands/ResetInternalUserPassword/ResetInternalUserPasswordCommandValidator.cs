using FluentValidation;

namespace EforTakip.Application.Directories.Commands.ResetInternalUserPassword;

public sealed class ResetInternalUserPasswordCommandValidator
    : AbstractValidator<ResetInternalUserPasswordCommand>
{
    public ResetInternalUserPasswordCommandValidator()
    {
        RuleFor(x => x.DirectoryUserId).NotEmpty().WithMessage("Kullanıcı seçilmelidir.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Yeni şifre zorunludur.")
            .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalıdır.")
            .MaximumLength(128).WithMessage("Şifre en fazla 128 karakter olabilir.");
    }
}
