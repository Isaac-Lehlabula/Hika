using Hika.Application.Users.Dtos;

namespace Hika.Application.Users;

public interface IAuthService
{
    Task<Guid> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<AuthTokenResponse> LoginAsync(LoginRequest request, string? ipAddress, string? deviceInfo, CancellationToken cancellationToken);

    Task<AuthTokenResponse> RefreshAsync(string refreshToken, string? ipAddress, string? deviceInfo, CancellationToken cancellationToken);

    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken);

    Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken);

    Task ResendVerificationEmailAsync(string email, CancellationToken cancellationToken);

    Task RequestPhoneOtpAsync(Guid userId, string phoneNumber, CancellationToken cancellationToken);

    Task VerifyPhoneAsync(Guid userId, string code, CancellationToken cancellationToken);

    Task ForgotPasswordAsync(string email, CancellationToken cancellationToken);

    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken);
}
