/// Mirrors backend Hika.Application.RideRequests.Dtos.RideRequestResponse.
class RideRequest {
  const RideRequest({
    required this.id,
    required this.originRawText,
    required this.destinationRawText,
    required this.travelDate,
    required this.seatsNeeded,
    this.proposedPricePerSeat,
    required this.status,
    required this.isExpired,
    this.claimedBookingId,
    required this.createdAtUtc,
  });

  factory RideRequest.fromJson(Map<String, dynamic> json) => RideRequest(
    id: json['id'] as String,
    originRawText: json['originRawText'] as String,
    destinationRawText: json['destinationRawText'] as String,
    travelDate: DateTime.parse(json['travelDate'] as String),
    seatsNeeded: json['seatsNeeded'] as int,
    proposedPricePerSeat: (json['proposedPricePerSeat'] as num?)?.toDouble(),
    status: json['status'] as String,
    isExpired: json['isExpired'] as bool,
    claimedBookingId: json['claimedBookingId'] as String?,
    createdAtUtc: DateTime.parse(json['createdAtUtc'] as String),
  );

  final String id;
  final String originRawText;
  final String destinationRawText;
  final DateTime travelDate;
  final int seatsNeeded;
  final double? proposedPricePerSeat;
  final String status;
  final bool isExpired;
  final String? claimedBookingId;
  final DateTime createdAtUtc;

  String get label => '$originRawText → $destinationRawText';

  bool get isOpen => status == 'Open' && !isExpired;
}
