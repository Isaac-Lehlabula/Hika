import 'package:flutter_test/flutter_test.dart';
import 'package:hika_app/features/ride_requests/data/ride_request.dart';

Map<String, dynamic> _json({
  String status = 'Open',
  bool isExpired = false,
  double? proposedPricePerSeat,
  String? claimedBookingId,
}) => {
  'id': 'r1',
  'originRawText': 'Johannesburg',
  'destinationRawText': 'Giyani',
  'travelDate': '2026-12-20',
  'seatsNeeded': 2,
  'proposedPricePerSeat': proposedPricePerSeat,
  'status': status,
  'isExpired': isExpired,
  'claimedBookingId': claimedBookingId,
  'createdAtUtc': '2026-12-01T10:00:00Z',
};

void main() {
  test('RideRequest.fromJson round-trips the backend RideRequestResponse shape', () {
    final request = RideRequest.fromJson(_json(proposedPricePerSeat: 250));

    expect(request.originRawText, 'Johannesburg');
    expect(request.destinationRawText, 'Giyani');
    expect(request.travelDate, DateTime(2026, 12, 20));
    expect(request.seatsNeeded, 2);
    expect(request.proposedPricePerSeat, 250);
    expect(request.status, 'Open');
    expect(request.isExpired, isFalse);
  });

  test('RideRequest.fromJson handles a null proposedPricePerSeat and claimedBookingId', () {
    final request = RideRequest.fromJson(_json());

    expect(request.proposedPricePerSeat, isNull);
    expect(request.claimedBookingId, isNull);
  });

  test('label combines origin and destination', () {
    final request = RideRequest.fromJson(_json());

    expect(request.label, 'Johannesburg → Giyani');
  });

  test('isOpen is true only when status is Open and not expired', () {
    expect(RideRequest.fromJson(_json()).isOpen, isTrue);
    expect(RideRequest.fromJson(_json(isExpired: true)).isOpen, isFalse);
    expect(RideRequest.fromJson(_json(status: 'Claimed')).isOpen, isFalse);
    expect(RideRequest.fromJson(_json(status: 'Cancelled')).isOpen, isFalse);
  });
}
