using EforTakip.Application.Activities.Dtos;
using MediatR;

namespace EforTakip.Application.Activities.Queries.GetActivityById;

public sealed record GetActivityByIdQuery(Guid ActivityId) : IRequest<ActivityDto>;
