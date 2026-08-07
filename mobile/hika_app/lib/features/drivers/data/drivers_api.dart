import 'package:intl/intl.dart';

import '../../../core/networking/api_client.dart';
import 'driver_profile.dart';
import 'vehicle.dart';

/// Mirrors backend/src/Hika.Api/Controllers/DriversController.cs 1:1.
class DriversApi {
  DriversApi(this._client);

  final ApiClient _client;

  Future<DriverProfile> createOrUpdateDriverProfile({
    required String licenseNumber,
    required DateTime licenseExpiryDate,
  }) async {
    final body = await _client.post(
      '/api/v1/drivers/me/driver-profile',
      data: {'licenseNumber': licenseNumber, 'licenseExpiryDate': DateFormat('yyyy-MM-dd').format(licenseExpiryDate)},
    );
    return DriverProfile.fromJson(body!);
  }

  Future<DriverProfile> getOwnDriverProfile() async {
    final body = await _client.get('/api/v1/drivers/me/driver-profile');
    return DriverProfile.fromJson(body!);
  }

  Future<void> submitLicenseVerification(String filePath) =>
      _client.postMultipart('/api/v1/drivers/me/driver-profile/verification-documents', filePath: filePath);

  Future<Vehicle> createVehicle({
    required String make,
    required String model,
    required int year,
    required String color,
    required String registrationNumber,
    required int seatCapacity,
  }) async {
    final body = await _client.post(
      '/api/v1/drivers/me/vehicles',
      data: {
        'make': make,
        'model': model,
        'year': year,
        'color': color,
        'registrationNumber': registrationNumber,
        'seatCapacity': seatCapacity,
      },
    );
    return Vehicle.fromJson(body!);
  }

  Future<Vehicle> updateVehicle({
    required String vehicleId,
    required String make,
    required String model,
    required int year,
    required String color,
    required String registrationNumber,
    required int seatCapacity,
  }) async {
    final body = await _client.put(
      '/api/v1/drivers/me/vehicles/$vehicleId',
      data: {
        'make': make,
        'model': model,
        'year': year,
        'color': color,
        'registrationNumber': registrationNumber,
        'seatCapacity': seatCapacity,
      },
    );
    return Vehicle.fromJson(body!);
  }

  Future<List<Vehicle>> listVehicles() async {
    final list = await _client.getList('/api/v1/drivers/me/vehicles');
    return list.map((v) => Vehicle.fromJson(v as Map<String, dynamic>)).toList();
  }

  Future<Vehicle> getVehicle(String vehicleId) async {
    final body = await _client.get('/api/v1/drivers/me/vehicles/$vehicleId');
    return Vehicle.fromJson(body!);
  }

  Future<void> deleteVehicle(String vehicleId) => _client.delete('/api/v1/drivers/me/vehicles/$vehicleId');

  Future<VehiclePhoto> uploadVehiclePhoto({required String vehicleId, required String filePath, required bool isPrimary}) async {
    final body = await _client.postMultipart(
      '/api/v1/drivers/me/vehicles/$vehicleId/photos',
      filePath: filePath,
      fields: {'isPrimary': isPrimary},
    );
    return VehiclePhoto.fromJson(body!);
  }

  Future<void> submitVehicleVerification({required String vehicleId, required String filePath}) => _client.postMultipart(
    '/api/v1/drivers/me/vehicles/$vehicleId/verification-documents',
    filePath: filePath,
  );
}
