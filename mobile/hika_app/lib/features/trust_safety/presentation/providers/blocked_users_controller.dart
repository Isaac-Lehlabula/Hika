import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/providers.dart';
import '../../data/blocked_user.dart';

class BlockedUsersController extends AsyncNotifier<List<BlockedUser>> {
  @override
  Future<List<BlockedUser>> build() => ref.read(trustSafetyApiProvider).getMyBlocks();

  Future<void> refresh() async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() => ref.read(trustSafetyApiProvider).getMyBlocks());
  }

  Future<void> block(String userId) async {
    await ref.read(trustSafetyApiProvider).blockUser(userId);
    await refresh();
  }

  Future<void> unblock(String userId) async {
    await ref.read(trustSafetyApiProvider).unblockUser(userId);
    await refresh();
  }
}

final blockedUsersControllerProvider = AsyncNotifierProvider<BlockedUsersController, List<BlockedUser>>(
  BlockedUsersController.new,
);
