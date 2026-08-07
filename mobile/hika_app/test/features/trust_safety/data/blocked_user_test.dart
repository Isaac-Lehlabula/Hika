import 'package:flutter_test/flutter_test.dart';
import 'package:hika_app/features/trust_safety/data/blocked_user.dart';

void main() {
  test('BlockedUser.fromJson round-trips the backend BlockedUserResponse shape', () {
    final user = BlockedUser.fromJson({
      'userId': 'u1',
      'firstName': 'Thabo',
      'lastName': 'Nkosi',
      'photoUrl': null,
      'blockedAtUtc': '2026-08-01T10:00:00Z',
    });

    expect(user.userId, 'u1');
    expect(user.firstName, 'Thabo');
    expect(user.lastName, 'Nkosi');
    expect(user.photoUrl, isNull);
  });

  test('fullName combines first and last name', () {
    final user = BlockedUser.fromJson({
      'userId': 'u1',
      'firstName': 'Thabo',
      'lastName': 'Nkosi',
      'photoUrl': null,
      'blockedAtUtc': '2026-08-01T10:00:00Z',
    });

    expect(user.fullName, 'Thabo Nkosi');
  });
}
