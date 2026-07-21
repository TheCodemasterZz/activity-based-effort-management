using EforTakip.Application.Directories.Ldap;
using MediatR;

namespace EforTakip.Application.Directories.Commands.TestDirectoryConnection;

public sealed record TestDirectoryConnectionCommand(Guid DirectoryId) : IRequest<LdapConnectionTestResult>;
