using Eshhanly.Domain.Common;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Eshhanly.Web.Endpoints;

/// <summary>Maps application <see cref="Result"/>s to HTTP responses (RFC 7807 problem details on failure).</summary>
internal static class ResultExtensions
{
    public static IResult ToHttp(this Result result) =>
        result.IsSuccess ? TypedResults.NoContent() : Problem(result.Error);

    public static IResult ToHttp<T>(this Result<T> result, Func<T, IResult>? onSuccess = null) =>
        result.IsSuccess ? onSuccess?.Invoke(result.Value) ?? TypedResults.Ok(result.Value) : Problem(result.Error);

    private static ProblemHttpResult Problem(Error error) => TypedResults.Problem(
        title: error.Code,
        detail: error.Message,
        statusCode: error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest,
        });
}

/// <summary>Turns FluentValidation failures thrown by the MediatR pipeline into a 400 validation problem.</summary>
internal sealed class ValidationExceptionFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (ValidationException ex)
        {
            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return TypedResults.ValidationProblem(errors);
        }
    }
}
