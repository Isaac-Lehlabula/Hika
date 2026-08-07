import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/providers.dart';
import '../../data/booking.dart';

/// Riverpod 3.x family notifiers receive their argument via the constructor.
class TripRequestsController extends AsyncNotifier<List<Booking>> {
  TripRequestsController(this.tripId);

  final String tripId;

  @override
  Future<List<Booking>> build() => ref.read(bookingsApiProvider).getTripRequests(tripId);

  Future<void> refresh() async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() => ref.read(bookingsApiProvider).getTripRequests(tripId));
  }

  Future<void> accept(String bookingId) async {
    await ref.read(bookingsApiProvider).acceptBooking(bookingId);
    await refresh();
  }

  Future<void> decline(String bookingId) async {
    await ref.read(bookingsApiProvider).declineBooking(bookingId);
    await refresh();
  }
}

final tripRequestsControllerProvider = AsyncNotifierProvider.family<TripRequestsController, List<Booking>, String>(
  TripRequestsController.new,
);
