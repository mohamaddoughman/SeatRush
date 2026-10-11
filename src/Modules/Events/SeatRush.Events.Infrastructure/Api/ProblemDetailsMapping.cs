using System.Diagnostics;
using System.Text.Json;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SeatRush.Shared.Results;

namespace SeatRush.Events.Infrastructure.Api;

/// <summary>
/// Turns failures into RFC 7807 problem details, the same way for every action.
/// </summary>
/// <remarks>
/// Internal to the Events module while it's the only module with controllers. When a second module needs it,
/// it moves into a shared web project (ADR 0002): the mapping rules are one piece of knowledge, not one per module.
/// </remarks>
internal static class ProblemDetailsMapping
{
    /// <summary>
    /// A handler's <see cref="Error"/> as problem details, with the stable <see cref="Error.Code"/> in a
    /// <c>code</c> extension so clients can branch on it instead of on the message text.
    /// </summary>
    public static ObjectResult ProblemFor(this ControllerBase controller, Error error)
    {
        var statusCode = StatusCodeFor(error.Type);

        // Created through the factory so it gets the same title, type link and traceId as every other problem response.
        var problem = controller.ProblemDetailsFactory.CreateProblemDetails(
            controller.HttpContext, statusCode, detail: error.Message);
        problem.Extensions["code"] = error.Code;

        return new ObjectResult(problem)
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" },
        };
    }

    /// <summary>
    /// FluentValidation failures as <see cref="ValidationProblemDetails"/>: every field error at once, keyed by
    /// the field's JSON name (camelCase, as clients send it).
    /// </summary>
    public static ActionResult ValidationProblemFor(this ControllerBase controller, ValidationResult validation)
    {
        var modelState = new ModelStateDictionary();

        foreach (var failure in validation.Errors)
        {
            modelState.AddModelError(JsonNamingPolicy.CamelCase.ConvertName(failure.PropertyName), failure.ErrorMessage);
        }

        return controller.ValidationProblem(modelState);
    }

    // Failure is 400, not 500: it's an expected outcome. 500 stays reserved for real bugs and outages, so alerts
    // on server errors stay meaningful.
    private static int StatusCodeFor(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Failure => StatusCodes.Status400BadRequest,
        _ => throw new UnreachableException($"ErrorType '{type}' has no HTTP status code."),
    };
}
