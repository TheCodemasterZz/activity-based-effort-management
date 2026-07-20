using EforTakip.Application.Activities.Commands.CreateActivity;
using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Exceptions;
using FluentAssertions;
using NSubstitute;
using DomainActivity = EforTakip.Domain.Activities.Activity;

namespace EforTakip.Application.Tests.Activities.Commands;

public class CreateActivityCommandHandlerTests
{
    private readonly IRepository<DomainActivity> _repository = Substitute.For<IRepository<DomainActivity>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Handle_AsTopLevelActivity_CreatesActivity()
    {
        var handler = new CreateActivityCommandHandler(_repository, _unitOfWork);
        var command = new CreateActivityCommand("Geliştirme", null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        await _repository.Received(1).AddAsync(Arg.Is<DomainActivity>(a => a.Name == "Geliştirme"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AsSubActivityOfTopLevelParent_CreatesActivity()
    {
        var parent = DomainActivity.Create("Geliştirme", null, null);
        _repository.GetByIdAsync(parent.Id, Arg.Any<CancellationToken>()).Returns(parent);

        var handler = new CreateActivityCommandHandler(_repository, _unitOfWork);
        var command = new CreateActivityCommand("Kod İnceleme", null, parent.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_WithNonExistingParent_ThrowsNotFoundException()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DomainActivity?)null);

        var handler = new CreateActivityCommandHandler(_repository, _unitOfWork);
        var command = new CreateActivityCommand("Kod İnceleme", null, Guid.NewGuid());

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WithParentThatIsAlreadySubActivity_ThrowsBusinessRuleValidationException()
    {
        var topLevel = DomainActivity.Create("Geliştirme", null, null);
        var subActivity = DomainActivity.Create("Kod İnceleme", null, topLevel.Id);
        _repository.GetByIdAsync(subActivity.Id, Arg.Any<CancellationToken>()).Returns(subActivity);

        var handler = new CreateActivityCommandHandler(_repository, _unitOfWork);
        var command = new CreateActivityCommand("Alt Alt Aktivite", null, subActivity.Id);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleValidationException>();
    }
}
