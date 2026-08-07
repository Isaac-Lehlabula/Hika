import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../../core/theme/hika_colors.dart';
import '../../../../core/theme/hika_spacing.dart';
import '../../../../shared/widgets/hika_button.dart';
import '../../../../shared/widgets/hika_card.dart';
import '../../../../shared/widgets/hika_empty_state.dart';
import '../../data/review.dart';
import '../providers/user_reviews_controller.dart';

class UserReviewsScreen extends ConsumerWidget {
  const UserReviewsScreen({required this.userId, required this.displayName, super.key});

  final String userId;
  final String displayName;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final reviewsAsync = ref.watch(userReviewsControllerProvider(userId));

    return Scaffold(
      appBar: AppBar(title: Text('$displayName\'s reviews')),
      body: reviewsAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => Center(
          child: HikaButton(
            label: 'Try again',
            variant: HikaButtonVariant.secondary,
            onPressed: () => ref.read(userReviewsControllerProvider(userId).notifier).refresh(),
          ),
        ),
        data: (reviews) {
          if (reviews.items.isEmpty) {
            return const HikaEmptyState(
              icon: Icons.star_outline_rounded,
              title: 'No reviews yet',
              message: 'Reviews appear here once trips are completed and reviewed.',
            );
          }

          return RefreshIndicator(
            onRefresh: () => ref.read(userReviewsControllerProvider(userId).notifier).refresh(),
            child: ListView.separated(
              padding: const EdgeInsets.all(HikaSpacing.lg),
              itemCount: reviews.items.length + (reviews.hasMore ? 1 : 0),
              separatorBuilder: (_, _) => const SizedBox(height: HikaSpacing.md),
              itemBuilder: (context, index) {
                if (index == reviews.items.length) {
                  return Center(
                    child: HikaButton(
                      label: 'Load more',
                      variant: HikaButtonVariant.secondary,
                      onPressed: () => ref.read(userReviewsControllerProvider(userId).notifier).loadMore(),
                    ),
                  );
                }
                return _ReviewCard(review: reviews.items[index]);
              },
            ),
          );
        },
      ),
    );
  }
}

class _ReviewCard extends StatelessWidget {
  const _ReviewCard({required this.review});

  final Review review;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return HikaCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              CircleAvatar(
                radius: 18,
                backgroundColor: HikaColors.accentLight,
                backgroundImage: review.reviewerPhotoUrl == null ? null : NetworkImage(review.reviewerPhotoUrl!),
                child: review.reviewerPhotoUrl == null
                    ? Text(review.reviewerFirstName.substring(0, 1), style: theme.textTheme.titleSmall)
                    : null,
              ),
              const SizedBox(width: HikaSpacing.sm),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(review.reviewerFirstName, style: theme.textTheme.titleSmall),
                    Text(DateFormat.yMMMd().format(review.createdAtUtc.toLocal()), style: theme.textTheme.bodySmall),
                  ],
                ),
              ),
              Row(
                children: [
                  for (var star = 1; star <= 5; star++)
                    Icon(
                      star <= review.rating ? Icons.star_rounded : Icons.star_outline_rounded,
                      size: 16,
                      color: HikaColors.warning,
                    ),
                ],
              ),
            ],
          ),
          if (review.comment != null) ...[
            const SizedBox(height: HikaSpacing.sm),
            Text(review.comment!, style: theme.textTheme.bodyMedium),
          ],
        ],
      ),
    );
  }
}
