import 'package:intl/intl.dart';

import '../../../core/networking/api_client.dart';
import 'ride_alert.dart';

/// Mirrors backend/src/Hika.Api/Controllers/RideAlertsController.cs 1:1.
class RideAlertsApi {
  RideAlertsApi(this._client);

  final ApiClient _client;

  Future<RideAlert> createAlert({required String origin, required String destination, DateTime? travelDate}) async {
    final body = await _client.post(
      '/api/v1/ride-alerts',
      data: {
        'origin': origin,
        'destination': destination,
        'travelDate': travelDate == null ? null : DateFormat('yyyy-MM-dd').format(travelDate),
      },
    );
    return RideAlert.fromJson(body!);
  }

  Future<List<RideAlert>> getMyAlerts() async {
    final list = await _client.getList('/api/v1/ride-alerts/me');
    return list.map((a) => RideAlert.fromJson(a as Map<String, dynamic>)).toList();
  }

  Future<void> deleteAlert(String alertId) => _client.delete('/api/v1/ride-alerts/$alertId');
}
