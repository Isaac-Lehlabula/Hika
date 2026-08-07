import 'package:flutter_test/flutter_test.dart';
import 'package:hika_app/features/trips/data/trip.dart';

Map<String, dynamic> _driverJson() => {
  'userId': 'u1',
  'firstName': 'Thabo',
  'lastName': 'Mokoena',
  'photoUrl': null,
  'averageRating': null,
  'completedTripCount': 5,
  'isVerifiedDriver': true,
};

Map<String, dynamic> _vehicleJson() => {
  'id': 'v1',
  'make': 'Toyota',
  'model': 'Corolla',
  'year': 2020,
  'color': 'White',
  'isVerified': true,
  'primaryPhotoUrl': null,
};

void main() {
  test('Trip.fromJson round-trips the backend TripResponse shape and sorts stops by sequence', () {
    final trip = Trip.fromJson({
      'id': 't1',
      'status': 'Scheduled',
      'departureAtUtc': '2026-12-20T05:00:00Z',
      'totalSeatsOffered': 4,
      'pricePerSeat': 300,
      'luggageAllowance': 'One bag per passenger',
      'notes': null,
      'driver': _driverJson(),
      'vehicle': _vehicleJson(),
      'stops': [
        {'sequence': 2, 'locationId': null, 'name': 'Giyani', 'province': 'Limpopo'},
        {'sequence': 0, 'locationId': null, 'name': 'Johannesburg', 'province': 'Gauteng'},
        {'sequence': 1, 'locationId': null, 'name': 'Polokwane', 'province': 'Limpopo'},
      ],
      'segments': [
        {'fromSequence': 0, 'toSequence': 1, 'seatsAvailable': 4},
        {'fromSequence': 1, 'toSequence': 2, 'seatsAvailable': 3},
      ],
    });

    expect(trip.stops.map((s) => s.name), ['Johannesburg', 'Polokwane', 'Giyani']);
    expect(trip.origin.name, 'Johannesburg');
    expect(trip.destination.name, 'Giyani');
    expect(trip.minSeatsAvailable, 3);
    expect(trip.driver.fullName, 'Thabo Mokoena');
    expect(trip.vehicle.displayName, '2020 Toyota Corolla');
  });

  test('Trip.minSeatsAvailable is 0 when there are no segments', () {
    final trip = Trip.fromJson({
      'id': 't1',
      'status': 'Scheduled',
      'departureAtUtc': '2026-12-20T05:00:00Z',
      'totalSeatsOffered': 4,
      'pricePerSeat': 300,
      'luggageAllowance': null,
      'notes': null,
      'driver': _driverJson(),
      'vehicle': _vehicleJson(),
      'stops': [
        {'sequence': 0, 'locationId': null, 'name': 'Johannesburg', 'province': 'Gauteng'},
        {'sequence': 1, 'locationId': null, 'name': 'Polokwane', 'province': 'Limpopo'},
      ],
      'segments': <Map<String, dynamic>>[],
    });

    expect(trip.minSeatsAvailable, 0);
  });

  test('TripSummary.fromJson round-trips the backend TripSummaryResponse shape', () {
    final summary = TripSummary.fromJson({
      'id': 't1',
      'status': 'Scheduled',
      'departureAtUtc': '2026-12-20T05:00:00Z',
      'originName': 'Johannesburg',
      'destinationName': 'Giyani',
      'totalSeatsOffered': 4,
      'minSeatsAvailable': 3,
      'pricePerSeat': 300,
      'driver': _driverJson(),
    });

    expect(summary.originName, 'Johannesburg');
    expect(summary.minSeatsAvailable, 3);
    expect(summary.driver.isVerifiedDriver, isTrue);
  });
}
