using Hika.Application.Common.Exceptions;
using Hika.Application.Common.Storage;
using Hika.Application.Drivers;
using Hika.Application.Drivers.Dtos;
using Hika.Domain.Drivers;
using Hika.UnitTests.TestSupport;
using NSubstitute;
using Shouldly;

namespace Hika.UnitTests.Application.Drivers;

public class DriverProfileServiceTests
{
    private readonly InMemoryAppDbContext _db = new();
    private readonly IFileStorage _fileStorage = Substitute.For<IFileStorage>();
    private readonly DriverProfileService _sut;

    public DriverProfileServiceTests()
    {
        _sut = new DriverProfileService(_db, _fileStorage);
        _fileStorage.SaveAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("https://example.com/verification-documents/license.jpg");
    }

    private static CreateOrUpdateDriverProfileRequest ValidRequest() => new()
    {
        LicenseNumber = "1234567890123",
        LicenseExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2)),
    };

    [Fact]
    public async Task CreateOrUpdateAsync_NoExistingProfile_CreatesOne()
    {
        var userId = Guid.NewGuid();

        var response = await _sut.CreateOrUpdateAsync(userId, ValidRequest(), CancellationToken.None);

        response.UserId.ShouldBe(userId);
        response.IsVerifiedDriver.ShouldBeFalse();
        response.VerificationStatus.ShouldBe("NotSubmitted");
        _db.DriverProfiles.Count().ShouldBe(1);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_ExistingProfile_UpdatesLicenseInPlace()
    {
        var userId = Guid.NewGuid();
        await _sut.CreateOrUpdateAsync(userId, ValidRequest(), CancellationToken.None);

        var updated = ValidRequest() with { LicenseNumber = "NEW-LICENSE-1" };
        await _sut.CreateOrUpdateAsync(userId, updated, CancellationToken.None);

        _db.DriverProfiles.Count().ShouldBe(1);
        _db.DriverProfiles.Single().LicenseNumber.ShouldBe("NEW-LICENSE-1");
    }

    [Fact]
    public async Task GetOwnAsync_NoProfile_ThrowsNotFound()
    {
        await Should.ThrowAsync<NotFoundException>(() => _sut.GetOwnAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task SubmitLicenseVerificationAsync_NoProfile_ThrowsNotFound()
    {
        using var stream = new MemoryStream();

        await Should.ThrowAsync<NotFoundException>(
            () => _sut.SubmitLicenseVerificationAsync(Guid.NewGuid(), stream, "license.jpg", "image/jpeg", CancellationToken.None));
    }

    [Fact]
    public async Task SubmitLicenseVerificationAsync_CreatesPendingVerification_AndKeepsDriverUnverified()
    {
        var userId = Guid.NewGuid();
        await _sut.CreateOrUpdateAsync(userId, ValidRequest(), CancellationToken.None);
        using var stream = new MemoryStream();

        await _sut.SubmitLicenseVerificationAsync(userId, stream, "license.jpg", "image/jpeg", CancellationToken.None);

        var response = await _sut.GetOwnAsync(userId, CancellationToken.None);
        response.VerificationStatus.ShouldBe("Pending");
        response.IsVerifiedDriver.ShouldBeFalse();
    }

    [Fact]
    public async Task SubmitLicenseVerificationAsync_WhenPreviouslyVerified_ResetsToUnverified()
    {
        var userId = Guid.NewGuid();
        await _sut.CreateOrUpdateAsync(userId, ValidRequest(), CancellationToken.None);
        var profile = _db.DriverProfiles.Single(p => p.Id == userId);
        profile.MarkVerified();
        await _db.SaveChangesAsync(CancellationToken.None);

        using var stream = new MemoryStream();
        await _sut.SubmitLicenseVerificationAsync(userId, stream, "license.jpg", "image/jpeg", CancellationToken.None);

        _db.DriverProfiles.Single(p => p.Id == userId).IsVerifiedDriver.ShouldBeFalse();
    }
}
