namespace Hika.Application.Users.Ports;

/// <summary>
/// Everything AuthService needs from the identity/credential store, expressed in domain
/// terms (Guid userId, plain records) — hides ASP.NET Core Identity and ApplicationUser
/// (an Infrastructure/persistence concern, see docs/architecture.md §4) from Application.
/// </summary>
public interface IUserAccountService
{
    Task<AccountOperationResult> CreateAccountAsync(string email, string password, CancellationToken cancellationToken);

    Task<UserAccountSummary?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task<UserAccountSummary?> FindByIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Batch lookup for admin list views (see IAdminUserService) — avoids an
    /// N+1 FindByIdAsync per row when rendering a page of users.</summary>
    Task<IReadOnlyList<UserAccountSummary>> FindByIdsAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);

    Task<CredentialCheckResult> CheckPasswordAsync(Guid userId, string password, CancellationToken cancellationToken);

    Task MarkEmailConfirmedAsync(Guid userId, CancellationToken cancellationToken);

    Task<AccountOperationResult> SetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken);
}

public sealed record UserAccountSummary(Guid UserId, string Email, bool EmailConfirmed);

public sealed record AccountOperationResult(bool Succeeded, IReadOnlyList<string> Errors)
{
    public static AccountOperationResult Success() => new(true, []);

    public static AccountOperationResult Failure(IEnumerable<string> errors) => new(false, errors.ToList());
}

public enum CredentialCheckResult
{
    Success,
    InvalidCredentials,
    LockedOut,
}
