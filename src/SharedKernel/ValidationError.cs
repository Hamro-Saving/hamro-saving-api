namespace HamroSavings.SharedKernel;

public sealed class ValidationError
{
    public static readonly string Code = "Validation.General";
    public static readonly string Description = "One or more validation errors occurred";

    public ValidationError(string[] errors)
    {
        Errors = errors;
    }

    public string[] Errors { get; }

    public Error ToError() => Error.Validation(Code, string.Join("; ", Errors));
}
