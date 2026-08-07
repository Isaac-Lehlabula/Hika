using Hika.Application.Common.Exceptions;
using Hika.Application.Common.Options;
using Hika.Application.Common.Persistence;
using Hika.Application.Common.Security;
using Hika.Application.Users.Dtos;
using Hika.Application.Users.Ports;
using Hika.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hika.Application.Users;

public sealed class AuthService(
    IAppDbContext db,
    IUserAccountService userAccounts,
    IJwtTokenGenerator jwtTokenGenerator,
    IEmailSender emailSender,
    ISmsSender smsSender,
    IOptions<NotificationLinksOptions> notificationLinks,
    ILogger<AuthService> logger) : IAuthService
{
    private static readonly TimeSpan EmailVerificationLifetime = TimeSpan.FromHours(24);
    private static readonly TimeSpan PasswordResetLifetime = TimeSpan.FromHours(1);
    private static readonly TimeSpan PhoneOtpLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    public async Task<Guid> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var existing = await userAccounts.FindByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException("An account with this email already exists.");
        }

        var creationResult = await userAccounts.CreateAccountAsync(request.Email, request.Password, cancellationToken);
        if (!creationResult.Succeeded)
        {
            throw new AppValidationException(new Dictionary<string, string[]>
            {
                ["password"] = creationResult.Errors.ToArray(),
            });
        }

        var account = await userAccounts.FindByEmailAsync(request.Email, cancellationToken)
            ?? throw new InvalidOperationException("Account was created but could not be re-read.");

        var profile = UserProfile.Create(account.UserId, request.FirstName, request.LastName);
        if (request.PhoneNumber is not null)
        {
            profile.SetPhoneNumber(request.PhoneNumber);
        }

        db.UserProfiles.Add(profile);
        await db.SaveChangesAsync(cancellationToken);

        await SendEmailVerificationAsync(account.UserId, account.Email, cancellationToken);

        return account.UserId;
    }

    public async Task<AuthTokenResponse> LoginAsync(
        LoginRequest request, string? ipAddress, string? deviceInfo, CancellationToken cancellationToken)
    {
        var account = await userAccounts.FindByEmailAsync(request.Email, cancellationToken);
        if (account is null)
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        var checkResult = await userAccounts.CheckPasswordAsync(account.UserId, request.Password, cancellationToken);

        if (checkResult == CredentialCheckResult.LockedOut)
        {
            throw new UnauthorizedException("This account is temporarily locked due to multiple failed login attempts. Try again later.");
        }

        if (checkResult != CredentialCheckResult.Success)
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        return await IssueTokenPairAsync(account.UserId, account.Email, ipAddress, deviceInfo, cancellationToken);
    }

    public async Task<AuthTokenResponse> RefreshAsync(
        string refreshToken, string? ipAddress, string? deviceInfo, CancellationToken cancellationToken)
    {
        var hash = TokenHasher.Hash(refreshToken);
        var existing = await db.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hash, cancellationToken);

        if (existing is null)
        {
            throw new UnauthorizedException("Invalid refresh token.");
        }

        if (existing.RevokedAtUtc is not null)
        {
            // A revoked token being presented again means it was already rotated away and is
            // now being replayed — classic theft signature. Kill every active session for this
            // user rather than trusting any single token again.
            logger.LogWarning("Revoked refresh token replay detected for user {UserId}; revoking all sessions", existing.UserId);
            var activeTokens = await db.RefreshTokens
                .Where(rt => rt.UserId == existing.UserId && rt.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);
            foreach (var token in activeTokens)
            {
                token.Revoke();
            }

            await db.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException("Refresh token has already been used. All sessions have been signed out for safety.");
        }

        if (DateTimeOffset.UtcNow >= existing.ExpiresAtUtc)
        {
            throw new UnauthorizedException("Refresh token has expired.");
        }

        var account = await userAccounts.FindByIdAsync(existing.UserId, cancellationToken)
            ?? throw new UnauthorizedException("Account no longer exists.");

        var newRawToken = SecureTokenGenerator.GenerateUrlSafeToken();
        var newExpiresAt = DateTimeOffset.UtcNow.Add(RefreshTokenLifetime);
        var newToken = new RefreshToken(account.UserId, TokenHasher.Hash(newRawToken), newExpiresAt, ipAddress, deviceInfo);

        existing.Revoke(newToken.TokenHash);
        db.RefreshTokens.Add(newToken);
        await db.SaveChangesAsync(cancellationToken);

        var accessToken = jwtTokenGenerator.GenerateAccessToken(account.UserId, account.Email);

        return new AuthTokenResponse
        {
            AccessToken = accessToken.Token,
            AccessTokenExpiresAtUtc = accessToken.ExpiresAtUtc,
            RefreshToken = newRawToken,
            RefreshTokenExpiresAtUtc = newExpiresAt,
        };
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var hash = TokenHasher.Hash(refreshToken);
        var existing = await db.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hash, cancellationToken);

        if (existing is { RevokedAtUtc: null })
        {
            existing.Revoke();
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        var hash = TokenHasher.Hash(request.Token);
        var token = await db.EmailVerificationTokens
            .Where(t => t.UserId == request.UserId && t.TokenHash == hash)
            .FirstOrDefaultAsync(cancellationToken);

        if (token is null || !token.IsUsable)
        {
            throw new AppValidationException("token", "This verification link is invalid or has expired.");
        }

        token.MarkUsed();
        await userAccounts.MarkEmailConfirmedAsync(request.UserId, cancellationToken);

        var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.Id == request.UserId, cancellationToken);
        profile?.MarkEmailVerified();

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ResendVerificationEmailAsync(string email, CancellationToken cancellationToken)
    {
        var account = await userAccounts.FindByEmailAsync(email, cancellationToken);

        // Deliberately silent on "account not found" / "already verified" — avoids account enumeration.
        if (account is null || account.EmailConfirmed)
        {
            return;
        }

        await SendEmailVerificationAsync(account.UserId, account.Email, cancellationToken);
    }

    public async Task RequestPhoneOtpAsync(Guid userId, string phoneNumber, CancellationToken cancellationToken)
    {
        var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.Id == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProfile), userId);

        profile.SetPhoneNumber(phoneNumber);

        var rawCode = SecureTokenGenerator.GenerateNumericOtp();
        var code = new PhoneVerificationCode(
            userId, phoneNumber, TokenHasher.Hash(rawCode), DateTimeOffset.UtcNow.Add(PhoneOtpLifetime));

        db.PhoneVerificationCodes.Add(code);
        await db.SaveChangesAsync(cancellationToken);

        await smsSender.SendAsync(
            phoneNumber, $"Your Hika verification code is {rawCode}. It expires in 10 minutes.", cancellationToken);
    }

    public async Task VerifyPhoneAsync(Guid userId, string code, CancellationToken cancellationToken)
    {
        var candidate = await db.PhoneVerificationCodes
            .Where(c => c.UserId == userId && c.UsedAtUtc == null)
            .OrderByDescending(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (candidate is null || !candidate.IsUsable)
        {
            throw new AppValidationException("code", "No active verification code. Request a new one.");
        }

        if (candidate.CodeHash != TokenHasher.Hash(code))
        {
            candidate.RecordAttempt();
            await db.SaveChangesAsync(cancellationToken);
            throw new AppValidationException("code", "Incorrect code.");
        }

        candidate.MarkUsed();

        var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.Id == userId, cancellationToken);
        profile?.MarkPhoneVerified();

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ForgotPasswordAsync(string email, CancellationToken cancellationToken)
    {
        var account = await userAccounts.FindByEmailAsync(email, cancellationToken);
        if (account is null)
        {
            // Silent — never reveal whether an email is registered.
            return;
        }

        var rawToken = SecureTokenGenerator.GenerateUrlSafeToken();
        var token = new PasswordResetToken(account.UserId, TokenHasher.Hash(rawToken), DateTimeOffset.UtcNow.Add(PasswordResetLifetime));
        db.PasswordResetTokens.Add(token);
        await db.SaveChangesAsync(cancellationToken);

        var link = $"{notificationLinks.Value.BaseUrl}/reset-password?userId={account.UserId}&token={Uri.EscapeDataString(rawToken)}";
        await emailSender.SendAsync(
            account.Email,
            "Reset your Hika password",
            $"<p>Click the link below to reset your password. This link expires in 1 hour.</p><p><a href=\"{link}\">{link}</a></p>",
            cancellationToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var hash = TokenHasher.Hash(request.Token);
        var token = await db.PasswordResetTokens
            .Where(t => t.UserId == request.UserId && t.TokenHash == hash)
            .FirstOrDefaultAsync(cancellationToken);

        if (token is null || !token.IsUsable)
        {
            throw new AppValidationException("token", "This reset link is invalid or has expired.");
        }

        var result = await userAccounts.SetPasswordAsync(request.UserId, request.NewPassword, cancellationToken);
        if (!result.Succeeded)
        {
            throw new AppValidationException(new Dictionary<string, string[]>
            {
                ["newPassword"] = result.Errors.ToArray(),
            });
        }

        token.MarkUsed();

        // A password reset invalidates every existing session as a safety measure.
        var activeTokens = await db.RefreshTokens
            .Where(rt => rt.UserId == request.UserId && rt.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var refreshToken in activeTokens)
        {
            refreshToken.Revoke();
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthTokenResponse> IssueTokenPairAsync(
        Guid userId, string email, string? ipAddress, string? deviceInfo, CancellationToken cancellationToken)
    {
        var accessToken = jwtTokenGenerator.GenerateAccessToken(userId, email);

        var rawRefreshToken = SecureTokenGenerator.GenerateUrlSafeToken();
        var refreshExpiresAt = DateTimeOffset.UtcNow.Add(RefreshTokenLifetime);
        var refreshToken = new RefreshToken(userId, TokenHasher.Hash(rawRefreshToken), refreshExpiresAt, ipAddress, deviceInfo);

        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync(cancellationToken);

        return new AuthTokenResponse
        {
            AccessToken = accessToken.Token,
            AccessTokenExpiresAtUtc = accessToken.ExpiresAtUtc,
            RefreshToken = rawRefreshToken,
            RefreshTokenExpiresAtUtc = refreshExpiresAt,
        };
    }

    private async Task SendEmailVerificationAsync(Guid userId, string email, CancellationToken cancellationToken)
    {
        var rawToken = SecureTokenGenerator.GenerateUrlSafeToken();
        var token = new EmailVerificationToken(userId, TokenHasher.Hash(rawToken), DateTimeOffset.UtcNow.Add(EmailVerificationLifetime));
        db.EmailVerificationTokens.Add(token);
        await db.SaveChangesAsync(cancellationToken);

        var link = $"{notificationLinks.Value.BaseUrl}/verify-email?userId={userId}&token={Uri.EscapeDataString(rawToken)}";
        await emailSender.SendAsync(
            email,
            "Verify your Hika email address",
            $"<p>Welcome to Hika! Click the link below to verify your email. This link expires in 24 hours.</p><p><a href=\"{link}\">{link}</a></p>",
            cancellationToken);
    }
}
