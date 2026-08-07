class VehiclePhoto {
  const VehiclePhoto({required this.id, required this.url, required this.isPrimary});

  factory VehiclePhoto.fromJson(Map<String, dynamic> json) =>
      VehiclePhoto(id: json['id'] as String, url: json['url'] as String, isPrimary: json['isPrimary'] as bool);

  final String id;
  final String url;
  final bool isPrimary;
}

class Vehicle {
  const Vehicle({
    required this.id,
    required this.make,
    required this.model,
    required this.year,
    required this.color,
    required this.registrationNumber,
    required this.seatCapacity,
    required this.isVerified,
    required this.photos,
  });

  factory Vehicle.fromJson(Map<String, dynamic> json) => Vehicle(
    id: json['id'] as String,
    make: json['make'] as String,
    model: json['model'] as String,
    year: json['year'] as int,
    color: json['color'] as String,
    registrationNumber: json['registrationNumber'] as String,
    seatCapacity: json['seatCapacity'] as int,
    isVerified: json['isVerified'] as bool,
    photos: (json['photos'] as List<dynamic>? ?? [])
        .map((p) => VehiclePhoto.fromJson(p as Map<String, dynamic>))
        .toList(),
  );

  final String id;
  final String make;
  final String model;
  final int year;
  final String color;
  final String registrationNumber;
  final int seatCapacity;
  final bool isVerified;
  final List<VehiclePhoto> photos;

  String get displayName => '$year $make $model';

  VehiclePhoto? get primaryPhoto {
    for (final photo in photos) {
      if (photo.isPrimary) {
        return photo;
      }
    }
    return photos.isEmpty ? null : photos.first;
  }
}
