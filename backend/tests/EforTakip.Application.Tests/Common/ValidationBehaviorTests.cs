using EforTakip.Application.Common.Behaviors;
using FluentAssertions;
using FluentValidation;
using MediatR;
using ValidationException = EforTakip.Application.Common.Exceptions.ValidationException;

namespace EforTakip.Application.Tests.Common;

/// <summary>
/// MediatR 12+ ile <c>IRequest</c> (yanıtsız) artık <c>IRequest&lt;Unit&gt;</c>'ten türemiyor.
/// Behavior'ın kısıtı <c>IRequest&lt;TResponse&gt;</c> olursa yanıtsız komutlarda doğrulama
/// sessizce atlanır — bu testler o regresyonu yakalar.
/// </summary>
public class ValidationBehaviorTests
{
    private sealed record CommandWithResponse(string Name) : IRequest<Guid>;

    private sealed record CommandWithoutResponse(string Name) : IRequest;

    private sealed class CommandWithResponseValidator : AbstractValidator<CommandWithResponse>
    {
        public CommandWithResponseValidator() => RuleFor(x => x.Name).NotEmpty();
    }

    private sealed class CommandWithoutResponseValidator : AbstractValidator<CommandWithoutResponse>
    {
        public CommandWithoutResponseValidator() => RuleFor(x => x.Name).NotEmpty();
    }

    [Fact]
    public async Task Handle_CommandWithResponse_RunsValidation()
    {
        var behavior = new ValidationBehavior<CommandWithResponse, Guid>(
            [new CommandWithResponseValidator()]);

        var act = async () => await behavior.Handle(
            new CommandWithResponse(string.Empty),
            _ => Task.FromResult(Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_CommandWithoutResponse_RunsValidation()
    {
        var behavior = new ValidationBehavior<CommandWithoutResponse, Unit>(
            [new CommandWithoutResponseValidator()]);

        var act = async () => await behavior.Handle(
            new CommandWithoutResponse(string.Empty),
            _ => Task.FromResult(Unit.Value),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsNext()
    {
        var behavior = new ValidationBehavior<CommandWithoutResponse, Unit>(
            [new CommandWithoutResponseValidator()]);
        var nextCalled = false;

        await behavior.Handle(
            new CommandWithoutResponse("gecerli"),
            _ =>
            {
                nextCalled = true;
                return Task.FromResult(Unit.Value);
            },
            CancellationToken.None);

        nextCalled.Should().BeTrue();
    }
}
