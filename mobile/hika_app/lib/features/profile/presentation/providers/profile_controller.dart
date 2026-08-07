import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/providers.dart';
import '../../data/user_profile.dart';

/// Loads and holds the signed-in user's own profile. AsyncValue gives
/// screens loading/error/data states for free instead of hand-rolled flags.
class ProfileController extends AsyncNotifier<UserProfile> {
  @override
  Future<UserProfile> build() => ref.read(profileApiProvider).getOwnProfile();

  Future<void> refresh() async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() => ref.read(profileApiProvider).getOwnProfile());
  }

  Future<void> updateProfile({required String firstName, required String lastName}) async {
    final updated = await ref.read(profileApiProvider).updateProfile(firstName: firstName, lastName: lastName);
    state = AsyncData(updated);
  }
}

final profileControllerProvider = AsyncNotifierProvider<ProfileController, UserProfile>(ProfileController.new);
