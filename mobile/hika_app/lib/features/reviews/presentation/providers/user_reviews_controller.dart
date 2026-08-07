import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/providers.dart';
import '../../data/review.dart';

/// Riverpod 3.x family notifiers receive their argument via the constructor.
class UserReviewsController extends AsyncNotifier<PagedReviews> {
  UserReviewsController(this.userId);

  final String userId;

  @override
  Future<PagedReviews> build() => ref.read(reviewsApiProvider).getReviewsForUser(userId);

  Future<void> refresh() async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() => ref.read(reviewsApiProvider).getReviewsForUser(userId));
  }

  Future<void> loadMore() async {
    final current = state.value;
    if (current == null || !current.hasMore) {
      return;
    }

    final next = await ref.read(reviewsApiProvider).getReviewsForUser(userId, page: current.page + 1, pageSize: current.pageSize);
    state = AsyncData(
      PagedReviews(
        items: [...current.items, ...next.items],
        page: next.page,
        pageSize: next.pageSize,
        totalCount: next.totalCount,
      ),
    );
  }
}

final userReviewsControllerProvider = AsyncNotifierProvider.family<UserReviewsController, PagedReviews, String>(
  UserReviewsController.new,
);
