/// Mirrors backend Hika.Application.Payments.Dtos.PaymentResponse.
class Payment {
  const Payment({
    required this.id,
    required this.bookingId,
    required this.amount,
    required this.platformFee,
    required this.driverPayoutAmount,
    required this.provider,
    this.providerReference,
    required this.status,
    required this.createdAtUtc,
  });

  factory Payment.fromJson(Map<String, dynamic> json) => Payment(
    id: json['id'] as String,
    bookingId: json['bookingId'] as String,
    amount: (json['amount'] as num).toDouble(),
    platformFee: (json['platformFee'] as num).toDouble(),
    driverPayoutAmount: (json['driverPayoutAmount'] as num).toDouble(),
    provider: json['provider'] as String,
    providerReference: json['providerReference'] as String?,
    status: json['status'] as String,
    createdAtUtc: DateTime.parse(json['createdAtUtc'] as String),
  );

  final String id;
  final String bookingId;
  final double amount;
  final double platformFee;
  final double driverPayoutAmount;
  final String provider;
  final String? providerReference;
  final String status;
  final DateTime createdAtUtc;
}
