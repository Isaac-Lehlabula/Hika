import '../../../core/networking/api_client.dart';
import 'review.dart';

/// Mirrors backend/src/Hika.Api/Controllers/ReviewsController.cs 1:1.
class ReviewsApi {
  ReviewsApi(this._client);

  final ApiClient _client;

  Future<Review> submitReview({required String bookingId, required int rating, String? comment}) async {
    final body = await _client.post(
      '/api/v1/reviews/bookings/$bookingId',
      data: {'rating': rating, 'comment': comment},
    );
    return Review.fromJson(body!);
  }

  Future<PagedReviews> getReviewsForUser(String userId, {int page = 1, int pageSize = 20}) async {
    final body = await _client.get('/api/v1/reviews/users/$userId', query: {'page': page, 'pageSize': pageSize});
    final items = (body!['items'] as List<dynamic>).map((r) => Review.fromJson(r as Map<String, dynamic>)).toList();
    return PagedReviews(items: items, page: body['page'] as int, pageSize: body['pageSize'] as int, totalCount: body['totalCount'] as int);
  }
}
