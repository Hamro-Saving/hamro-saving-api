using HamroSavings.SharedKernel;

namespace HamroSavings.Api.Extensions;

public static class ResultExtensions
{
    public static IResult Match<T>(
        this Result<T> result,
        Func<T, IResult> onSuccess,
        Func<Result, IResult> onFailure)
    {
        return result.IsSuccess ? onSuccess(result.Value) : onFailure(result);
    }

    public static IResult Match(
        this Result result,
        Func<IResult> onSuccess,
        Func<Result, IResult> onFailure)
    {
        return result.IsSuccess ? onSuccess() : onFailure(result);
    }
}
