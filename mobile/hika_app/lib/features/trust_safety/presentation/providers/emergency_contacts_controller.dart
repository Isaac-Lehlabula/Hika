import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/providers.dart';
import '../../data/emergency_contact.dart';

class EmergencyContactsController extends AsyncNotifier<List<EmergencyContact>> {
  @override
  Future<List<EmergencyContact>> build() => ref.read(trustSafetyApiProvider).getMyEmergencyContacts();

  Future<void> refresh() async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() => ref.read(trustSafetyApiProvider).getMyEmergencyContacts());
  }

  Future<void> create({required String name, required String phoneNumber, String? relationship}) async {
    await ref.read(trustSafetyApiProvider).createEmergencyContact(name: name, phoneNumber: phoneNumber, relationship: relationship);
    await refresh();
  }

  Future<void> updateContact(String contactId, {required String name, required String phoneNumber, String? relationship}) async {
    await ref
        .read(trustSafetyApiProvider)
        .updateEmergencyContact(contactId, name: name, phoneNumber: phoneNumber, relationship: relationship);
    await refresh();
  }

  Future<void> delete(String contactId) async {
    await ref.read(trustSafetyApiProvider).deleteEmergencyContact(contactId);
    await refresh();
  }
}

final emergencyContactsControllerProvider = AsyncNotifierProvider<EmergencyContactsController, List<EmergencyContact>>(
  EmergencyContactsController.new,
);
