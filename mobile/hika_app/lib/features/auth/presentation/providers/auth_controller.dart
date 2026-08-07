import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/providers.dart';

enum AuthStatus { checking, authenticated, unauthenticated }

class AuthState {
  const AuthState({required this.status});

  final AuthStatus status;

  bool get isAuthenticated => status == AuthStatus.authenticated;
}

/// Owns the session lifecycle (login/register/logout, and reacting to a
/// refresh-token failure reported by [ApiClient]). Screens read
/// [authControllerProvider] to decide whether to show the auth flow or the
/// main app shell — see core/routing/app_router.dart.
class AuthController extends Notifier<AuthState> {
  @override
  AuthState build() {
    // Fire-and-forget: the initial "checking" state covers the gap until
    // this resolves, so `build` itself must return synchronously.
    Future.microtask(_bootstrap);
    return const AuthState(status: AuthStatus.checking);
  }

  Future<void> _bootstrap() async {
    final token = await ref.read(tokenStorageProvider).readAccessToken();
    state = AuthState(status: token != null ? AuthStatus.authenticated : AuthStatus.unauthenticated);
  }

  Future<String> register({
    required String email,
    required String password,
    required String firstName,
    required String lastName,
    String? phoneNumber,
  }) => ref
      .read(authApiProvider)
      .register(email: email, password: password, firstName: firstName, lastName: lastName, phoneNumber: phoneNumber);

  Future<void> login({required String email, required String password}) async {
    final tokens = await ref.read(authApiProvider).login(email: email, password: password);
    await ref
        .read(tokenStorageProvider)
        .saveTokens(accessToken: tokens.accessToken, refreshToken: tokens.refreshToken);
    state = const AuthState(status: AuthStatus.authenticated);
  }

  Future<void> signOut() async {
    final refreshToken = await ref.read(tokenStorageProvider).readRefreshToken();
    if (refreshToken != null) {
      try {
        await ref.read(authApiProvider).logout(refreshToken: refreshToken);
      } catch (_) {
        // Best-effort — the local tokens are cleared below regardless, so the
        // device is signed out even if the revoke call itself fails.
      }
    }
    await ref.read(tokenStorageProvider).clear();
    state = const AuthState(status: AuthStatus.unauthenticated);
  }

  /// Called by [ApiClient] when a refresh attempt fails — tokens are already
  /// cleared by that point, this just flips the app back to the auth flow.
  void handleSessionExpired() {
    state = const AuthState(status: AuthStatus.unauthenticated);
  }
}

final authControllerProvider = NotifierProvider<AuthController, AuthState>(AuthController.new);
