namespace Hika.Application.Common.Exceptions;

/// <summary>Maps to HTTP 404. Thrown by application services when a requested entity doesn't exist.</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with key '{key}' was not found.")
    {
    }

    public NotFoundException(string message)
        : base(message)
    {
    }
}
