/// Mirrors backend Hika.Application.Reviews.Dtos.ReviewResponse.
class Review {
  const Review({
    required this.id,
    required this.bookingId,
    required this.reviewerUserId,
    required this.reviewerFirstName,
    this.reviewerPhotoUrl,
    required this.revieweeUserId,
    required this.direction,
    required this.rating,
    this.comment,
    required this.createdAtUtc,
  });

  factory Review.fromJson(Map<String, dynamic> json) => Review(
    id: json['id'] as String,
    bookingId: json['bookingId'] as String,
    reviewerUserId: json['reviewerUserId'] as String,
    reviewerFirstName: json['reviewerFirstName'] as String,
    reviewerPhotoUrl: json['reviewerPhotoUrl'] as String?,
    revieweeUserId: json['revieweeUserId'] as String,
    direction: json['direction'] as String,
    rating: json['rating'] as int,
    comment: json['comment'] as String?,
    createdAtUtc: DateTime.parse(json['createdAtUtc'] as String),
  );

  final String id;
  final String bookingId;
  final String reviewerUserId;
  final String reviewerFirstName;
  final String? reviewerPhotoUrl;
  final String revieweeUserId;
  final String direction;
  final int rating;
  final String? comment;
  final DateTime createdAtUtc;
}

class PagedReviews {
  const PagedReviews({required this.items, required this.page, required this.pageSize, required this.totalCount});

  final List<Review> items;
  final int page;
  final int pageSize;
  final int totalCount;

  bool get hasMore => page * pageSize < totalCount;
}
