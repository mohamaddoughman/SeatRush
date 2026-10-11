using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SeatRush.Api.ModelBinding;

/// <summary>
/// The query-string, route and form counterpart of <see cref="Json.StrictDateTimeOffsetConverter"/>: a
/// <see cref="DateTimeOffset"/> is accepted only as defined by <see cref="StrictDateTimeOffset"/>.
/// </summary>
/// <remarks>
/// JSON bodies go through System.Text.Json, but query values go through model binding, whose default would read
/// <c>?startsFrom=2030-05-01T00:00</c> as the server's local time. A rejected value becomes a 400 for that field.
/// </remarks>
internal sealed class StrictDateTimeOffsetModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (value == ValueProviderResult.None)
        {
            // Not sent: the parameter keeps its default (null for the optional filters), as with MVC's own binders.
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, value);

        if (string.IsNullOrWhiteSpace(value.FirstValue) && bindingContext.ModelMetadata.IsReferenceOrNullableType)
        {
            // "?startsFrom=" on an optional filter means "no filter".
            bindingContext.Result = ModelBindingResult.Success(null);
        }
        else if (StrictDateTimeOffset.TryParse(value.FirstValue, out var dateTimeOffset))
        {
            bindingContext.Result = ModelBindingResult.Success(dateTimeOffset);
        }
        else
        {
            // '+' in a query string means a space, so a "+02:00" offset must be sent as "%2B02:00".
            bindingContext.ModelState.TryAddModelError(
                bindingContext.ModelName,
                $"{StrictDateTimeOffset.ErrorMessage} In a query string, encode '+' as %2B.");
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Uses <see cref="StrictDateTimeOffsetModelBinder"/> for <see cref="DateTimeOffset"/> and its nullable form, for
/// values that come through model binding (query, route, form). Body and header values keep their own binders.
/// </summary>
internal sealed class StrictDateTimeOffsetModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context) =>
        context.Metadata.UnderlyingOrModelType == typeof(DateTimeOffset)
        && ValueProviderSources.Includes(context.BindingInfo.BindingSource)
            ? new StrictDateTimeOffsetModelBinder()
            : null;
}
