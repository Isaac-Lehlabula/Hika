class UserProfile {
  const UserProfile({
    required this.userId,
    required this.email,
    required this.emailVerified,
    required this.firstName,
    required this.lastName,
    required this.phoneVerified,
    required this.memberSinceUtc,
    required this.completedTripCount,
    this.photoUrl,
    this.phoneNumber,
    this.averageRating,
  });

  factory UserProfile.fromJson(Map<String, dynamic> json) => UserProfile(
    userId: json['userId'] as String,
    email: json['email'] as String,
    emailVerified: json['emailVerified'] as bool,
    firstName: json['firstName'] as String,
    lastName: json['lastName'] as String,
    photoUrl: json['photoUrl'] as String?,
    phoneNumber: json['phoneNumber'] as String?,
    phoneVerified: json['phoneVerified'] as bool,
    memberSinceUtc: DateTime.parse(json['memberSinceUtc'] as String),
    averageRating: (json['averageRating'] as num?)?.toDouble(),
    completedTripCount: json['completedTripCount'] as int,
  );

  final String userId;
  final String email;
  final bool emailVerified;
  final String firstName;
  final String lastName;
  final String? photoUrl;
  final String? phoneNumber;
  final bool phoneVerified;
  final DateTime memberSinceUtc;
  final double? averageRating;
  final int completedTripCount;

  String get fullName => '$firstName $lastName';

  String get initials {
    final f = firstName.isNotEmpty ? firstName[0] : '';
    final l = lastName.isNotEmpty ? lastName[0] : '';
    return '$f$l'.toUpperCase();
  }
}
