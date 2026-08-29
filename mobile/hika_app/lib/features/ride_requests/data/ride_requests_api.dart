import '../../../core/networking/api_client.dart';
import '../../bookings/data/booking.dart';
import 'ride_request.dart';

/// Mirrors backend/src/Hika.Api/Controllers/RideRequestsController.cs 1:1.
class RideRequestsApi {
  RideRequestsApi(this._client);

  final ApiClient _client;

  Future<RideRequest> createRequest({
    required String origin,
    required String destination,
    required DateTime travelDate,
    required int seatsNeeded,
    double? proposedPricePerSeat,
  }) async {
    final body = await _client.post(
      '/api/v1/ride-requests',
      data: {
        'origin': origin,
        'destination': destination,
        'travelDate': '${travelDate.year.toString().padLeft(4, '0')}-${travelDate.month.toString().padLeft(2, '0')}-${travelDate.day.toString().padLeft(2, '0')}',
        'seatsNeeded': seatsNeeded,
        'proposedPricePerSeat': proposedPricePerSeat,
      },
    );
    return RideRequest.fromJson(body!);
  }

  Future<List<RideRequest>> getMyRequests() async {
    final list = await _client.getList('/api/v1/ride-requests/me');
    return list.map((r) => RideRequest.fromJson(r as Map<String, dynamic>)).toList();
  }

  Future<List<RideRequest>> getOpenRequests() async {
    final list = await _client.getList('/api/v1/ride-requests/open');
    return list.map((r) => RideRequest.fromJson(r as Map<String, dynamic>)).toList();
  }

  Future<void> cancelRequest(String requestId) => _client.delete('/api/v1/ride-requests/$requestId');

  Future<Booking> claimRequest({
    required String requestId,
    required String tripId,
    required int boardingStopSequence,
    required int alightingStopSequence,
  }) async {
    final body = await _client.post(
      '/api/v1/ride-requests/$requestId/claim',
      data: {'tripId': tripId, 'boardingStopSequence': boardingStopSequence, 'alightingStopSequence': alightingStopSequence},
    );
    return Booking.fromJson(body!);
  }
}
