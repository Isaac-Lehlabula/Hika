import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../features/auth/data/auth_api.dart';
import '../features/auth/presentation/providers/auth_controller.dart';
import '../features/bookings/data/bookings_api.dart';
import '../features/bookings/data/payments_api.dart';
import '../features/drivers/data/drivers_api.dart';
import '../features/notifications/data/notifications_api.dart';
import '../features/profile/data/profile_api.dart';
import '../features/reviews/data/reviews_api.dart';
import '../features/ride_alerts/data/ride_alerts_api.dart';
import '../features/search/data/search_api.dart';
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

final searchApiProvider = Provider<SearchApi>((ref) => SearchApi(ref.watch(apiClientProvider)));

final bookingsApiProvider = Provider<BookingsApi>((ref) => BookingsApi(ref.watch(apiClientProvider)));

final paymentsApiProvider = Provider<PaymentsApi>((ref) => PaymentsApi(ref.watch(apiClientProvider)));

final reviewsApiProvider = Provider<ReviewsApi>((ref) => ReviewsApi(ref.watch(apiClientProvider)));

final notificationsApiProvider = Provider<NotificationsApi>((ref) => NotificationsApi(ref.watch(apiClientProvider)));

final rideAlertsApiProvider = Provider<RideAlertsApi>((ref) => RideAlertsApi(ref.watch(apiClientProvider)));
