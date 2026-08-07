import '../../../core/networking/api_client.dart';
import 'notification.dart';

/// Mirrors backend/src/Hika.Api/Controllers/NotificationsController.cs 1:1.
class NotificationsApi {
  NotificationsApi(this._client);

  final ApiClient _client;

  Future<PagedNotifications> getMyNotifications({int page = 1, int pageSize = 20}) async {
    final body = await _client.get('/api/v1/notifications/me', query: {'page': page, 'pageSize': pageSize});
    final items = (body!['items'] as List<dynamic>).map((n) => AppNotification.fromJson(n as Map<String, dynamic>)).toList();

    return PagedNotifications(items: items, page: body['page'] as int, pageSize: body['pageSize'] as int, totalCount: body['totalCount'] as int);
  }

  Future<void> markRead(String notificationId) async {
    await _client.post('/api/v1/notifications/me/$notificationId/read');
  }
}
