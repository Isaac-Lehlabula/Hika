import 'package:flutter_test/flutter_test.dart';
import 'package:hika_app/features/trust_safety/data/emergency_contact.dart';

void main() {
  test('EmergencyContact.fromJson round-trips the backend EmergencyContactResponse shape', () {
    final contact = EmergencyContact.fromJson({
      'id': 'c1',
      'name': 'Naledi Dlamini',
      'phoneNumber': '+27821234567',
      'relationship': 'Spouse',
    });

    expect(contact.id, 'c1');
    expect(contact.name, 'Naledi Dlamini');
    expect(contact.phoneNumber, '+27821234567');
    expect(contact.relationship, 'Spouse');
  });

  test('EmergencyContact.fromJson allows a null relationship', () {
    final contact = EmergencyContact.fromJson({
      'id': 'c1',
      'name': 'Naledi Dlamini',
      'phoneNumber': '+27821234567',
      'relationship': null,
    });

    expect(contact.relationship, isNull);
  });
}
