import '../../../core/networking/api_client.dart';
import 'booking.dart';

/// Mirrors backend/src/Hika.Api/Controllers/BookingsController.cs 1:1.
class BookingsApi {
  BookingsApi(this._client);

  final ApiClient _client;

  Future<Booking> requestBooking({
    required String tripId,
    required int boardingStopSequence,
    required int alightingStopSequence,
    required int seatsRequested,
  }) async {
    final body = await _client.post(
      '/api/v1/bookings',
      data: {
        'tripId': tripId,
        'boardingStopSequence': boardingStopSequence,
        'alightingStopSequence': alightingStopSequence,
        'seatsRequested': seatsRequested,
      },
    );
    return Booking.fromJson(body!);
  }

  Future<List<Booking>> getMyBookings() async {
    final list = await _client.getList('/api/v1/bookings/me');
    return list.map((b) => Booking.fromJson(b as Map<String, dynamic>)).toList();
  }

  Future<Booking> getBooking(String bookingId) async {
    final body = await _client.get('/api/v1/bookings/$bookingId');
    return Booking.fromJson(body!);
  }

  Future<Booking> cancelBooking(String bookingId, {String? reason}) async {
    final body = await _client.post('/api/v1/bookings/$bookingId/cancel', data: {'reason': reason});
    return Booking.fromJson(body!);
  }

  Future<List<Booking>> getTripRequests(String tripId) async {
    final list = await _client.getList('/api/v1/bookings/trips/$tripId/requests');
    return list.map((b) => Booking.fromJson(b as Map<String, dynamic>)).toList();
  }

  Future<Booking> acceptBooking(String bookingId) async {
    final body = await _client.post('/api/v1/bookings/$bookingId/accept');
    return Booking.fromJson(body!);
  }

  Future<Booking> declineBooking(String bookingId) async {
    final body = await _client.post('/api/v1/bookings/$bookingId/decline');
    return Booking.fromJson(body!);
  }
}
