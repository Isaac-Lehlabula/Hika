using Hika.Domain.TrustSafety;
using Shouldly;

namespace Hika.UnitTests.Domain.TrustSafety;

public class ReportTests
{
    [Fact]
    public void File_CreatesOpenReport()
    {
        var report = Report.File(Guid.NewGuid(), Guid.NewGuid(), null, ReportReason.Harassment, "Made me uncomfortable");

        report.Status.ShouldBe(ReportStatus.Open);
        report.Reason.ShouldBe(ReportReason.Harassment);
        report.Description.ShouldBe("Made me uncomfortable");
    }

    [Fact]
    public void File_AboutATrip_SetsReportedTripId()
    {
        var tripId = Guid.NewGuid();

        var report = Report.File(Guid.NewGuid(), null, tripId, ReportReason.UnsafeDriving, "Was speeding");

        report.ReportedTripId.ShouldBe(tripId);
        report.ReportedUserId.ShouldBeNull();
    }
}
