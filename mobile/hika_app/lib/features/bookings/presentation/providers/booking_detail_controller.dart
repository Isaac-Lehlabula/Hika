import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/providers.dart';
import '../../data/booking.dart';

/// Riverpod 3.x family notifiers receive their argument via the constructor (not a
/// `build(arg)` override) — see providers/vehicle_detail_controller.dart for precedent.
class BookingDetailController extends AsyncNotifier<Booking> {
  BookingDetailController(this.bookingId);

  final String bookingId;

  @override
  Future<Booking> build() => ref.read(bookingsApiProvider).getBooking(bookingId);

  Future<void> refresh() async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() => ref.read(bookingsApiProvider).getBooking(bookingId));
  }

  Future<void> cancel({String? reason}) async {
    await ref.read(bookingsApiProvider).cancelBooking(bookingId, reason: reason);
    await refresh();
  }
}

final bookingDetailControllerProvider = AsyncNotifierProvider.family<BookingDetailController, Booking, String>(
  BookingDetailController.new,
);
