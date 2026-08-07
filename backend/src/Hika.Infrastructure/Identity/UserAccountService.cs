using Hika.Application.Users.Ports;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Hika.Infrastructure.Identity;

/// <remarks>
/// Uses UserManager only, not SignInManager — SignInManager depends on ASP.NET Core's HTTP
/// abstractions (IHttpContextAccessor, cookie auth), which aren't available to a plain class
/// library like Hika.Infrastructure. Lockout tracking (AccessFailedAsync/ResetAccessFailedCountAsync)
/// is replicated manually below to get the same behavior SignInManager.CheckPasswordSignInAsync
/// provides in a Web SDK project.
/// </remarks>
public sealed class UserAccountService(UserManager<ApplicationUser> userManager) : IUserAccountService
{
    public async Task<AccountOperationResult> CreateAccountAsync(
        string email, string password, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            UserName = email,
            Email = email,
        };

        var result = await userManager.CreateAsync(user, password);

        return result.Succeeded
            ? AccountOperationResult.Success()
            : AccountOperationResult.Failure(result.Errors.Select(e => e.Description));
    }

    public async Task<UserAccountSummary?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);
        return user is null ? null : ToSummary(user);
    }

    public async Task<UserAccountSummary?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : ToSummary(user);
    }

    public async Task<IReadOnlyList<UserAccountSummary>> FindByIdsAsync(
        IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return [];
        }

        var users = await userManager.Users.Where(u => userIds.Contains(u.Id)).ToListAsync(cancellationToken);
        return users.Select(ToSummary).ToList();
    }

    public async Task<CredentialCheckResult> CheckPasswordAsync(
        Guid userId, string password, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return CredentialCheckResult.InvalidCredentials;
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return CredentialCheckResult.LockedOut;
        }

        var passwordValid = await userManager.CheckPasswordAsync(user, password);

        if (passwordValid)
        {
            if (userManager.Options.Lockout.AllowedForNewUsers)
            {
                await userManager.ResetAccessFailedCountAsync(user);
            }

            return CredentialCheckResult.Success;
        }

        await userManager.AccessFailedAsync(user);
        return await userManager.IsLockedOutAsync(user) ? CredentialCheckResult.LockedOut : CredentialCheckResult.InvalidCredentials;
    }

    public async Task MarkEmailConfirmedAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return;
        }

        user.EmailConfirmed = true;
        await userManager.UpdateAsync(user);
    }

    public async Task<AccountOperationResult> SetPasswordAsync(
        Guid userId, string newPassword, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return AccountOperationResult.Failure(["Account not found."]);
        }

        if (await userManager.HasPasswordAsync(user))
        {
            var removeResult = await userManager.RemovePasswordAsync(user);
            if (!removeResult.Succeeded)
            {
                return AccountOperationResult.Failure(removeResult.Errors.Select(e => e.Description));
            }
        }

        var addResult = await userManager.AddPasswordAsync(user, newPassword);

        return addResult.Succeeded
            ? AccountOperationResult.Success()
            : AccountOperationResult.Failure(addResult.Errors.Select(e => e.Description));
    }

    private static UserAccountSummary ToSummary(ApplicationUser user) =>
        new(user.Id, user.Email!, user.EmailConfirmed);
}
