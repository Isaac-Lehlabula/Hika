import 'package:flutter_test/flutter_test.dart';
import 'package:hika_app/features/trust_safety/data/report.dart';

void main() {
  test('Report.fromJson round-trips the backend ReportResponse shape', () {
    final report = Report.fromJson({
      'id': 'r1',
      'reportedUserId': 'u1',
      'reportedTripId': null,
      'reason': 'Harassment',
      'description': 'Was rude to me.',
      'status': 'Open',
      'createdAtUtc': '2026-08-01T10:00:00Z',
    });

    expect(report.id, 'r1');
    expect(report.reportedUserId, 'u1');
    expect(report.reportedTripId, isNull);
    expect(report.reason, 'Harassment');
    expect(report.status, 'Open');
  });

  test('ReportReason wireValue matches backend enum member names', () {
    expect(ReportReason.harassment.wireValue, 'Harassment');
    expect(ReportReason.unsafeDriving.wireValue, 'UnsafeDriving');
    expect(ReportReason.noShow.wireValue, 'NoShow');
    expect(ReportReason.scam.wireValue, 'Scam');
    expect(ReportReason.other.wireValue, 'Other');
  });
}
