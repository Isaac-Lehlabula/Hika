import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/providers.dart';
import '../../data/notification.dart';

class NotificationsController extends AsyncNotifier<PagedNotifications> {
  @override
  Future<PagedNotifications> build() => ref.read(notificationsApiProvider).getMyNotifications();

  Future<void> refresh() async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() => ref.read(notificationsApiProvider).getMyNotifications());
  }

  Future<void> loadMore() async {
    final current = state.value;
    if (current == null || !current.hasMore) {
      return;
    }

    final next = await ref.read(notificationsApiProvider).getMyNotifications(page: current.page + 1, pageSize: current.pageSize);
    state = AsyncData(
      PagedNotifications(items: [...current.items, ...next.items], page: next.page, pageSize: next.pageSize, totalCount: next.totalCount),
    );
  }

  /// Marks read on the server and updates the loaded list in place — avoids a full
  /// refetch just to flip one item's status.
  Future<void> markRead(String notificationId) async {
    await ref.read(notificationsApiProvider).markRead(notificationId);

    final current = state.value;
    if (current == null) {
      return;
    }

    final updatedItems = [
      for (final item in current.items)
        if (item.id == notificationId)
          AppNotification(
            id: item.id,
            type: item.type,
            message: item.message,
            relatedEntityId: item.relatedEntityId,
            status: 'Read',
            createdAtUtc: item.createdAtUtc,
            readAtUtc: DateTime.now(),
          )
        else
          item,
    ];
    state = AsyncData(
      PagedNotifications(items: updatedItems, page: current.page, pageSize: current.pageSize, totalCount: current.totalCount),
    );
  }
}

final notificationsControllerProvider = AsyncNotifierProvider<NotificationsController, PagedNotifications>(
  NotificationsController.new,
);
