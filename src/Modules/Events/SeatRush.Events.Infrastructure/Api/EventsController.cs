using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SeatRush.Events.Application.Features.CreateEvent;
using SeatRush.Events.Application.Features.GetEventById;
using SeatRush.Events.Application.Features.ListEvents;
using SeatRush.Events.Domain.Events;
using SeatRush.Shared.Messaging;
using SeatRush.Shared.Results;

namespace SeatRush.Events.Infrastructure.Api;

/// <summary>
/// HTTP entry point of the Events module: the module's inbound adapter (ADR 0002).
/// Each action is validate → dispatch to one handler → map the result. No business logic here.
/// </summary>
/// <remarks>
/// Public because MVC only discovers public controllers. Handlers are injected per action ([FromServices]), so a
/// request builds only the handler it uses.
/// </remarks>
[ApiController]
[Route("events")]
public sealed class EventsController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CreatedEventResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        CreateEventCommand command,
        [FromServices] IValidator<CreateEventCommand> validator,
        [FromServices] ICommandHandler<CreateEventCommand, Result<Guid>> handler,
        CancellationToken cancellationToken)
    {
        // Explicit for now; moves into a shared action filter at the third validated endpoint (roadmap, 1.4).
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return this.ValidationProblemFor(validation);
        }

        var result = await handler.HandleAsync(command, cancellationToken);
        if (result.IsFailure)
        {
            return this.ProblemFor(result.Error);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value }, new CreatedEventResponse(result.Value));
    }

    [HttpGet]
    [ProducesResponseType<PagedResponse<EventResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        [FromServices] IValidator<ListEventsQuery> validator,
        [FromServices] IQueryHandler<ListEventsQuery, Result<PagedResponse<EventResponse>>> handler,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = ListEventsQuery.DefaultPageSize,
        EventStatus? status = null,
        DateTimeOffset? startsFrom = null,
        DateTimeOffset? startsBefore = null)
    {
        var query = new ListEventsQuery(page, pageSize, status, startsFrom, startsBefore);

        // Explicit for now; moves into a shared action filter at the third validated endpoint (roadmap, 1.4).
        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            return this.ValidationProblemFor(validation);
        }

        var result = await handler.HandleAsync(query, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : this.ProblemFor(result.Error);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<EventResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromServices] IQueryHandler<GetEventByIdQuery, Result<EventResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetEventByIdQuery(id), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : this.ProblemFor(result.Error);
    }
}
