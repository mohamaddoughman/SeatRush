using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SeatRush.Api.ModelBinding;

/// <summary>
/// Binds an enum from the query string, route or form only by one of its names (case-insensitive), e.g.
/// <c>?status=Published</c>.
/// </summary>
/// <remarks>
/// MVC's default enum binding also accepts numbers (<c>?status=1</c>), which makes the enum's numbers part of the
/// API, and comma lists, which it ORs together even on a non-[Flags] enum: <c>?status=Draft,Cancelled</c> silently
/// becomes <c>Cancelled</c>. Names are the contract (responses return them too), so anything else is a 400.
/// </remarks>
internal sealed class StrictEnumModelBinder(Type enumType) : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (value == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, value);

        var name = Enum.GetNames(enumType)
            .FirstOrDefault(candidate => string.Equals(candidate, value.FirstValue?.Trim(), StringComparison.OrdinalIgnoreCase));

        if (name is not null)
        {
            bindingContext.Result = ModelBindingResult.Success(Enum.Parse(enumType, name));
        }
        else if (string.IsNullOrWhiteSpace(value.FirstValue) && bindingContext.ModelMetadata.IsReferenceOrNullableType)
        {
            // "?status=" on an optional filter means "no filter".
            bindingContext.Result = ModelBindingResult.Success(null);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(
                bindingContext.ModelName,
                $"Must be one of: {string.Join(", ", Enum.GetNames(enumType))}.");
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Uses <see cref="StrictEnumModelBinder"/> for enums (and their nullable form) that come through model binding
/// (query, route, form). [Flags] enums are left to MVC, where combining names is the point.
/// </summary>
internal sealed class StrictEnumModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        var type = context.Metadata.UnderlyingOrModelType;

        return type.IsEnum
            && !type.IsDefined(typeof(FlagsAttribute), inherit: false)
            && ValueProviderSources.Includes(context.BindingInfo.BindingSource)
                ? new StrictEnumModelBinder(type)
                : null;
    }
}
