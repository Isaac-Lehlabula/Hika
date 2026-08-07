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

    [Fact]
    public void Resolve_OpenReport_SetsStatusToResolved()
    {
        var report = Report.File(Guid.NewGuid(), Guid.NewGuid(), null, ReportReason.Scam, "Asked for cash upfront");

        report.Resolve();

        report.Status.ShouldBe(ReportStatus.Resolved);
    }

    [Fact]
    public void Dismiss_OpenReport_SetsStatusToDismissed()
    {
        var report = Report.File(Guid.NewGuid(), Guid.NewGuid(), null, ReportReason.Other, "Minor complaint");

        report.Dismiss();

        report.Status.ShouldBe(ReportStatus.Dismissed);
    }

    [Fact]
    public void MarkUnderReview_OpenReport_SetsStatusToUnderReview()
    {
        var report = Report.File(Guid.NewGuid(), Guid.NewGuid(), null, ReportReason.NoShow, "Never arrived");

        report.MarkUnderReview();

        report.Status.ShouldBe(ReportStatus.UnderReview);
    }

    [Fact]
    public void Resolve_AlreadyResolvedReport_Throws()
    {
        var report = Report.File(Guid.NewGuid(), Guid.NewGuid(), null, ReportReason.Scam, "Asked for cash upfront");
        report.Resolve();

        Should.Throw<InvalidOperationException>(() => report.Resolve());
    }

    [Fact]
    public void Dismiss_AlreadyResolvedReport_Throws()
    {
        var report = Report.File(Guid.NewGuid(), Guid.NewGuid(), null, ReportReason.Scam, "Asked for cash upfront");
        report.Resolve();

        Should.Throw<InvalidOperationException>(() => report.Dismiss());
    }
}
