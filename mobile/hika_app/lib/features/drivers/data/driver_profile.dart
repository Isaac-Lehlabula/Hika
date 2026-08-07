class DriverProfile {
  const DriverProfile({
    required this.userId,
    required this.licenseNumber,
    required this.licenseExpiryDate,
    required this.isVerifiedDriver,
    required this.verificationStatus,
  });

  factory DriverProfile.fromJson(Map<String, dynamic> json) => DriverProfile(
    userId: json['userId'] as String,
    licenseNumber: json['licenseNumber'] as String,
    licenseExpiryDate: DateTime.parse(json['licenseExpiryDate'] as String),
    isVerifiedDriver: json['isVerifiedDriver'] as bool,
    verificationStatus: json['verificationStatus'] as String,
  );

  final String userId;
  final String licenseNumber;
  final DateTime licenseExpiryDate;
  final bool isVerifiedDriver;
  final String verificationStatus;
}
