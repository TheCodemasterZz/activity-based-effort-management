using MediatR;

namespace EforTakip.Application.WorkLogs.Commands.DeleteWorkLog;

public sealed record DeleteWorkLogCommand(Guid Id) : IRequest;
