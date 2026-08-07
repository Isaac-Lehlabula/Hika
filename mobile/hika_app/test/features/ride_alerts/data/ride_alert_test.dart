import 'package:flutter_test/flutter_test.dart';
import 'package:hika_app/features/ride_alerts/data/ride_alert.dart';

void main() {
  test('RideAlert.fromJson round-trips the backend RideAlertResponse shape', () {
    final alert = RideAlert.fromJson({
      'id': 'a1',
      'originRawText': 'Johannesburg',
      'destinationRawText': 'Giyani',
      'travelDate': '2026-12-20',
      'status': 'Active',
      'createdAtUtc': '2026-12-01T10:00:00Z',
    });

    expect(alert.originRawText, 'Johannesburg');
    expect(alert.destinationRawText, 'Giyani');
    expect(alert.travelDate, DateTime(2026, 12, 20));
    expect(alert.status, 'Active');
  });

  test('RideAlert.fromJson parses a null travelDate as "any date"', () {
    final alert = RideAlert.fromJson({
      'id': 'a1',
      'originRawText': 'Johannesburg',
      'destinationRawText': 'Giyani',
      'travelDate': null,
      'status': 'Active',
      'createdAtUtc': '2026-12-01T10:00:00Z',
    });

    expect(alert.travelDate, isNull);
  });

  test('label combines origin and destination', () {
    final alert = RideAlert.fromJson({
      'id': 'a1',
      'originRawText': 'Johannesburg',
      'destinationRawText': 'Giyani',
      'travelDate': null,
      'status': 'Active',
      'createdAtUtc': '2026-12-01T10:00:00Z',
    });

    expect(alert.label, 'Johannesburg → Giyani');
  });
}
