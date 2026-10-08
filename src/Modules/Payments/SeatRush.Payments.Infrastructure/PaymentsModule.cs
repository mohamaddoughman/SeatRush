using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SeatRush.Payments.Infrastructure;

public static class PaymentsModule
{
    /// <summary>
    /// Registers everything the Payments module needs. Called once by the Api (the composition root).
    /// </summary>
    /// <remarks>
    /// Registration lives in Infrastructure because it is the only layer that can see the whole module
    /// (Domain, Application, and its own persistence/adapters). The Api calls this single entry point
    /// instead of knowing the module's internals.
    /// </remarks>
    public static IServiceCollection AddPaymentsModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        return services;
    }
}
