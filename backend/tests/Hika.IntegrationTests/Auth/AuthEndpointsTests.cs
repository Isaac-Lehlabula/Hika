using System.Net;
using System.Net.Http.Json;
using Hika.IntegrationTests.TestSupport;
using Shouldly;

namespace Hika.IntegrationTests.Auth;

public class AuthEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static string UniqueEmail([System.Runtime.CompilerServices.CallerMemberName] string testName = "") =>
        $"{testName.ToLowerInvariant()}-{Guid.NewGuid():N}@example.com";

    private async Task<(Guid UserId, string Email)> RegisterAndVerifyAsync(string email)
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "Passw0rd123",
            firstName = "Thabo",
            lastName = "Nkosi",
        });
        registerResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var userId = (await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>())!.UserId;

        var email1 = factory.EmailSender.SentEmails.Single(e => e.To == email);
        var token = CapturingEmailSender.ExtractQueryParam(email1.Body, "token");

        var verifyResponse = await _client.PostAsJsonAsync("/api/v1/auth/verify-email", new { userId, token });
        verifyResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        return (userId, email);
    }

    private async Task<TokenResponse> LoginAsync(string email, string password = "Passw0rd123")
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<TokenResponse>())!;
    }

    [Fact]
    public async Task Register_ThenVerifyEmail_ThenLogin_Succeeds()
    {
        var email = UniqueEmail();

        await RegisterAndVerifyAsync(email);
        var tokens = await LoginAsync(email);

        tokens.AccessToken.ShouldNotBeNullOrWhiteSpace();
        tokens.RefreshToken.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var email = UniqueEmail();
        await RegisterAndVerifyAsync(email);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "Passw0rd123",
            firstName = "Someone",
            lastName = "Else",
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var email = UniqueEmail();
        await RegisterAndVerifyAsync(email);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "WrongPassw0rd" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/users/me");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_WithValidToken_ReturnsOwnProfile()
    {
        var email = UniqueEmail();
        await RegisterAndVerifyAsync(email);
        var tokens = await LoginAsync(email);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/me");
        request.Headers.Authorization = new("Bearer", tokens.AccessToken);
        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<ProfileResponse>();
        profile!.Email.ShouldBe(email);
        profile.EmailVerified.ShouldBeTrue();
    }

    [Fact]
    public async Task Refresh_RotatesToken_AndRejectsReuseOfOldOne()
    {
        var email = UniqueEmail();
        await RegisterAndVerifyAsync(email);
        var tokens = await LoginAsync(email);

        var refreshResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = tokens.RefreshToken });
        refreshResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var rotated = await refreshResponse.Content.ReadFromJsonAsync<TokenResponse>();
        rotated!.RefreshToken.ShouldNotBe(tokens.RefreshToken);

        var reuseResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = tokens.RefreshToken });
        reuseResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        var email = UniqueEmail();
        await RegisterAndVerifyAsync(email);
        var tokens = await LoginAsync(email);

        var logoutResponse = await _client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = tokens.RefreshToken });
        logoutResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var refreshResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = tokens.RefreshToken });
        refreshResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PhoneOtp_RequestThenVerify_MarksPhoneVerified()
    {
        var email = UniqueEmail();
        await RegisterAndVerifyAsync(email);
        var tokens = await LoginAsync(email);

        using var otpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/request-phone-otp")
        {
            Content = JsonContent.Create(new { phoneNumber = "+27821234567" }),
        };
        otpRequest.Headers.Authorization = new("Bearer", tokens.AccessToken);
        var otpResponse = await _client.SendAsync(otpRequest);
        otpResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        var sms = factory.SmsSender.SentMessages.Last(m => m.To == "+27821234567");
        var code = System.Text.RegularExpressions.Regex.Match(sms.Message, @"\d{6}").Value;

        using var wrongVerify = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/verify-phone")
        {
            Content = JsonContent.Create(new { code = "000000" }),
        };
        wrongVerify.Headers.Authorization = new("Bearer", tokens.AccessToken);
        (await _client.SendAsync(wrongVerify)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        using var correctVerify = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/verify-phone")
        {
            Content = JsonContent.Create(new { code }),
        };
        correctVerify.Headers.Authorization = new("Bearer", tokens.AccessToken);
        var correctResponse = await _client.SendAsync(correctVerify);
        correctResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/me");
        meRequest.Headers.Authorization = new("Bearer", tokens.AccessToken);
        var profile = await (await _client.SendAsync(meRequest)).Content.ReadFromJsonAsync<ProfileResponse>();
        profile!.PhoneVerified.ShouldBeTrue();
    }

    [Fact]
    public async Task ForgotPassword_UnknownEmail_StillReturnsAccepted()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = UniqueEmail() });

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task ForgotPassword_ThenResetPassword_AllowsLoginWithNewPassword()
    {
        var email = UniqueEmail();
        var (userId, _) = await RegisterAndVerifyAsync(email);

        var forgotResponse = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email });
        forgotResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        var resetEmail = factory.EmailSender.SentEmails.Last(e => e.To == email && e.Subject.Contains("Reset"));
        var token = CapturingEmailSender.ExtractQueryParam(resetEmail.Body, "token");

        var resetResponse = await _client.PostAsJsonAsync("/api/v1/auth/reset-password", new
        {
            userId,
            token,
            newPassword = "BrandNewPassw0rd",
        });
        resetResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Passw0rd123" }))
            .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        (await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "BrandNewPassw0rd" }))
            .StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private sealed record RegisterResponse(Guid UserId);

    private sealed record TokenResponse(string AccessToken, string RefreshToken);

    private sealed record ProfileResponse(Guid UserId, string Email, bool EmailVerified, bool PhoneVerified);
}
