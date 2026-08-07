/// Mirrors backend Hika.Domain.TrustSafety.ReportReason — serialized by member name.
enum ReportReason {
  harassment('Harassment', 'Harassment'),
  unsafeDriving('UnsafeDriving', 'Unsafe driving'),
  noShow('NoShow', 'No-show'),
  scam('Scam', 'Scam or fraud'),
  other('Other', 'Something else');

  const ReportReason(this.wireValue, this.displayName);

  final String wireValue;
  final String displayName;
}

/// Mirrors backend Hika.Application.TrustSafety.Dtos.ReportResponse.
class Report {
  const Report({
    required this.id,
    this.reportedUserId,
    this.reportedTripId,
    required this.reason,
    required this.description,
    required this.status,
    required this.createdAtUtc,
  });

  factory Report.fromJson(Map<String, dynamic> json) => Report(
    id: json['id'] as String,
    reportedUserId: json['reportedUserId'] as String?,
    reportedTripId: json['reportedTripId'] as String?,
    reason: json['reason'] as String,
    description: json['description'] as String,
    status: json['status'] as String,
    createdAtUtc: DateTime.parse(json['createdAtUtc'] as String),
  );

  final String id;
  final String? reportedUserId;
  final String? reportedTripId;
  final String reason;
  final String description;
  final String status;
  final DateTime createdAtUtc;
}
