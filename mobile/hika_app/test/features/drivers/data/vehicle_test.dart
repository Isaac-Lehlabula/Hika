import 'package:flutter_test/flutter_test.dart';
import 'package:hika_app/features/drivers/data/vehicle.dart';

Vehicle _vehicleWithPhotos(List<VehiclePhoto> photos) => Vehicle(
  id: 'v1',
  make: 'Toyota',
  model: 'Corolla',
  year: 2020,
  color: 'White',
  registrationNumber: 'CA123456',
  seatCapacity: 4,
  isVerified: false,
  photos: photos,
);

void main() {
  group('Vehicle.primaryPhoto', () {
    test('returns null when there are no photos', () {
      expect(_vehicleWithPhotos(const []).primaryPhoto, isNull);
    });

    test('returns the photo marked primary', () {
      final vehicle = _vehicleWithPhotos(const [
        VehiclePhoto(id: 'p1', url: 'https://example.com/a.jpg', isPrimary: false),
        VehiclePhoto(id: 'p2', url: 'https://example.com/b.jpg', isPrimary: true),
      ]);

      expect(vehicle.primaryPhoto?.id, 'p2');
    });

    test('falls back to the first photo when none are marked primary', () {
      final vehicle = _vehicleWithPhotos(const [
        VehiclePhoto(id: 'p1', url: 'https://example.com/a.jpg', isPrimary: false),
        VehiclePhoto(id: 'p2', url: 'https://example.com/b.jpg', isPrimary: false),
      ]);

      expect(vehicle.primaryPhoto?.id, 'p1');
    });
  });

  test('displayName combines year, make, and model', () {
    expect(_vehicleWithPhotos(const []).displayName, '2020 Toyota Corolla');
  });

  test('Vehicle.fromJson round-trips the backend VehicleResponse shape', () {
    final vehicle = Vehicle.fromJson({
      'id': 'v1',
      'make': 'Toyota',
      'model': 'Corolla',
      'year': 2020,
      'color': 'White',
      'registrationNumber': 'CA123456',
      'seatCapacity': 4,
      'isVerified': true,
      'photos': [
        {'id': 'p1', 'url': 'https://example.com/a.jpg', 'isPrimary': true},
      ],
    });

    expect(vehicle.isVerified, isTrue);
    expect(vehicle.photos, hasLength(1));
    expect(vehicle.primaryPhoto?.url, 'https://example.com/a.jpg');
  });
}
