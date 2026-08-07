import 'province.dart';

class TripStopInput {
  const TripStopInput({required this.rawName, required this.province});

  final String rawName;
  final Province province;
}

class TripStop {
  const TripStop({required this.sequence, this.locationId, required this.name, required this.province});

  factory TripStop.fromJson(Map<String, dynamic> json) => TripStop(
    sequence: json['sequence'] as int,
    locationId: json['locationId'] as String?,
    name: json['name'] as String,
    province: json['province'] as String,
  );

  final int sequence;
  final String? locationId;
  final String name;

  /// Raw wire value (e.g. "KwaZuluNatal") — see [Province.fromWireValue] for the display name.
  final String province;
}

class TripSegment {
  const TripSegment({required this.fromSequence, required this.toSequence, required this.seatsAvailable});

  factory TripSegment.fromJson(Map<String, dynamic> json) => TripSegment(
    fromSequence: json['fromSequence'] as int,
    toSequence: json['toSequence'] as int,
    seatsAvailable: json['seatsAvailable'] as int,
  );

  final int fromSequence;
  final int toSequence;
  final int seatsAvailable;
}

class TripDriverSummary {
  const TripDriverSummary({
    required this.userId,
    required this.firstName,
    required this.lastName,
    this.photoUrl,
    this.averageRating,
    required this.completedTripCount,
    required this.isVerifiedDriver,
  });

  factory TripDriverSummary.fromJson(Map<String, dynamic> json) => TripDriverSummary(
    userId: json['userId'] as String,
    firstName: json['firstName'] as String,
    lastName: json['lastName'] as String,
    photoUrl: json['photoUrl'] as String?,
    averageRating: (json['averageRating'] as num?)?.toDouble(),
    completedTripCount: json['completedTripCount'] as int,
    isVerifiedDriver: json['isVerifiedDriver'] as bool,
  );

  final String userId;
  final String firstName;
  final String lastName;
  final String? photoUrl;
  final double? averageRating;
  final int completedTripCount;
  final bool isVerifiedDriver;

  String get fullName => '$firstName $lastName';
}

class TripVehicleSummary {
  const TripVehicleSummary({
    required this.id,
    required this.make,
    required this.model,
    required this.year,
    required this.color,
    required this.isVerified,
    this.primaryPhotoUrl,
  });

  factory TripVehicleSummary.fromJson(Map<String, dynamic> json) => TripVehicleSummary(
    id: json['id'] as String,
    make: json['make'] as String,
    model: json['model'] as String,
    year: json['year'] as int,
    color: json['color'] as String,
    isVerified: json['isVerified'] as bool,
    primaryPhotoUrl: json['primaryPhotoUrl'] as String?,
  );

  final String id;
  final String make;
  final String model;
  final int year;
  final String color;
  final bool isVerified;
  final String? primaryPhotoUrl;

  String get displayName => '$year $make $model';
}

/// Full trip detail — see backend Hika.Application.Trips.Dtos.TripResponse.
class Trip {
  const Trip({
    required this.id,
    required this.status,
    required this.departureAtUtc,
    required this.totalSeatsOffered,
    required this.pricePerSeat,
    this.luggageAllowance,
    this.notes,
    required this.driver,
    required this.vehicle,
    required this.stops,
    required this.segments,
  });

  factory Trip.fromJson(Map<String, dynamic> json) => Trip(
    id: json['id'] as String,
    status: json['status'] as String,
    departureAtUtc: DateTime.parse(json['departureAtUtc'] as String),
    totalSeatsOffered: json['totalSeatsOffered'] as int,
    pricePerSeat: (json['pricePerSeat'] as num).toDouble(),
    luggageAllowance: json['luggageAllowance'] as String?,
    notes: json['notes'] as String?,
    driver: TripDriverSummary.fromJson(json['driver'] as Map<String, dynamic>),
    vehicle: TripVehicleSummary.fromJson(json['vehicle'] as Map<String, dynamic>),
    stops: (json['stops'] as List<dynamic>).map((s) => TripStop.fromJson(s as Map<String, dynamic>)).toList()
      ..sort((a, b) => a.sequence.compareTo(b.sequence)),
    segments: (json['segments'] as List<dynamic>)
        .map((s) => TripSegment.fromJson(s as Map<String, dynamic>))
        .toList(),
  );

  final String id;
  final String status;
  final DateTime departureAtUtc;
  final int totalSeatsOffered;
  final double pricePerSeat;
  final String? luggageAllowance;
  final String? notes;
  final TripDriverSummary driver;
  final TripVehicleSummary vehicle;
  final List<TripStop> stops;
  final List<TripSegment> segments;

  TripStop get origin => stops.first;

  TripStop get destination => stops.last;

  int get minSeatsAvailable =>
      segments.isEmpty ? 0 : segments.map((s) => s.seatsAvailable).reduce((a, b) => a < b ? a : b);
}

/// Lighter-weight shape for list endpoints — see backend TripSummaryResponse.
class TripSummary {
  const TripSummary({
    required this.id,
    required this.status,
    required this.departureAtUtc,
    required this.originName,
    required this.destinationName,
    required this.totalSeatsOffered,
    required this.minSeatsAvailable,
    required this.pricePerSeat,
    required this.driver,
  });

  factory TripSummary.fromJson(Map<String, dynamic> json) => TripSummary(
    id: json['id'] as String,
    status: json['status'] as String,
    departureAtUtc: DateTime.parse(json['departureAtUtc'] as String),
    originName: json['originName'] as String,
    destinationName: json['destinationName'] as String,
    totalSeatsOffered: json['totalSeatsOffered'] as int,
    minSeatsAvailable: json['minSeatsAvailable'] as int,
    pricePerSeat: (json['pricePerSeat'] as num).toDouble(),
    driver: TripDriverSummary.fromJson(json['driver'] as Map<String, dynamic>),
  );

  final String id;
  final String status;
  final DateTime departureAtUtc;
  final String originName;
  final String destinationName;
  final int totalSeatsOffered;
  final int minSeatsAvailable;
  final double pricePerSeat;
  final TripDriverSummary driver;
}
