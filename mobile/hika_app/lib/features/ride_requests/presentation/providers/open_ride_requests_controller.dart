import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/providers.dart';
import '../../data/ride_request.dart';

/// The "demand board" drivers browse — every rider's open, unexpired request, regardless of who
/// posted it (unlike MyRideRequestsController, which is scoped to the signed-in rider's own).
class OpenRideRequestsController extends AsyncNotifier<List<RideRequest>> {
  @override
  Future<List<RideRequest>> build() => ref.read(rideRequestsApiProvider).getOpenRequests();

  Future<void> refresh() async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() => ref.read(rideRequestsApiProvider).getOpenRequests());
  }
}

final openRideRequestsControllerProvider = AsyncNotifierProvider<OpenRideRequestsController, List<RideRequest>>(
  OpenRideRequestsController.new,
);
