using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Roles.Commands.DeleteRole;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Roles;
using FluentAssertions;
using NSubstitute;

namespace EforTakip.Application.Tests.Roles.Commands;

public class DeleteRoleCommandHandlerTests
{
    private readonly IRepository<Role> _repository = Substitute.For<IRepository<Role>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private DeleteRoleCommandHandler CreateHandler() => new(_repository, _unitOfWork);

    [Fact]
    public async Task Handle_ExistingRole_Removes()
    {
        var role = Role.Create("Proje Yöneticisi", null, false);
        _repository.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);

        await CreateHandler().Handle(new DeleteRoleCommand(role.Id), CancellationToken.None);

        _repository.Received(1).Remove(role);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownRole_ThrowsNotFound()
    {
        var act = async () => await CreateHandler().Handle(
            new DeleteRoleCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_SystemAdminRole_ThrowsBusinessRule()
    {
        var role = Role.Create("Sistem Yöneticisi", null, isSystemAdmin: true);
        _repository.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);

        var act = async () => await CreateHandler().Handle(new DeleteRoleCommand(role.Id), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleValidationException>();
    }
}
