import 'package:flutter_test/flutter_test.dart';
import 'package:hika_app/features/search/data/search_models.dart';

void main() {
  test('SearchTripResult.fromJson round-trips the backend SearchTripResponse shape', () {
    final result = SearchTripResult.fromJson({
      'id': 't1',
      'departureAtUtc': '2026-12-20T05:00:00Z',
      'boardingStopName': 'Midrand',
      'boardingProvince': 'Gauteng',
      'alightingStopName': 'Polokwane',
      'alightingProvince': 'Limpopo',
      'totalSeatsOffered': 4,
      'seatsAvailable': 2,
      'pricePerSeat': 300,
      'driver': {
        'userId': 'u1',
        'firstName': 'Thabo',
        'lastName': 'Mokoena',
        'photoUrl': null,
        'averageRating': 4.5,
        'completedTripCount': 12,
        'isVerifiedDriver': true,
      },
    });

    expect(result.boardingStopName, 'Midrand');
    expect(result.alightingStopName, 'Polokwane');
    expect(result.seatsAvailable, 2);
    expect(result.driver.fullName, 'Thabo Mokoena');
    expect(result.driver.isVerifiedDriver, isTrue);
  });

  test('TripSearchSort wire values match backend enum member names exactly', () {
    expect(TripSearchSort.departureTime.wireValue, 'DepartureTime');
    expect(TripSearchSort.price.wireValue, 'Price');
    expect(TripSearchSort.driverRating.wireValue, 'DriverRating');
    expect(TripSearchSort.seatsAvailable.wireValue, 'SeatsAvailable');
  });

  test('SearchTripsQuery.copyWith only overrides sort/verifiedOnly, keeping the rest', () {
    const query = SearchTripsQuery(from: 'Johannesburg', to: 'Giyani', passengers: 2);

    final updated = query.copyWith(sort: TripSearchSort.price, verifiedOnly: true);

    expect(updated.from, 'Johannesburg');
    expect(updated.to, 'Giyani');
    expect(updated.passengers, 2);
    expect(updated.sort, TripSearchSort.price);
    expect(updated.verifiedOnly, isTrue);
  });

  test('LocationSuggestion.fromJson parses correctly', () {
    final suggestion = LocationSuggestion.fromJson({
      'id': 'l1',
      'name': 'Johannesburg',
      'province': 'Gauteng',
      'type': 'City',
    });

    expect(suggestion.name, 'Johannesburg');
    expect(suggestion.type, 'City');
  });

  test('PopularRoute.label combines origin and destination', () {
    const route = PopularRoute(originName: 'Johannesburg', destinationName: 'Giyani', tripCount: 5);

    expect(route.label, 'Johannesburg → Giyani');
  });
}
