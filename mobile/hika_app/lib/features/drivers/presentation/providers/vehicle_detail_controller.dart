import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/providers.dart';
import '../../data/vehicle.dart';

/// Riverpod 3.x's family notifiers receive their argument via the constructor (not a
/// `build(arg)` override) — see the provider declaration below.
class VehicleDetailController extends AsyncNotifier<Vehicle> {
  VehicleDetailController(this.vehicleId);

  final String vehicleId;

  @override
  Future<Vehicle> build() => ref.read(driversApiProvider).getVehicle(vehicleId);

  Future<void> refresh() async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() => ref.read(driversApiProvider).getVehicle(vehicleId));
  }

  Future<void> uploadPhoto({required String filePath, required bool isPrimary}) async {
    await ref.read(driversApiProvider).uploadVehiclePhoto(vehicleId: vehicleId, filePath: filePath, isPrimary: isPrimary);
    await refresh();
  }

  Future<void> submitVerification(String filePath) async {
    await ref.read(driversApiProvider).submitVehicleVerification(vehicleId: vehicleId, filePath: filePath);
    await refresh();
  }
}

final vehicleDetailControllerProvider = AsyncNotifierProvider.family<VehicleDetailController, Vehicle, String>(
  VehicleDetailController.new,
);
