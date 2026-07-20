using FluentValidation;

namespace EforTakip.Application.ValueStreams.Commands.AssignActivityToStage;

public sealed class AssignActivityToStageCommandValidator : AbstractValidator<AssignActivityToStageCommand>
{
    public AssignActivityToStageCommandValidator()
    {
        RuleFor(x => x.ValueStreamStageId).NotEmpty();
        RuleFor(x => x.ActivityId).NotEmpty();
    }
}
