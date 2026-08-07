import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/auth/presentation/providers/auth_controller.dart';
import '../../features/auth/presentation/screens/forgot_password_screen.dart';
import '../../features/auth/presentation/screens/login_screen.dart';
import '../../features/auth/presentation/screens/register_screen.dart';
import '../../features/auth/presentation/screens/reset_password_screen.dart';
import '../../features/auth/presentation/screens/verify_email_screen.dart';
import '../../features/auth/presentation/screens/verify_phone_screen.dart';
import '../../features/drivers/presentation/screens/add_vehicle_screen.dart';
import '../../features/drivers/presentation/screens/become_driver_screen.dart';
import '../../features/drivers/presentation/screens/vehicle_detail_screen.dart';
import '../../features/drivers/presentation/screens/vehicles_screen.dart';
import '../../features/search/data/search_models.dart';
import '../../features/search/presentation/screens/search_results_screen.dart';
import '../../features/shell/presentation/app_shell.dart';
import '../../features/shell/presentation/splash_screen.dart';
import '../../features/trips/presentation/screens/post_trip_screen.dart';
import '../../features/trips/presentation/screens/trip_detail_screen.dart';

const _authRoutes = {'/login', '/register', '/verify-email', '/forgot-password', '/reset-password'};

/// Recreated whenever [authControllerProvider] changes (via `ref.watch`),
/// which is what makes `redirect` below re-run on login/logout without any
/// manual `ChangeNotifier`/stream plumbing.
final goRouterProvider = Provider<GoRouter>((ref) {
  final authState = ref.watch(authControllerProvider);

  return GoRouter(
    initialLocation: '/',
    redirect: (context, state) {
      final atSplash = state.matchedLocation == '/';

      switch (authState.status) {
        case AuthStatus.checking:
          return atSplash ? null : '/';
        case AuthStatus.unauthenticated:
          return _authRoutes.contains(state.matchedLocation) ? null : '/login';
        case AuthStatus.authenticated:
          return atSplash || _authRoutes.contains(state.matchedLocation) ? '/home' : null;
      }
    },
    routes: [
      GoRoute(path: '/', builder: (context, state) => const SplashScreen()),
      GoRoute(path: '/login', builder: (context, state) => const LoginScreen()),
      GoRoute(path: '/register', builder: (context, state) => const RegisterScreen()),
      GoRoute(
        path: '/verify-email',
        builder: (context, state) {
          final extra = state.extra! as Map<String, String>;
          return VerifyEmailScreen(userId: extra['userId']!, email: extra['email']!);
        },
      ),
      GoRoute(path: '/forgot-password', builder: (context, state) => const ForgotPasswordScreen()),
      GoRoute(path: '/reset-password', builder: (context, state) => const ResetPasswordScreen()),
      GoRoute(path: '/verify-phone', builder: (context, state) => const VerifyPhoneScreen()),
      GoRoute(path: '/home', builder: (context, state) => const AppShell()),
      GoRoute(path: '/become-driver', builder: (context, state) => const BecomeDriverScreen()),
      GoRoute(path: '/vehicles', builder: (context, state) => const VehiclesScreen()),
      GoRoute(path: '/vehicles/new', builder: (context, state) => const AddVehicleScreen()),
      GoRoute(
        path: '/vehicles/:id',
        builder: (context, state) => VehicleDetailScreen(vehicleId: state.pathParameters['id']!),
      ),
      GoRoute(path: '/trips/new', builder: (context, state) => const PostTripScreen()),
      GoRoute(
        path: '/trips/:id',
        builder: (context, state) => TripDetailScreen(tripId: state.pathParameters['id']!),
      ),
      GoRoute(
        path: '/search/results',
        builder: (context, state) => SearchResultsScreen(query: state.extra! as SearchTripsQuery),
      ),
    ],
  );
});
