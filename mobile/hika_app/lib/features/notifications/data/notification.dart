/// Mirrors backend Hika.Application.Notifications.Dtos.NotificationResponse.
class AppNotification {
  const AppNotification({
    required this.id,
    required this.type,
    required this.message,
    this.relatedEntityId,
    required this.status,
    required this.createdAtUtc,
    this.readAtUtc,
  });

  factory AppNotification.fromJson(Map<String, dynamic> json) => AppNotification(
    id: json['id'] as String,
    type: json['type'] as String,
    message: json['message'] as String,
    relatedEntityId: json['relatedEntityId'] as String?,
    status: json['status'] as String,
    createdAtUtc: DateTime.parse(json['createdAtUtc'] as String),
    readAtUtc: json['readAtUtc'] == null ? null : DateTime.parse(json['readAtUtc'] as String),
  );

  final String id;
  final String type;
  final String message;
  final String? relatedEntityId;
  final String status;
  final DateTime createdAtUtc;
  final DateTime? readAtUtc;

  bool get isUnread => status == 'Sent';
}

class PagedNotifications {
  const PagedNotifications({required this.items, required this.page, required this.pageSize, required this.totalCount});

  final List<AppNotification> items;
  final int page;
  final int pageSize;
  final int totalCount;

  bool get hasMore => page * pageSize < totalCount;

  int get unreadCount => items.where((n) => n.isUnread).length;
}
