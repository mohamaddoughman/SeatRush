using SeatRush.Booking.Infrastructure;
using SeatRush.Events.Infrastructure;
using SeatRush.Identity.Infrastructure;
using SeatRush.Notifications.Infrastructure;
using SeatRush.Payments.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();

// Unhandled exceptions and empty error responses become RFC 7807 problem details.
builder.Services.AddProblemDetails();

builder.Services
    .AddIdentityModule(builder.Configuration)
    .AddEventsModule(builder.Configuration)
    .AddBookingModule(builder.Configuration)
    .AddPaymentsModule(builder.Configuration)
    .AddNotificationsModule(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapControllers();
app.MapDefaultEndpoints();

await app.RunAsync();
