using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Roles.Commands.CreateRole;
using EforTakip.Application.Tests.Directories.Commands;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Roles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace EforTakip.Application.Tests.Roles.Commands;

public class CreateRoleCommandHandlerTests : IAsyncDisposable
{
    private readonly IRepository<Role> _repository = Substitute.For<IRepository<Role>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TestDbContext _db;

    public CreateRoleCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"create-role-tests-{Guid.NewGuid()}")
            .Options;
        _db = new TestDbContext(options);

        _repository.AddAsync(Arg.Any<Role>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                _db.Roles.Add(callInfo.Arg<Role>());
                return Task.CompletedTask;
            });
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo => _db.SaveChangesAsync(callInfo.Arg<CancellationToken>()));
    }

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    private CreateRoleCommandHandler CreateHandler() => new(_db, _repository, _unitOfWork);

    [Fact]
    public async Task Handle_CreatesRole()
    {
        var result = await CreateHandler().Handle(
            new CreateRoleCommand("Proje Yöneticisi", "Açıklama"), CancellationToken.None);

        result.Should().NotBeEmpty();
        (await _db.Roles.FindAsync(result))!.Name.Should().Be("Proje Yöneticisi");
    }

    [Fact]
    public async Task Handle_DuplicateName_Throws()
    {
        await CreateHandler().Handle(new CreateRoleCommand("Proje Yöneticisi", null), CancellationToken.None);

        var act = async () => await CreateHandler().Handle(
            new CreateRoleCommand("Proje Yöneticisi", null), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleValidationException>();
    }
}
