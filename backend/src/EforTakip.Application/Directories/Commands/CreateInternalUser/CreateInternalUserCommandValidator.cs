using FluentValidation;

namespace EforTakip.Application.Directories.Commands.CreateInternalUser;

public sealed class CreateInternalUserCommandValidator : AbstractValidator<CreateInternalUserCommand>
{
    public CreateInternalUserCommandValidator()
    {
        RuleFor(x => x.DirectoryId).NotEmpty().WithMessage("Dizin seçilmelidir.");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Kullanıcı adı zorunludur.")
            .MaximumLength(150).WithMessage("Kullanıcı adı en fazla 150 karakter olabilir.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre zorunludur.")
            .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalıdır.")
            .MaximumLength(128).WithMessage("Şifre en fazla 128 karakter olabilir.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}
