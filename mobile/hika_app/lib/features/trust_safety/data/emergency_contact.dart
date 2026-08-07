/// Mirrors backend Hika.Application.TrustSafety.Dtos.EmergencyContactResponse.
class EmergencyContact {
  const EmergencyContact({required this.id, required this.name, required this.phoneNumber, this.relationship});

  factory EmergencyContact.fromJson(Map<String, dynamic> json) => EmergencyContact(
    id: json['id'] as String,
    name: json['name'] as String,
    phoneNumber: json['phoneNumber'] as String,
    relationship: json['relationship'] as String?,
  );

  final String id;
  final String name;
  final String phoneNumber;
  final String? relationship;
}
