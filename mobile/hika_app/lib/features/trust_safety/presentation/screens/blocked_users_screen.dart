import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/theme/hika_colors.dart';
import '../../../../core/theme/hika_spacing.dart';
import '../../../../shared/widgets/hika_button.dart';
import '../../../../shared/widgets/hika_card.dart';
import '../../../../shared/widgets/hika_empty_state.dart';
import '../../data/blocked_user.dart';
import '../providers/blocked_users_controller.dart';

class BlockedUsersScreen extends ConsumerWidget {
  const BlockedUsersScreen({super.key});

  Future<void> _unblock(BuildContext context, WidgetRef ref, BlockedUser user) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Unblock this user?'),
        content: Text('${user.fullName} will be able to see your trips and book with you again.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Keep blocked')),
          TextButton(onPressed: () => Navigator.pop(context, true), child: const Text('Unblock')),
        ],
      ),
    );
    if (confirmed == true) {
      await ref.read(blockedUsersControllerProvider.notifier).unblock(user.userId);
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final blocksAsync = ref.watch(blockedUsersControllerProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Blocked users')),
      body: blocksAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => Center(
          child: HikaButton(
            label: 'Try again',
            variant: HikaButtonVariant.secondary,
            onPressed: () => ref.read(blockedUsersControllerProvider.notifier).refresh(),
          ),
        ),
        data: (blocks) {
          if (blocks.isEmpty) {
            return const HikaEmptyState(
              icon: Icons.block_outlined,
              title: 'No blocked users',
              message: 'Users you block won\'t be able to book your trips, and you won\'t be able to book theirs.',
            );
          }

          return RefreshIndicator(
            onRefresh: () => ref.read(blockedUsersControllerProvider.notifier).refresh(),
            child: ListView.separated(
              padding: const EdgeInsets.all(HikaSpacing.lg),
              itemCount: blocks.length,
              separatorBuilder: (_, _) => const SizedBox(height: HikaSpacing.md),
              itemBuilder: (context, index) {
                final user = blocks[index];
                return HikaCard(
                  child: Row(
                    children: [
                      CircleAvatar(
                        radius: 20,
                        backgroundColor: HikaColors.accentLight,
                        backgroundImage: user.photoUrl == null ? null : NetworkImage(user.photoUrl!),
                        child: user.photoUrl == null ? Text(user.firstName.substring(0, 1)) : null,
                      ),
                      const SizedBox(width: HikaSpacing.md),
                      Expanded(child: Text(user.fullName, style: Theme.of(context).textTheme.titleMedium)),
                      HikaButton(
                        label: 'Unblock',
                        variant: HikaButtonVariant.secondary,
                        onPressed: () => _unblock(context, ref, user),
                      ),
                    ],
                  ),
                );
              },
            ),
          );
        },
      ),
    );
  }
}
