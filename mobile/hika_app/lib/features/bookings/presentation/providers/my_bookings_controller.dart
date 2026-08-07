import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/providers.dart';
import '../../data/booking.dart';

class MyBookingsController extends AsyncNotifier<List<Booking>> {
  @override
  Future<List<Booking>> build() => ref.read(bookingsApiProvider).getMyBookings();

  Future<void> refresh() async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() => ref.read(bookingsApiProvider).getMyBookings());
  }

  Future<Booking> request({
    required String tripId,
    required int boardingStopSequence,
    required int alightingStopSequence,
    required int seatsRequested,
  }) async {
    final booking = await ref
        .read(bookingsApiProvider)
        .requestBooking(
          tripId: tripId,
          boardingStopSequence: boardingStopSequence,
          alightingStopSequence: alightingStopSequence,
          seatsRequested: seatsRequested,
        );
    await refresh();
    return booking;
  }

  Future<void> cancel(String bookingId, {String? reason}) async {
    await ref.read(bookingsApiProvider).cancelBooking(bookingId, reason: reason);
    await refresh();
  }
}

final myBookingsControllerProvider = AsyncNotifierProvider<MyBookingsController, List<Booking>>(
  MyBookingsController.new,
);
