/// Mirrors backend Hika.Application.TrustSafety.Dtos.BlockedUserResponse.
class BlockedUser {
  const BlockedUser({
    required this.userId,
    required this.firstName,
    required this.lastName,
    this.photoUrl,
    required this.blockedAtUtc,
  });

  factory BlockedUser.fromJson(Map<String, dynamic> json) => BlockedUser(
    userId: json['userId'] as String,
    firstName: json['firstName'] as String,
    lastName: json['lastName'] as String,
    photoUrl: json['photoUrl'] as String?,
    blockedAtUtc: DateTime.parse(json['blockedAtUtc'] as String),
  );

  final String userId;
  final String firstName;
  final String lastName;
  final String? photoUrl;
  final DateTime blockedAtUtc;

  String get fullName => '$firstName $lastName';
}
