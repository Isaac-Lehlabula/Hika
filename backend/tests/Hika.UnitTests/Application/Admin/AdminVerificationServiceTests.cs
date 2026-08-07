using Hika.Application.Admin;
using Hika.Domain.Drivers;
using Hika.Domain.TrustSafety;
using Hika.UnitTests.TestSupport;
using Shouldly;

namespace Hika.UnitTests.Application.Admin;

public class AdminVerificationServiceTests
{
    private readonly InMemoryAppDbContext _db = new();
    private readonly AdminVerificationService _sut;

    public AdminVerificationServiceTests()
    {
        _sut = new AdminVerificationService(_db, new AuditLogger(_db));
    }

    private DriverProfile SeedDriver()
    {
        var driverProfile = DriverProfile.Create(Guid.NewGuid(), "1234567890123", DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2)));
        _db.DriverProfiles.Add(driverProfile);
        _db.SaveChangesAsync(CancellationToken.None).GetAwaiter().GetResult();
        return driverProfile;
    }

    [Fact]
    public async Task GetQueueAsync_DefaultsToPending()
    {
        var driver = SeedDriver();
        var pending = Verification.CreateAndSubmit(VerificationSubjectType.User, driver.Id, VerificationType.DriverLicense, "https://example.com/doc.jpg");
        _db.Verifications.Add(pending);
        await _db.SaveChangesAsync(CancellationToken.None);

        var result = await _sut.GetQueueAsync(null, page: 1, pageSize: 20, CancellationToken.None);

        result.Items.ShouldHaveSingleItem();
        result.Items[0].Status.ShouldBe("Pending");
    }

    [Fact]
    public async Task ApproveAsync_DriverLicenseVerification_MarksDriverProfileVerified()
    {
        var driver = SeedDriver();
        var verification = Verification.CreateAndSubmit(VerificationSubjectType.User, driver.Id, VerificationType.DriverLicense, "https://example.com/doc.jpg");
        _db.Verifications.Add(verification);
        await _db.SaveChangesAsync(CancellationToken.None);

        var result = await _sut.ApproveAsync(Guid.NewGuid(), verification.Id, CancellationToken.None);

        result.Status.ShouldBe("Verified");
        driver.IsVerifiedDriver.ShouldBeTrue();
    }

    [Fact]
    public async Task RejectAsync_DriverLicenseVerification_KeepsDriverProfileUnverifiedAndRecordsReason()
    {
        var driver = SeedDriver();
        var verification = Verification.CreateAndSubmit(VerificationSubjectType.User, driver.Id, VerificationType.DriverLicense, "https://example.com/doc.jpg");
        _db.Verifications.Add(verification);
        await _db.SaveChangesAsync(CancellationToken.None);

        var result = await _sut.RejectAsync(Guid.NewGuid(), verification.Id, "Document is illegible", CancellationToken.None);

        result.Status.ShouldBe("Rejected");
        result.RejectionReason.ShouldBe("Document is illegible");
        driver.IsVerifiedDriver.ShouldBeFalse();
    }
}
