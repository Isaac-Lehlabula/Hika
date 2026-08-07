import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/providers.dart';
import '../../data/ride_alert.dart';

class RideAlertsController extends AsyncNotifier<List<RideAlert>> {
  @override
  Future<List<RideAlert>> build() => ref.read(rideAlertsApiProvider).getMyAlerts();

  Future<void> refresh() async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() => ref.read(rideAlertsApiProvider).getMyAlerts());
  }

  Future<RideAlert> create({required String origin, required String destination, DateTime? travelDate}) async {
    final alert = await ref.read(rideAlertsApiProvider).createAlert(origin: origin, destination: destination, travelDate: travelDate);
    await refresh();
    return alert;
  }

  Future<void> delete(String alertId) async {
    await ref.read(rideAlertsApiProvider).deleteAlert(alertId);
    await refresh();
  }
}

final rideAlertsControllerProvider = AsyncNotifierProvider<RideAlertsController, List<RideAlert>>(RideAlertsController.new);
