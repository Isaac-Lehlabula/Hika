import 'package:flutter_test/flutter_test.dart';
import 'package:hika_app/features/reviews/data/review.dart';

void main() {
  test('Review.fromJson round-trips the backend ReviewResponse shape', () {
    final review = Review.fromJson({
      'id': 'r1',
      'bookingId': 'b1',
      'reviewerUserId': 'u1',
      'reviewerFirstName': 'Naledi',
      'reviewerPhotoUrl': null,
      'revieweeUserId': 'u2',
      'direction': 'PassengerToDriver',
      'rating': 5,
      'comment': 'Great driver!',
      'createdAtUtc': '2026-12-01T10:00:00Z',
    });

    expect(review.reviewerFirstName, 'Naledi');
    expect(review.direction, 'PassengerToDriver');
    expect(review.rating, 5);
    expect(review.comment, 'Great driver!');
  });

  test('PagedReviews.hasMore is true when more pages remain', () {
    const reviews = PagedReviews(items: [], page: 1, pageSize: 20, totalCount: 45);

    expect(reviews.hasMore, isTrue);
  });

  test('PagedReviews.hasMore is false on the last page', () {
    const reviews = PagedReviews(items: [], page: 3, pageSize: 20, totalCount: 45);

    expect(reviews.hasMore, isFalse);
  });
}
