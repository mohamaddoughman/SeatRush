using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SeatRush.Api.ModelBinding;

/// <summary>
/// Whether a parameter's value comes through MVC's value providers (query, route, form), which is where the
/// strict binders apply. Body values go through System.Text.Json, and headers and services have their own binders.
/// </summary>
internal static class ValueProviderSources
{
    public static bool Includes(BindingSource? source) =>
        source is null
        || source == BindingSource.ModelBinding

        // CanAcceptDataFrom throws for a composite source, and ModelBinding (form + route + query) is one.
        || (source is not CompositeBindingSource && BindingSource.ModelBinding.CanAcceptDataFrom(source));
}
