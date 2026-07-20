using Asp.Versioning;
using EforTakip.Application.Common.Models;
using EforTakip.Application.Notifications.Commands.MarkNotificationAsRead;
using EforTakip.Application.Notifications.Dtos;
using EforTakip.Application.Notifications.Queries.GetNotifications;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EforTakip.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class NotificationsController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<NotificationDto>>> GetAll(
        [FromQuery] GetNotificationsQuery query, CancellationToken cancellationToken)
        => Ok(await mediator.Send(query, cancellationToken));

    [HttpPatch("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new MarkNotificationAsReadCommand(id), cancellationToken);
        return NoContent();
    }
}
