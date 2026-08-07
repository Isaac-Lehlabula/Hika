using Hika.Application.Common.Exceptions;
using Hika.Application.Common.Options;
using Hika.Application.Common.Security;
using Hika.Application.Users;
using Hika.Application.Users.Dtos;
using Hika.Application.Users.Ports;
using Hika.Domain.Users;
using Hika.UnitTests.TestSupport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Hika.UnitTests.Application.Users;

public class AuthServiceTests
{
    private readonly InMemoryAppDbContext _db = new();
    private readonly IUserAccountService _userAccounts = Substitute.For<IUserAccountService>();
    private readonly IJwtTokenGenerator _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly ISmsSender _smsSender = Substitute.For<ISmsSender>();

    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        var links = Options.Create(new NotificationLinksOptions { BaseUrl = "http://localhost:3000" });
        _sut = new AuthService(
            _db, _userAccounts, _jwtTokenGenerator, _emailSender, _smsSender, links,
            Substitute.For<ILogger<AuthService>>());

        _jwtTokenGenerator.GenerateAccessToken(Arg.Any<Guid>(), Arg.Any<string>())
            .Returns(new GeneratedAccessToken("fake-jwt", DateTimeOffset.UtcNow.AddMinutes(15)));
    }

    private static RegisterRequest ValidRegisterRequest() => new()
    {
        Email = "thabo@example.com",
        Password = "Passw0rd123",
        FirstName = "Thabo",
        LastName = "Nkosi",
    };

    [Fact]
    public async Task RegisterAsync_NewEmail_CreatesProfileAndSendsVerificationEmail()
    {
        var userId = Guid.NewGuid();
        _userAccounts.FindByEmailAsync("thabo@example.com", Arg.Any<CancellationToken>())
            .Returns((UserAccountSummary?)null, new UserAccountSummary(userId, "thabo@example.com", false));
        _userAccounts.CreateAccountAsync("thabo@example.com", "Passw0rd123", Arg.Any<CancellationToken>())
            .Returns(AccountOperationResult.Success());

        var result = await _sut.RegisterAsync(ValidRegisterRequest(), CancellationToken.None);

        result.ShouldBe(userId);
        (await _db.UserProfiles.FindAsync(userId))!.FirstName.ShouldBe("Thabo");
        await _emailSender.Received(1).SendAsync(
            "thabo@example.com", Arg.Is<string>(s => s != null && s.Contains("Verify")), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_ExistingEmail_ThrowsConflict()
    {
        _userAccounts.FindByEmailAsync("thabo@example.com", Arg.Any<CancellationToken>())
            .Returns(new UserAccountSummary(Guid.NewGuid(), "thabo@example.com", true));

        await Should.ThrowAsync<ConflictException>(() => _sut.RegisterAsync(ValidRegisterRequest(), CancellationToken.None));

        await _userAccounts.DidNotReceive().CreateAccountAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_IdentityRejectsPassword_ThrowsAppValidationWithErrors()
    {
        _userAccounts.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((UserAccountSummary?)null);
        _userAccounts.CreateAccountAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AccountOperationResult.Failure(["Passwords must have at least one non alphanumeric character."]));

        var ex = await Should.ThrowAsync<AppValidationException>(
            () => _sut.RegisterAsync(ValidRegisterRequest(), CancellationToken.None));

        ex.Errors.ShouldContainKey("password");
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ThrowsUnauthorized()
    {
        _userAccounts.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((UserAccountSummary?)null);

        await Should.ThrowAsync<UnauthorizedException>(
            () => _sut.LoginAsync(new LoginRequest { Email = "nobody@example.com", Password = "x" }, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task LoginAsync_LockedOut_ThrowsUnauthorizedWithLockoutMessage()
    {
        var account = new UserAccountSummary(Guid.NewGuid(), "thabo@example.com", true);
        _userAccounts.FindByEmailAsync("thabo@example.com", Arg.Any<CancellationToken>()).Returns(account);
        _userAccounts.CheckPasswordAsync(account.UserId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CredentialCheckResult.LockedOut);

        var ex = await Should.ThrowAsync<UnauthorizedException>(() => _sut.LoginAsync(
            new LoginRequest { Email = "thabo@example.com", Password = "wrong" }, null, null, CancellationToken.None));

        ex.Message.ShouldContain("locked");
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokensAndPersistsRefreshToken()
    {
        var account = new UserAccountSummary(Guid.NewGuid(), "thabo@example.com", true);
        _userAccounts.FindByEmailAsync("thabo@example.com", Arg.Any<CancellationToken>()).Returns(account);
        _userAccounts.CheckPasswordAsync(account.UserId, "Passw0rd123", Arg.Any<CancellationToken>())
            .Returns(CredentialCheckResult.Success);

        var response = await _sut.LoginAsync(
            new LoginRequest { Email = "thabo@example.com", Password = "Passw0rd123" }, "196.1.2.3", "TestAgent/1.0", CancellationToken.None);

        response.AccessToken.ShouldBe("fake-jwt");
        response.RefreshToken.ShouldNotBeNullOrWhiteSpace();

        var stored = _db.RefreshTokens.Single();
        stored.UserId.ShouldBe(account.UserId);
        stored.TokenHash.ShouldBe(TokenHasher.Hash(response.RefreshToken));
        stored.CreatedByIp.ShouldBe("196.1.2.3");
    }

    [Fact]
    public async Task RefreshAsync_UnknownToken_ThrowsUnauthorized()
    {
        await Should.ThrowAsync<UnauthorizedException>(
            () => _sut.RefreshAsync("does-not-exist", null, null, CancellationToken.None));
    }

    [Fact]
    public async Task RefreshAsync_ExpiredToken_ThrowsUnauthorized()
    {
        var userId = Guid.NewGuid();
        var expired = new RefreshToken(userId, TokenHasher.Hash("expired-token"), DateTimeOffset.UtcNow.AddDays(-1), null, null);
        _db.RefreshTokens.Add(expired);
        await _db.SaveChangesAsync(CancellationToken.None);

        await Should.ThrowAsync<UnauthorizedException>(
            () => _sut.RefreshAsync("expired-token", null, null, CancellationToken.None));
    }

    [Fact]
    public async Task RefreshAsync_ValidToken_RotatesAndReturnsNewTokens()
    {
        var account = new UserAccountSummary(Guid.NewGuid(), "thabo@example.com", true);
        _userAccounts.FindByIdAsync(account.UserId, Arg.Any<CancellationToken>()).Returns(account);

        var original = new RefreshToken(account.UserId, TokenHasher.Hash("original-token"), DateTimeOffset.UtcNow.AddDays(30), null, null);
        _db.RefreshTokens.Add(original);
        await _db.SaveChangesAsync(CancellationToken.None);

        var response = await _sut.RefreshAsync("original-token", null, null, CancellationToken.None);

        response.RefreshToken.ShouldNotBe("original-token");

        var reloadedOriginal = _db.RefreshTokens.Single(t => t.Id == original.Id);
        reloadedOriginal.RevokedAtUtc.ShouldNotBeNull();
        reloadedOriginal.ReplacedByTokenHash.ShouldBe(TokenHasher.Hash(response.RefreshToken));
    }

    [Fact]
    public async Task RefreshAsync_ReusedRevokedToken_RevokesAllSessionsAndThrows()
    {
        var userId = Guid.NewGuid();
        var revoked = new RefreshToken(userId, TokenHasher.Hash("stolen-token"), DateTimeOffset.UtcNow.AddDays(30), null, null);
        revoked.Revoke("some-other-hash");
        var stillActive = new RefreshToken(userId, TokenHasher.Hash("other-active-session"), DateTimeOffset.UtcNow.AddDays(30), null, null);
        _db.RefreshTokens.AddRange(revoked, stillActive);
        await _db.SaveChangesAsync(CancellationToken.None);

        await Should.ThrowAsync<UnauthorizedException>(
            () => _sut.RefreshAsync("stolen-token", null, null, CancellationToken.None));

        _db.RefreshTokens.Single(t => t.Id == stillActive.Id).RevokedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task VerifyEmailAsync_InvalidToken_ThrowsAppValidation()
    {
        var request = new VerifyEmailRequest { UserId = Guid.NewGuid(), Token = "bogus" };

        await Should.ThrowAsync<AppValidationException>(() => _sut.VerifyEmailAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task VerifyEmailAsync_ValidToken_ConfirmsEmailAndMarksProfile()
    {
        var userId = Guid.NewGuid();
        var profile = UserProfile.Create(userId, "Thabo", "Nkosi");
        _db.UserProfiles.Add(profile);
        _db.EmailVerificationTokens.Add(new EmailVerificationToken(userId, TokenHasher.Hash("good-token"), DateTimeOffset.UtcNow.AddHours(1)));
        await _db.SaveChangesAsync(CancellationToken.None);

        await _sut.VerifyEmailAsync(new VerifyEmailRequest { UserId = userId, Token = "good-token" }, CancellationToken.None);

        await _userAccounts.Received(1).MarkEmailConfirmedAsync(userId, Arg.Any<CancellationToken>());
        _db.UserProfiles.Single(p => p.Id == userId).EmailVerifiedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task ForgotPasswordAsync_UnknownEmail_DoesNotSendEmail()
    {
        _userAccounts.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((UserAccountSummary?)null);

        await _sut.ForgotPasswordAsync("nobody@example.com", CancellationToken.None);

        await _emailSender.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VerifyPhoneAsync_WrongCode_RecordsAttemptAndThrows()
    {
        var userId = Guid.NewGuid();
        _db.PhoneVerificationCodes.Add(new PhoneVerificationCode(
            userId, "+27821234567", TokenHasher.Hash("123456"), DateTimeOffset.UtcNow.AddMinutes(10)));
        await _db.SaveChangesAsync(CancellationToken.None);

        await Should.ThrowAsync<AppValidationException>(() => _sut.VerifyPhoneAsync(userId, "000000", CancellationToken.None));

        _db.PhoneVerificationCodes.Single(c => c.UserId == userId).AttemptCount.ShouldBe(1);
    }

    [Fact]
    public async Task VerifyPhoneAsync_CorrectCode_MarksProfilePhoneVerified()
    {
        var userId = Guid.NewGuid();
        var profile = UserProfile.Create(userId, "Thabo", "Nkosi");
        profile.SetPhoneNumber("+27821234567");
        _db.UserProfiles.Add(profile);
        _db.PhoneVerificationCodes.Add(new PhoneVerificationCode(
            userId, "+27821234567", TokenHasher.Hash("123456"), DateTimeOffset.UtcNow.AddMinutes(10)));
        await _db.SaveChangesAsync(CancellationToken.None);

        await _sut.VerifyPhoneAsync(userId, "123456", CancellationToken.None);

        _db.UserProfiles.Single(p => p.Id == userId).PhoneVerifiedAtUtc.ShouldNotBeNull();
    }
}
