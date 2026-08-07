import 'package:flutter_test/flutter_test.dart';
import 'package:hika_app/features/notifications/data/notification.dart';

Map<String, dynamic> _notificationJson({String status = 'Sent', String? readAtUtc}) => {
  'id': 'n1',
  'type': 'BookingRequested',
  'message': 'New booking request',
  'relatedEntityId': 'b1',
  'status': status,
  'createdAtUtc': '2026-12-01T10:00:00Z',
  'readAtUtc': readAtUtc,
};

void main() {
  test('AppNotification.fromJson round-trips the backend NotificationResponse shape', () {
    final notification = AppNotification.fromJson(_notificationJson());

    expect(notification.type, 'BookingRequested');
    expect(notification.message, 'New booking request');
    expect(notification.relatedEntityId, 'b1');
    expect(notification.readAtUtc, isNull);
  });

  test('isUnread is true when status is Sent, false when Read', () {
    expect(AppNotification.fromJson(_notificationJson(status: 'Sent')).isUnread, isTrue);
    expect(AppNotification.fromJson(_notificationJson(status: 'Read', readAtUtc: '2026-12-01T11:00:00Z')).isUnread, isFalse);
  });

  test('PagedNotifications.hasMore is true when more pages remain', () {
    const paged = PagedNotifications(items: [], page: 1, pageSize: 20, totalCount: 25);

    expect(paged.hasMore, isTrue);
  });

  test('PagedNotifications.hasMore is false on the last page', () {
    const paged = PagedNotifications(items: [], page: 2, pageSize: 20, totalCount: 25);

    expect(paged.hasMore, isFalse);
  });

  test('PagedNotifications.unreadCount counts only Sent items in the loaded page', () {
    final paged = PagedNotifications(
      items: [
        AppNotification.fromJson(_notificationJson(status: 'Sent')),
        AppNotification.fromJson(_notificationJson(status: 'Read', readAtUtc: '2026-12-01T11:00:00Z')),
        AppNotification.fromJson(_notificationJson(status: 'Sent')),
      ],
      page: 1,
      pageSize: 20,
      totalCount: 3,
    );

    expect(paged.unreadCount, 2);
  });
}
