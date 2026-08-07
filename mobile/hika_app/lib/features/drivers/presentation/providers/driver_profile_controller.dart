import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/networking/api_exception.dart';
import '../../../../core/providers.dart';
import '../../data/driver_profile.dart';

/// Null state means "hasn't created a driver profile yet" — distinct from a loading/error
/// state, so the Become-a-Driver screen can show its form immediately rather than an error.
class DriverProfileController extends AsyncNotifier<DriverProfile?> {
  @override
  Future<DriverProfile?> build() => _load();

  Future<DriverProfile?> _load() async {
    try {
      return await ref.read(driversApiProvider).getOwnDriverProfile();
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

  Future<void> createOrUpdate({required String licenseNumber, required DateTime licenseExpiryDate}) async {
    final profile = await ref
        .read(driversApiProvider)
        .createOrUpdateDriverProfile(licenseNumber: licenseNumber, licenseExpiryDate: licenseExpiryDate);
    state = AsyncData(profile);
  }

  Future<void> submitLicenseVerification(String filePath) async {
    await ref.read(driversApiProvider).submitLicenseVerification(filePath);
    await refresh();
  }
}

final driverProfileControllerProvider = AsyncNotifierProvider<DriverProfileController, DriverProfile?>(
  DriverProfileController.new,
);
