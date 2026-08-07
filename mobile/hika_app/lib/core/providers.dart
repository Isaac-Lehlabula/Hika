import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../features/auth/data/auth_api.dart';
import '../features/auth/presentation/providers/auth_controller.dart';
import '../features/drivers/data/drivers_api.dart';
import '../features/profile/data/profile_api.dart';
import '../features/trips/data/trips_api.dart';
import 'networking/api_client.dart';
import 'storage/token_storage.dart';

/// App-wide infrastructure providers. Feature-level providers live next to
/// their feature (see features/*/presentation/providers).
final tokenStorageProvider = Provider<TokenStorage>((ref) => TokenStorage());

final apiClientProvider = Provider<ApiClient>((ref) {
  return ApiClient(
    tokenStorage: ref.watch(tokenStorageProvider),
    onSessionExpired: () => ref.read(authControllerProvider.notifier).handleSessionExpired(),
  );
});

final authApiProvider = Provider<AuthApi>((ref) => AuthApi(ref.watch(apiClientProvider)));

final profileApiProvider = Provider<ProfileApi>((ref) => ProfileApi(ref.watch(apiClientProvider)));

final driversApiProvider = Provider<DriversApi>((ref) => DriversApi(ref.watch(apiClientProvider)));

final tripsApiProvider = Provider<TripsApi>((ref) => TripsApi(ref.watch(apiClientProvider)));
