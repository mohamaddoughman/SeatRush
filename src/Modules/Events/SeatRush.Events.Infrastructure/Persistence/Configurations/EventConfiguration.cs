using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeatRush.Events.Domain.Events;

namespace SeatRush.Events.Infrastructure.Persistence.Configurations;

internal sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events");

        // The domain assigns the id in Event.Create, so the database must not generate one.
        builder.HasKey(@event => @event.Id);
        builder.Property(@event => @event.Id).ValueGeneratedNever();

        // Lengths come from the domain constants, so the column, the validator and the domain can't disagree.
        builder.Property(@event => @event.Title)
            .HasMaxLength(Event.TitleMaxLength)
            .IsRequired();

        builder.Property(@event => @event.Description)
            .HasMaxLength(Event.DescriptionMaxLength);

        // Stored as int (EF Core's default for enums): the numbers in EventStatus are part of the stored data.
        builder.Property(@event => @event.Status);
    }
}
