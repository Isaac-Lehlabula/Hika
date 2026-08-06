namespace Hika.Application.Common.Exceptions;

/// <summary>
/// Maps to HTTP 400 with field-level errors. Raised by application services for validation
/// rules that need data not available to a FluentValidation validator alone (e.g. uniqueness
/// checks against the database). Request-shape validation is handled earlier, by the
/// FluentValidation action filter, and never reaches this far.
/// </summary>
public sealed class AppValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public AppValidationException(string propertyName, string message)
        : base("One or more validation errors occurred.")
    {
        Errors = new Dictionary<string, string[]> { [propertyName] = [message] };
    }

    public AppValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }
}
