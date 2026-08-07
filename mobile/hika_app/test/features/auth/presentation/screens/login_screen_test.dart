import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:hika_app/core/networking/api_client.dart';
import 'package:hika_app/core/networking/api_exception.dart';
import 'package:hika_app/core/providers.dart';
import 'package:hika_app/core/storage/token_storage.dart';
import 'package:hika_app/features/auth/data/auth_api.dart';
import 'package:hika_app/features/auth/data/auth_tokens.dart';
import 'package:hika_app/features/auth/presentation/screens/login_screen.dart';

/// login() is overridden so the underlying ApiClient is never actually
/// called — it exists only to satisfy AuthApi's constructor.
class _FailingAuthApi extends AuthApi {
  _FailingAuthApi() : super(ApiClient(tokenStorage: TokenStorage()));

  @override
  Future<AuthTokens> login({required String email, required String password}) =>
      Future.error(ApiException(statusCode: 401, message: 'Invalid email or password.'));
}

class _SlowAuthApi extends AuthApi {
  _SlowAuthApi() : super(ApiClient(tokenStorage: TokenStorage()));

  @override
  Future<AuthTokens> login({required String email, required String password}) =>
      Future.delayed(const Duration(milliseconds: 500), () => throw ApiException.unknown());
}

void main() {
  testWidgets('shows an error message when login fails', (tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [authApiProvider.overrideWithValue(_FailingAuthApi())],
        child: const MaterialApp(home: LoginScreen()),
      ),
    );

    await tester.enterText(find.widgetWithText(TextField, 'Email'), 'thabo@example.com');
    await tester.enterText(find.widgetWithText(TextField, 'Password'), 'wrong-password');
    await tester.tap(find.text('Log in'));
    await tester.pumpAndSettle();

    expect(find.text('Invalid email or password.'), findsOneWidget);
  });

  testWidgets('shows a loading spinner while the request is in flight', (tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [authApiProvider.overrideWithValue(_SlowAuthApi())],
        child: const MaterialApp(home: LoginScreen()),
      ),
    );

    await tester.enterText(find.widgetWithText(TextField, 'Email'), 'thabo@example.com');
    await tester.enterText(find.widgetWithText(TextField, 'Password'), 'Passw0rd123');
    await tester.tap(find.text('Log in'));
    await tester.pump();

    expect(find.byType(CircularProgressIndicator), findsOneWidget);

    // Let the pending fake request resolve so its timer doesn't leak into
    // the next test.
    await tester.pumpAndSettle(const Duration(seconds: 1));
  });
}
