using MediatR;

namespace EforTakip.Application.Leaves.Commands.DeleteLeave;

public sealed record DeleteLeaveCommand(Guid Id) : IRequest;
