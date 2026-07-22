using EforTakip.Domain.Directories;
using FluentValidation;

namespace EforTakip.Application.Directories.Commands.UpdateDirectory;

public sealed class UpdateDirectoryCommandValidator : AbstractValidator<UpdateDirectoryCommand>
{
    public UpdateDirectoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Dizin adı zorunludur.")
            .MaximumLength(200).WithMessage("Dizin adı en fazla 200 karakter olabilir.");

        When(x => x.Source == DirectorySource.ActiveDirectory, () =>
        {
            RuleFor(x => x.Hostname).NotEmpty().WithMessage("Sunucu adresi (hostname) zorunludur.");
            RuleFor(x => x.BaseDn).NotEmpty().WithMessage("Base DN zorunludur.");
            RuleFor(x => x.Port).InclusiveBetween(1, 65535).WithMessage("Port 1-65535 aralığında olmalıdır.");
        });
    }
}
