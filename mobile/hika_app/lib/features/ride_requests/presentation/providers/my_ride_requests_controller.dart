import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/providers.dart';
import '../../data/ride_request.dart';

class MyRideRequestsController extends AsyncNotifier<List<RideRequest>> {
  @override
  Future<List<RideRequest>> build() => ref.read(rideRequestsApiProvider).getMyRequests();

  Future<void> refresh() async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() => ref.read(rideRequestsApiProvider).getMyRequests());
  }

  Future<RideRequest> create({
    required String origin,
    required String destination,
    required DateTime travelDate,
    required int seatsNeeded,
    double? proposedPricePerSeat,
  }) async {
    final request = await ref
        .read(rideRequestsApiProvider)
        .createRequest(
          origin: origin,
          destination: destination,
          travelDate: travelDate,
          seatsNeeded: seatsNeeded,
          proposedPricePerSeat: proposedPricePerSeat,
        );
    await refresh();
    return request;
  }

  Future<void> cancel(String requestId) async {
    await ref.read(rideRequestsApiProvider).cancelRequest(requestId);
    await refresh();
  }
}

final myRideRequestsControllerProvider = AsyncNotifierProvider<MyRideRequestsController, List<RideRequest>>(
  MyRideRequestsController.new,
);
