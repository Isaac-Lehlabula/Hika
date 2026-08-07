import '../../../core/networking/api_client.dart';
import 'trip.dart';

/// Mirrors backend/src/Hika.Api/Controllers/TripsController.cs 1:1.
class TripsApi {
  TripsApi(this._client);

  final ApiClient _client;

  Future<Trip> createTrip({
    required String vehicleId,
    required DateTime departureAtUtc,
    required int totalSeatsOffered,
    required double pricePerSeat,
    String? luggageAllowance,
    String? notes,
    required List<TripStopInput> stops,
  }) async {
    final body = await _client.post(
      '/api/v1/trips',
      data: {
        'vehicleId': vehicleId,
        'departureAtUtc': departureAtUtc.toUtc().toIso8601String(),
        'totalSeatsOffered': totalSeatsOffered,
        'pricePerSeat': pricePerSeat,
        'luggageAllowance': luggageAllowance,
        'notes': notes,
        'stops': [
          for (final stop in stops) {'rawName': stop.rawName, 'province': stop.province.wireValue},
        ],
      },
    );
    return Trip.fromJson(body!);
  }

  Future<Trip> getTrip(String tripId) async {
    final body = await _client.get('/api/v1/trips/$tripId');
    return Trip.fromJson(body!);
  }

  Future<List<TripSummary>> getMyTrips() async {
    final list = await _client.getList('/api/v1/trips/me');
    return list.map((t) => TripSummary.fromJson(t as Map<String, dynamic>)).toList();
  }

  Future<void> cancelTrip(String tripId) async {
    await _client.post('/api/v1/trips/$tripId/cancel');
  }
}
