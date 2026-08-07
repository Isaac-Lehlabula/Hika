import '../../../core/networking/api_client.dart';
import 'payment.dart';

/// Mirrors backend/src/Hika.Api/Controllers/PaymentsController.cs 1:1.
class PaymentsApi {
  PaymentsApi(this._client);

  final ApiClient _client;

  Future<Payment> getPayment(String bookingId) async {
    final body = await _client.get('/api/v1/bookings/$bookingId/payment');
    return Payment.fromJson(body!);
  }

  Future<Payment> refundPayment(String bookingId, {required String reason}) async {
    final body = await _client.post('/api/v1/bookings/$bookingId/refund', data: {'reason': reason});
    return Payment.fromJson(body!);
  }
}
