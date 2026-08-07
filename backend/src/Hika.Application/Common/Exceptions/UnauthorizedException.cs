namespace Hika.Application.Common.Exceptions;

/// <summary>Maps to HTTP 401. Invalid credentials, expired/revoked/reused refresh tokens.</summary>
public sealed class UnauthorizedException(string message) : Exception(message);
