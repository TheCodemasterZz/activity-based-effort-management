using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Directories.Ldap;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using MediatR;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Application.Directories.Commands.TestDirectoryConnection;

public sealed class TestDirectoryConnectionCommandHandler(
    IRepository<Directory> repository, ILdapService ldapService)
    : IRequestHandler<TestDirectoryConnectionCommand, LdapConnectionTestResult>
{
    public async Task<LdapConnectionTestResult> Handle(
        TestDirectoryConnectionCommand request, CancellationToken cancellationToken)
    {
        var directory = await repository.GetByIdAsync(request.DirectoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(Directory), request.DirectoryId);

        if (directory.Source != DirectorySource.ActiveDirectory)
            throw new BusinessRuleValidationException("Yalnızca Active Directory dizinleri için bağlantı testi yapılabilir.");

        return await ldapService.TestConnectionAsync(directory, cancellationToken);
    }
}
