import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/providers.dart';
import '../../data/trip.dart';

/// Riverpod 3.x family notifiers receive their argument via the constructor (not a
/// `build(arg)` override) — see providers/vehicle_detail_controller.dart for precedent.
class TripDetailController extends AsyncNotifier<Trip> {
  TripDetailController(this.tripId);

  final String tripId;

  @override
  Future<Trip> build() => ref.read(tripsApiProvider).getTrip(tripId);

  Future<void> refresh() async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() => ref.read(tripsApiProvider).getTrip(tripId));
  }

  Future<void> cancel() async {
    await ref.read(tripsApiProvider).cancelTrip(tripId);
    await refresh();
  }
}

final tripDetailControllerProvider = AsyncNotifierProvider.family<TripDetailController, Trip, String>(
  TripDetailController.new,
);
