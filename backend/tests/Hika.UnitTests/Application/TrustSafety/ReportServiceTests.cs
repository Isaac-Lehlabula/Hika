using Hika.Application.TrustSafety;
using Hika.Application.TrustSafety.Dtos;
using Hika.Domain.TrustSafety;
using Hika.UnitTests.TestSupport;
using Shouldly;

namespace Hika.UnitTests.Application.TrustSafety;

public class ReportServiceTests
{
    private readonly InMemoryAppDbContext _db = new();
    private readonly ReportService _sut;

    public ReportServiceTests()
    {
        _sut = new ReportService(_db);
    }

    [Fact]
    public async Task FileAsync_AboutAUser_ReturnsOpenReport()
    {
        var report = await _sut.FileAsync(
            Guid.NewGuid(),
            new CreateReportRequest { ReportedUserId = Guid.NewGuid(), Reason = ReportReason.Scam, Description = "Asked for cash upfront" },
            CancellationToken.None);

        report.Status.ShouldBe("Open");
        report.Reason.ShouldBe("Scam");
        report.ReportedUserId.ShouldNotBeNull();
    }

    [Fact]
    public async Task FileAsync_AboutATrip_ReturnsReportWithTripId()
    {
        var tripId = Guid.NewGuid();

        var report = await _sut.FileAsync(
            Guid.NewGuid(),
            new CreateReportRequest { ReportedTripId = tripId, Reason = ReportReason.UnsafeDriving, Description = "Was on the phone" },
            CancellationToken.None);

        report.ReportedTripId.ShouldBe(tripId);
    }
}
