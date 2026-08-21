using HamroSavings.SharedKernel;
using Microsoft.AspNetCore.Mvc;

namespace HamroSavings.Api.Infrastructure;

public static class CustomResults
{
    public static IResult Problem(Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Cannot create problem from success result.");
        }

        return result.Error.Type switch
        {
            ErrorType.NotFound => Results.NotFound(CreateProblemDetails(result.Error, StatusCodes.Status404NotFound)),
            ErrorType.Conflict => Results.Conflict(CreateProblemDetails(result.Error, StatusCodes.Status409Conflict)),
            ErrorType.Validation => Results.BadRequest(CreateProblemDetails(result.Error, StatusCodes.Status400BadRequest)),
            ErrorType.Forbidden => Results.Json(CreateProblemDetails(result.Error, StatusCodes.Status403Forbidden), statusCode: StatusCodes.Status403Forbidden),
            _ => Results.Problem(CreateProblemDetails(result.Error, StatusCodes.Status500InternalServerError))
        };
    }

    private static ProblemDetails CreateProblemDetails(Error error, int statusCode)
    {
        return new ProblemDetails
        {
            Title = error.Code,
            Detail = error.Description,
            Status = statusCode
        };
    }
}
