using Hika.Api.Common;
using Hika.Application.Users;
using Hika.Application.Users.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hika.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var userId = await authService.RegisterAsync(request, cancellationToken);
        return CreatedAtAction(nameof(UsersController.GetPublicProfile), "Users", new { userId }, new { userId });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthTokenResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(request, GetClientIp(), GetDeviceInfo(), cancellationToken);
        return Ok(response);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthTokenResponse>> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.RefreshAsync(request.RefreshToken, GetClientIp(), GetDeviceInfo(), cancellationToken);
        return Ok(response);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(request.RefreshToken, cancellationToken);
        return NoContent();
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        await authService.VerifyEmailAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpPost("resend-verification-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendVerificationEmail(ResendVerificationEmailRequest request, CancellationToken cancellationToken)
    {
        await authService.ResendVerificationEmailAsync(request.Email, cancellationToken);
        return Accepted();
    }

    [HttpPost("request-phone-otp")]
    [Authorize]
    public async Task<IActionResult> RequestPhoneOtp(RequestPhoneOtpRequest request, CancellationToken cancellationToken)
    {
        await authService.RequestPhoneOtpAsync(User.GetUserId(), request.PhoneNumber, cancellationToken);
        return Accepted();
    }

    [HttpPost("verify-phone")]
    [Authorize]
    public async Task<IActionResult> VerifyPhone(VerifyPhoneRequest request, CancellationToken cancellationToken)
    {
        await authService.VerifyPhoneAsync(User.GetUserId(), request.Code, cancellationToken);
        return NoContent();
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        // Always 202, regardless of whether the email is registered — avoids account enumeration.
        await authService.ForgotPasswordAsync(request.Email, cancellationToken);
        return Accepted();
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.ResetPasswordAsync(request, cancellationToken);
        return NoContent();
    }

    private string? GetClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? GetDeviceInfo() => Request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua : null;
}
