import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../../core/theme/hika_colors.dart';
import '../../../../core/theme/hika_spacing.dart';
import '../../../../shared/widgets/hika_button.dart';
import '../../../../shared/widgets/hika_card.dart';
import '../../../../shared/widgets/hika_empty_state.dart';
import '../../../profile/presentation/providers/profile_controller.dart';
import '../../data/notification.dart';
import '../notification_routing.dart';
import '../providers/notifications_controller.dart';

/// The Inbox tab — every Notification row for the signed-in user, in-app being the only
/// channel actually delivered today (see docs/roadmap.md's Phase 9 note on Push).
class InboxScreen extends ConsumerWidget {
  const InboxScreen({super.key});

  void _open(BuildContext context, WidgetRef ref, AppNotification notification) {
    if (notification.isUnread) {
      ref.read(notificationsControllerProvider.notifier).markRead(notification.id);
    }

    final route = notificationRoute(
      type: notification.type,
      relatedEntityId: notification.relatedEntityId,
      currentUserId: ref.read(profileControllerProvider).value?.userId,
    );
    if (route != null) {
      context.push(route.path, extra: route.extra);
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final notificationsAsync = ref.watch(notificationsControllerProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Inbox'),
        actions: [
          IconButton(
            icon: const Icon(Icons.campaign_outlined),
            tooltip: 'Ride requests',
            onPressed: () => context.push('/ride-requests'),
          ),
          IconButton(
            icon: const Icon(Icons.notifications_active_outlined),
            tooltip: 'My ride alerts',
            onPressed: () => context.push('/ride-alerts'),
          ),
        ],
      ),
      body: notificationsAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => Center(
          child: HikaButton(
            label: 'Try again',
            variant: HikaButtonVariant.secondary,
            onPressed: () => ref.read(notificationsControllerProvider.notifier).refresh(),
          ),
        ),
        data: (notifications) {
          if (notifications.items.isEmpty) {
            return HikaEmptyState(
              icon: Icons.mail_outline_rounded,
              title: 'Nothing yet',
              message: 'Booking updates, payment confirmations, and reviews will show up here.',
              action: HikaButton(
                label: 'Set a ride alert',
                variant: HikaButtonVariant.secondary,
                icon: Icons.notifications_active_outlined,
                onPressed: () => context.push('/ride-alerts'),
              ),
            );
          }

          return RefreshIndicator(
            onRefresh: () => ref.read(notificationsControllerProvider.notifier).refresh(),
            child: ListView.separated(
              padding: const EdgeInsets.all(HikaSpacing.lg),
              itemCount: notifications.items.length + (notifications.hasMore ? 1 : 0),
              separatorBuilder: (_, _) => const SizedBox(height: HikaSpacing.sm),
              itemBuilder: (context, index) {
                if (index == notifications.items.length) {
                  return Center(
                    child: HikaButton(
                      label: 'Load more',
                      variant: HikaButtonVariant.secondary,
                      onPressed: () => ref.read(notificationsControllerProvider.notifier).loadMore(),
                    ),
                  );
                }
                final notification = notifications.items[index];
                return _NotificationCard(notification: notification, onTap: () => _open(context, ref, notification));
              },
            ),
          );
        },
      ),
    );
  }
}

class _NotificationCard extends StatelessWidget {
  const _NotificationCard({required this.notification, required this.onTap});

  final AppNotification notification;
  final VoidCallback onTap;

  (IconData, Color) _iconFor(String type) => switch (type) {
    'BookingRequested' => (Icons.event_seat_outlined, HikaColors.accent),
    'BookingAccepted' => (Icons.check_circle_outline, HikaColors.success),
    'BookingDeclined' => (Icons.cancel_outlined, HikaColors.danger),
    'PaymentSucceeded' => (Icons.payments_outlined, HikaColors.accent),
    'NewReview' => (Icons.star_outline_rounded, HikaColors.warning),
    'RideAlertMatched' => (Icons.notifications_active_outlined, HikaColors.primary),
    _ => (Icons.notifications_none_rounded, HikaColors.accent),
  };

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final (icon, color) = _iconFor(notification.type);

    return HikaCard(
      onTap: onTap,
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            padding: const EdgeInsets.all(HikaSpacing.sm),
            decoration: BoxDecoration(color: color.withValues(alpha: 0.12), shape: BoxShape.circle),
            child: Icon(icon, size: 20, color: color),
          ),
          const SizedBox(width: HikaSpacing.md),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  notification.message,
                  style: theme.textTheme.bodyMedium?.copyWith(
                    fontWeight: notification.isUnread ? FontWeight.w600 : FontWeight.normal,
                  ),
                ),
                const SizedBox(height: HikaSpacing.xxs),
                Text(DateFormat('EEE d MMM, HH:mm').format(notification.createdAtUtc.toLocal()), style: theme.textTheme.bodySmall),
              ],
            ),
          ),
          if (notification.isUnread)
            Container(
              width: 8,
              height: 8,
              margin: const EdgeInsets.only(top: HikaSpacing.xxs),
              decoration: const BoxDecoration(color: HikaColors.primary, shape: BoxShape.circle),
            ),
        ],
      ),
    );
  }
}
