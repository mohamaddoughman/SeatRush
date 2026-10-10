using SeatRush.Events.Application.Features.CreateEvent;
using SeatRush.Events.Domain.Events;

namespace SeatRush.Events.UnitTests.Features;

public sealed class CreateEventValidatorTests
{
    private static readonly DateTimeOffset StartsAt = new(2026, 11, 20, 20, 0, 0, TimeSpan.Zero);

    private static readonly CreateEventCommand ValidCommand = new("Rock Night", "Live music", StartsAt, StartsAt.AddHours(3));

    private readonly CreateEventValidator _validator = new();

    [Fact]
    public void Accepts_a_valid_command()
    {
        var result = _validator.Validate(ValidCommand);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_an_empty_title(string title)
    {
        var result = _validator.Validate(ValidCommand with { Title = title });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateEventCommand.Title));
    }

    [Fact]
    public void Rejects_a_title_longer_than_the_domain_allows()
    {
        var result = _validator.Validate(ValidCommand with { Title = new string('a', Event.TitleMaxLength + 1) });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateEventCommand.Title));
    }

    [Fact]
    public void Accepts_a_padded_title_whose_trimmed_length_fits()
    {
        var result = _validator.Validate(ValidCommand with { Title = $"  {new string('a', Event.TitleMaxLength)}  " });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Accepts_a_padded_description_whose_trimmed_length_fits()
    {
        var result = _validator.Validate(ValidCommand with { Description = $"  {new string('a', Event.DescriptionMaxLength)}  " });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Rejects_a_description_longer_than_the_domain_allows()
    {
        var result = _validator.Validate(ValidCommand with { Description = new string('a', Event.DescriptionMaxLength + 1) });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateEventCommand.Description));
    }

    [Fact]
    public void Rejects_missing_dates()
    {
        var result = _validator.Validate(ValidCommand with { StartsAt = default, EndsAt = default });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateEventCommand.StartsAt));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateEventCommand.EndsAt));
    }

    [Fact]
    public void Rejects_an_end_that_is_not_after_the_start()
    {
        var result = _validator.Validate(ValidCommand with { EndsAt = StartsAt });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateEventCommand.EndsAt));
    }

    [Fact]
    public void Reports_every_problem_at_once()
    {
        var result = _validator.Validate(new CreateEventCommand("", new string('a', Event.DescriptionMaxLength + 1), StartsAt, StartsAt));

        Assert.Equal(3, result.Errors.Count);
    }
}
