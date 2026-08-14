import '../../trips/data/trip.dart';

/// Mirrors backend Hika.Application.Bookings.Dtos.BookingTripSummary.
class BookingTripSummary {
  const BookingTripSummary({
    required this.tripId,
    required this.departureAtUtc,
    required this.originName,
    required this.destinationName,
    required this.driver,
    required this.vehicle,
  });

  factory BookingTripSummary.fromJson(Map<String, dynamic> json) => BookingTripSummary(
    tripId: json['tripId'] as String,
    departureAtUtc: DateTime.parse(json['departureAtUtc'] as String),
    originName: json['originName'] as String,
    destinationName: json['destinationName'] as String,
    driver: TripDriverSummary.fromJson(json['driver'] as Map<String, dynamic>),
    vehicle: TripVehicleSummary.fromJson(json['vehicle'] as Map<String, dynamic>),
  );

  final String tripId;
  final DateTime departureAtUtc;
  final String originName;
  final String destinationName;
  final TripDriverSummary driver;
  final TripVehicleSummary vehicle;
}

class BookingPassengerSummary {
  const BookingPassengerSummary({
    required this.userId,
    required this.firstName,
    required this.lastName,
    this.photoUrl,
    this.phoneNumber,
  });

  factory BookingPassengerSummary.fromJson(Map<String, dynamic> json) => BookingPassengerSummary(
    userId: json['userId'] as String,
    firstName: json['firstName'] as String,
    lastName: json['lastName'] as String,
    photoUrl: json['photoUrl'] as String?,
    phoneNumber: json['phoneNumber'] as String?,
  );

  final String userId;
  final String firstName;
  final String lastName;
  final String? photoUrl;
  final String? phoneNumber;

  String get fullName => '$firstName $lastName';
}

/// Mirrors backend Hika.Application.Bookings.Dtos.BookingResponse.
class Booking {
  const Booking({
    required this.id,
    required this.status,
    required this.trip,
    required this.boardingStopSequence,
    required this.boardingStopName,
    required this.alightingStopSequence,
    required this.alightingStopName,
    required this.seatsRequested,
    required this.totalPrice,
    required this.passenger,
    required this.requestedAtUtc,
    this.respondedAtUtc,
    this.cancelledAtUtc,
    this.cancellationReason,
  });

  factory Booking.fromJson(Map<String, dynamic> json) => Booking(
    id: json['id'] as String,
    status: json['status'] as String,
    trip: BookingTripSummary.fromJson(json['trip'] as Map<String, dynamic>),
    boardingStopSequence: json['boardingStopSequence'] as int,
    boardingStopName: json['boardingStopName'] as String,
    alightingStopSequence: json['alightingStopSequence'] as int,
    alightingStopName: json['alightingStopName'] as String,
    seatsRequested: json['seatsRequested'] as int,
    totalPrice: (json['totalPrice'] as num).toDouble(),
    passenger: BookingPassengerSummary.fromJson(json['passenger'] as Map<String, dynamic>),
    requestedAtUtc: DateTime.parse(json['requestedAtUtc'] as String),
    respondedAtUtc: json['respondedAtUtc'] == null ? null : DateTime.parse(json['respondedAtUtc'] as String),
    cancelledAtUtc: json['cancelledAtUtc'] == null ? null : DateTime.parse(json['cancelledAtUtc'] as String),
    cancellationReason: json['cancellationReason'] as String?,
  );

  final String id;
  final String status;
  final BookingTripSummary trip;
  final int boardingStopSequence;
  final String boardingStopName;
  final int alightingStopSequence;
  final String alightingStopName;
  final int seatsRequested;
  final double totalPrice;
  final BookingPassengerSummary passenger;
  final DateTime requestedAtUtc;
  final DateTime? respondedAtUtc;
  final DateTime? cancelledAtUtc;
  final String? cancellationReason;

  bool get canCancel => status == 'Pending' || status == 'AwaitingPayment' || status == 'Confirmed';

  bool get canRespond => status == 'Pending';
}
