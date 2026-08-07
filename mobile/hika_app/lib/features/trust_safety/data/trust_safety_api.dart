import '../../../core/networking/api_client.dart';
import 'blocked_user.dart';
import 'emergency_contact.dart';
import 'report.dart';

/// Mirrors backend/src/Hika.Api/Controllers/TrustSafetyController.cs and the
/// emergency-contacts actions on UsersController.cs.
class TrustSafetyApi {
  TrustSafetyApi(this._client);

  final ApiClient _client;

  Future<Report> fileReport({String? reportedUserId, String? reportedTripId, required ReportReason reason, required String description}) async {
    final body = await _client.post(
      '/api/v1/trust-safety/reports',
      data: {
        'reportedUserId': reportedUserId,
        'reportedTripId': reportedTripId,
        'reason': reason.wireValue,
        'description': description,
      },
    );
    return Report.fromJson(body!);
  }

  Future<List<BlockedUser>> getMyBlocks() async {
    final list = await _client.getList('/api/v1/trust-safety/blocks');
    return list.map((b) => BlockedUser.fromJson(b as Map<String, dynamic>)).toList();
  }

  Future<void> blockUser(String userId) async {
    await _client.post('/api/v1/trust-safety/blocks/$userId');
  }

  Future<void> unblockUser(String userId) => _client.delete('/api/v1/trust-safety/blocks/$userId');

  Future<List<EmergencyContact>> getMyEmergencyContacts() async {
    final list = await _client.getList('/api/v1/users/me/emergency-contacts');
    return list.map((c) => EmergencyContact.fromJson(c as Map<String, dynamic>)).toList();
  }

  Future<EmergencyContact> createEmergencyContact({required String name, required String phoneNumber, String? relationship}) async {
    final body = await _client.post(
      '/api/v1/users/me/emergency-contacts',
      data: {'name': name, 'phoneNumber': phoneNumber, 'relationship': relationship},
    );
    return EmergencyContact.fromJson(body!);
  }

  Future<EmergencyContact> updateEmergencyContact(
    String contactId, {
    required String name,
    required String phoneNumber,
    String? relationship,
  }) async {
    final body = await _client.put(
      '/api/v1/users/me/emergency-contacts/$contactId',
      data: {'name': name, 'phoneNumber': phoneNumber, 'relationship': relationship},
    );
    return EmergencyContact.fromJson(body!);
  }

  Future<void> deleteEmergencyContact(String contactId) => _client.delete('/api/v1/users/me/emergency-contacts/$contactId');
}
