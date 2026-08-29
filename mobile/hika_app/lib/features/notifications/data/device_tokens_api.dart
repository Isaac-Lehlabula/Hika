import '../../../core/networking/api_client.dart';

/// Mirrors backend/src/Hika.Api/Controllers/DeviceTokensController.cs 1:1. `platform` is one of
/// 'Android' | 'Ios' | 'Web', matching Hika.Domain.Notifications.DevicePlatform's member names
/// (enums serialize by name, see Program.cs's JsonStringEnumConverter).
class DeviceTokensApi {
  DeviceTokensApi(this._client);

  final ApiClient _client;

  Future<void> register({required String token, required String platform}) =>
      _client.post('/api/v1/users/me/device-tokens', data: {'token': token, 'platform': platform});

  Future<void> unregister(String token) =>
      _client.delete('/api/v1/users/me/device-tokens?token=${Uri.encodeQueryComponent(token)}');
}
