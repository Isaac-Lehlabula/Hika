import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/providers.dart';
import '../../data/trip.dart';

class MyTripsController extends AsyncNotifier<List<TripSummary>> {
  @override
  Future<List<TripSummary>> build() => ref.read(tripsApiProvider).getMyTrips();

  Future<void> refresh() async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() => ref.read(tripsApiProvider).getMyTrips());
  }

  Future<Trip> create({
    required String vehicleId,
    required DateTime departureAtUtc,
    required int totalSeatsOffered,
    required double pricePerSeat,
    String? luggageAllowance,
    String? notes,
    required List<TripStopInput> stops,
  }) async {
    final trip = await ref
        .read(tripsApiProvider)
        .createTrip(
          vehicleId: vehicleId,
          departureAtUtc: departureAtUtc,
          totalSeatsOffered: totalSeatsOffered,
          pricePerSeat: pricePerSeat,
          luggageAllowance: luggageAllowance,
          notes: notes,
          stops: stops,
        );
    await refresh();
    return trip;
  }
}

final myTripsControllerProvider = AsyncNotifierProvider<MyTripsController, List<TripSummary>>(
  MyTripsController.new,
);
