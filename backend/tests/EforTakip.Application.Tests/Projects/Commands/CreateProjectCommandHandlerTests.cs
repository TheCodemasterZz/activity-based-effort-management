using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Projects;
using EforTakip.Application.Projects.Commands.CreateProject;
using EforTakip.Domain.Projects;
using FluentAssertions;
using NSubstitute;

namespace EforTakip.Application.Tests.Projects.Commands;

public class CreateProjectCommandHandlerTests
{
    private readonly IProjectRepository _repository = Substitute.For<IProjectRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Handle_WithValidCommand_PersistsProjectAndReturnsId()
    {
        var handler = new CreateProjectCommandHandler(_repository, _unitOfWork);
        var command = new CreateProjectCommand("Efor Takip Platformu", "Açıklama");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        await _repository.Received(1).AddAsync(Arg.Is<Project>(p => p.Name == "Efor Takip Platformu"), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
