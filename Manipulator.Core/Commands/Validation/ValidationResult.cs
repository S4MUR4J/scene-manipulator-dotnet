namespace Manipulator.Core.Commands.Validation;

public record ValidationResult
{
    private ValidationResult(bool isValid, string? error)
    {
        IsValid = isValid;
        Error = error;
    }

    public bool IsValid { get; }

    public string? Error { get; }

    public static ValidationResult Ok() => new ValidationResult(isValid: true, error: null);

    public static ValidationResult Fail(string error) =>
        new ValidationResult(isValid: false, error: error);
}
