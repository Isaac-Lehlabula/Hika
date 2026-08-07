import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/providers.dart';
import '../../data/vehicle.dart';

class VehiclesController extends AsyncNotifier<List<Vehicle>> {
  @override
  Future<List<Vehicle>> build() => ref.read(driversApiProvider).listVehicles();

  Future<void> refresh() async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() => ref.read(driversApiProvider).listVehicles());
  }

  Future<Vehicle> create({
    required String make,
    required String model,
    required int year,
    required String color,
    required String registrationNumber,
    required int seatCapacity,
  }) async {
    final vehicle = await ref
        .read(driversApiProvider)
        .createVehicle(
          make: make,
          model: model,
          year: year,
          color: color,
          registrationNumber: registrationNumber,
          seatCapacity: seatCapacity,
        );
    await refresh();
    return vehicle;
  }

  Future<void> delete(String vehicleId) async {
    await ref.read(driversApiProvider).deleteVehicle(vehicleId);
    await refresh();
  }
}

final vehiclesControllerProvider = AsyncNotifierProvider<VehiclesController, List<Vehicle>>(VehiclesController.new);
