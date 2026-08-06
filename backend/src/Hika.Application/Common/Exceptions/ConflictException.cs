namespace Hika.Application.Common.Exceptions;

/// <summary>Maps to HTTP 409. E.g. not enough seats available, duplicate email, stale concurrency token.</summary>
public sealed class ConflictException(string message) : Exception(message);
