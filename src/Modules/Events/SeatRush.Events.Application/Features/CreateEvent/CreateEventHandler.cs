using SeatRush.Events.Application.Abstractions;
using SeatRush.Events.Domain.Events;
using SeatRush.Shared.Messaging;
using SeatRush.Shared.Results;

namespace SeatRush.Events.Application.Features.CreateEvent;

public sealed class CreateEventHandler(
    IEventRepository eventRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICommandHandler<CreateEventCommand, Result<Guid>>
{
    public async Task<Result<Guid>> HandleAsync(CreateEventCommand command, CancellationToken cancellationToken)
    {
        var result = Event.Create(
            command.Title,
            command.Description,
            command.StartsAt,
            command.EndsAt,
            timeProvider.GetUtcNow());

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        eventRepository.Add(result.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value.Id);
    }
}
