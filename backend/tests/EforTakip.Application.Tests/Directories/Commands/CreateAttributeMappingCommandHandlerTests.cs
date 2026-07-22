using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Directories.Commands.CreateAttributeMapping;
using EforTakip.Domain.Directories;
using FluentAssertions;
using NSubstitute;

namespace EforTakip.Application.Tests.Directories.Commands;

public class CreateAttributeMappingCommandHandlerTests
{
    private readonly IRepository<DirectoryAttributeMapping> _repository =
        Substitute.For<IRepository<DirectoryAttributeMapping>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Handle_CreatesMapping()
    {
        var handler = new CreateAttributeMappingCommandHandler(_repository, _unitOfWork);
        var command = new CreateAttributeMappingCommand(Guid.NewGuid(), "company", "Kurum", "text", true, 0);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        await _repository.Received(1).AddAsync(
            Arg.Is<DirectoryAttributeMapping>(m => m.AdAttributeName == "company" && m.SystemFieldName == "Kurum"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
