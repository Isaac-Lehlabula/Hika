import 'package:flutter_test/flutter_test.dart';
import 'package:hika_app/features/bookings/data/booking.dart';

Map<String, dynamic> _bookingJson({String status = 'Pending', String? cancellationReason}) => {
  'id': 'b1',
  'status': status,
  'trip': {
    'tripId': 't1',
    'departureAtUtc': '2026-12-20T05:00:00Z',
    'originName': 'Johannesburg',
    'destinationName': 'Giyani',
    'driver': {
      'userId': 'u1',
      'firstName': 'Thabo',
      'lastName': 'Mokoena',
      'photoUrl': null,
      'averageRating': null,
      'completedTripCount': 3,
      'isVerifiedDriver': true,
    },
    'vehicle': {
      'id': 'v1',
      'make': 'Toyota',
      'model': 'Quantum',
      'year': 2019,
      'color': 'White',
      'isVerified': true,
      'primaryPhotoUrl': null,
    },
  },
  'boardingStopSequence': 0,
  'boardingStopName': 'Johannesburg',
  'alightingStopSequence': 1,
  'alightingStopName': 'Polokwane',
  'seatsRequested': 2,
  'totalPrice': 600,
  'passenger': {
    'userId': 'p1',
    'firstName': 'Naledi',
    'lastName': 'Dlamini',
    'photoUrl': null,
    'phoneNumber': '+27821234567',
  },
  'requestedAtUtc': '2026-12-01T10:00:00Z',
  'respondedAtUtc': null,
  'cancelledAtUtc': null,
  'cancellationReason': cancellationReason,
};

void main() {
  test('Booking.fromJson round-trips the backend BookingResponse shape', () {
    final booking = Booking.fromJson(_bookingJson());

    expect(booking.status, 'Pending');
    expect(booking.trip.originName, 'Johannesburg');
    expect(booking.trip.driver.fullName, 'Thabo Mokoena');
    expect(booking.trip.vehicle.displayName, '2019 Toyota Quantum');
    expect(booking.boardingStopName, 'Johannesburg');
    expect(booking.alightingStopName, 'Polokwane');
    expect(booking.seatsRequested, 2);
    expect(booking.totalPrice, 600);
    expect(booking.passenger.fullName, 'Naledi Dlamini');
    expect(booking.respondedAtUtc, isNull);
  });

  test('canCancel is true for Pending, AwaitingPayment, and Confirmed, false otherwise', () {
    expect(Booking.fromJson(_bookingJson(status: 'Pending')).canCancel, isTrue);
    expect(Booking.fromJson(_bookingJson(status: 'AwaitingPayment')).canCancel, isTrue);
    expect(Booking.fromJson(_bookingJson(status: 'Confirmed')).canCancel, isTrue);
    expect(Booking.fromJson(_bookingJson(status: 'Declined')).canCancel, isFalse);
    expect(Booking.fromJson(_bookingJson(status: 'Cancelled')).canCancel, isFalse);
    expect(Booking.fromJson(_bookingJson(status: 'Completed')).canCancel, isFalse);
  });

  test('canRespond is true only for Pending', () {
    expect(Booking.fromJson(_bookingJson(status: 'Pending')).canRespond, isTrue);
    expect(Booking.fromJson(_bookingJson(status: 'Confirmed')).canRespond, isFalse);
  });

  test('cancellationReason parses when present', () {
    final booking = Booking.fromJson(_bookingJson(status: 'Cancelled', cancellationReason: 'Change of plans'));

    expect(booking.cancellationReason, 'Change of plans');
  });
}
