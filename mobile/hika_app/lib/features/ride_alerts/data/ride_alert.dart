/// Mirrors backend Hika.Application.RideAlerts.Dtos.RideAlertResponse.
class RideAlert {
  const RideAlert({
    required this.id,
    required this.originRawText,
    required this.destinationRawText,
    this.travelDate,
    required this.status,
    required this.createdAtUtc,
  });

  factory RideAlert.fromJson(Map<String, dynamic> json) => RideAlert(
    id: json['id'] as String,
    originRawText: json['originRawText'] as String,
    destinationRawText: json['destinationRawText'] as String,
    travelDate: json['travelDate'] == null ? null : DateTime.parse(json['travelDate'] as String),
    status: json['status'] as String,
    createdAtUtc: DateTime.parse(json['createdAtUtc'] as String),
  );

  final String id;
  final String originRawText;
  final String destinationRawText;
  final DateTime? travelDate;
  final String status;
  final DateTime createdAtUtc;

  String get label => '$originRawText → $destinationRawText';
}
