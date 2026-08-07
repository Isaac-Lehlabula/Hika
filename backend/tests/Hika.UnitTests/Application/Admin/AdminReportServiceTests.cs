using Hika.Application.Admin;
using Hika.Domain.TrustSafety;
using Hika.Domain.Users;
using Hika.UnitTests.TestSupport;
using Shouldly;

namespace Hika.UnitTests.Application.Admin;

public class AdminReportServiceTests
{
    private readonly InMemoryAppDbContext _db = new();
    private readonly AdminReportService _sut;

    public AdminReportServiceTests()
    {
        _sut = new AdminReportService(_db, new AuditLogger(_db));
    }

    private Report SeedReport()
    {
        var reporter = UserProfile.Create(Guid.NewGuid(), "Thabo", "Nkosi");
        var reported = UserProfile.Create(Guid.NewGuid(), "Sipho", "Dlamini");
        _db.UserProfiles.AddRange(reporter, reported);
        var report = Report.File(reporter.Id, reported.Id, null, ReportReason.Harassment, "Made me uncomfortable");
        _db.Reports.Add(report);
        _db.SaveChangesAsync(CancellationToken.None).GetAwaiter().GetResult();
        return report;
    }

    [Fact]
    public async Task GetReportsAsync_DefaultsToAllStatuses_ResolvesReporterAndReportedNames()
    {
        SeedReport();

        var result = await _sut.GetReportsAsync(null, page: 1, pageSize: 20, CancellationToken.None);

        result.Items.ShouldHaveSingleItem();
        result.Items[0].ReporterName.ShouldBe("Thabo Nkosi");
        result.Items[0].ReportedUserName.ShouldBe("Sipho Dlamini");
    }

    [Fact]
    public async Task ResolveAsync_SetsStatusToResolved()
    {
        var report = SeedReport();

        var result = await _sut.ResolveAsync(Guid.NewGuid(), report.Id, CancellationToken.None);

        result.Status.ShouldBe("Resolved");
    }

    [Fact]
    public async Task DismissAsync_SetsStatusToDismissed()
    {
        var report = SeedReport();

        var result = await _sut.DismissAsync(Guid.NewGuid(), report.Id, CancellationToken.None);

        result.Status.ShouldBe("Dismissed");
    }
}
