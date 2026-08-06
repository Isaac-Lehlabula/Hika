namespace Hika.Application.Common.Exceptions;

/// <summary>Maps to HTTP 403. E.g. a user acting on a resource that isn't theirs.</summary>
public sealed class ForbiddenException(string message) : Exception(message);
