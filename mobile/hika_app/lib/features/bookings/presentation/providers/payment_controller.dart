import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/networking/api_exception.dart';
import '../../../../core/providers.dart';
import '../../data/payment.dart';

/// Null state means "no payment yet" (the booking hasn't been accepted) — distinct from a
/// loading/error state, matching the same pattern as DriverProfileController.
class PaymentController extends AsyncNotifier<Payment?> {
  PaymentController(this.bookingId);

  final String bookingId;

  @override
  Future<Payment?> build() => _load();

  Future<Payment?> _load() async {
    try {
      return await ref.read(paymentsApiProvider).getPayment(bookingId);
    } on ApiException catch (e) {
      if (e.statusCode == 404) {
        return null;
      }
      rethrow;
    }
  }

  Future<void> refresh() async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(_load);
  }

  Future<void> refund({required String reason}) async {
    await ref.read(paymentsApiProvider).refundPayment(bookingId, reason: reason);
    await refresh();
  }
}

final paymentControllerProvider = AsyncNotifierProvider.family<PaymentController, Payment?, String>(
  PaymentController.new,
);
