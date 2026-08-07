import '../../../core/networking/api_client.dart';
import 'auth_tokens.dart';

/// Mirrors backend/src/Hika.Api/Controllers/AuthController.cs 1:1.
class AuthApi {
  AuthApi(this._client);

  final ApiClient _client;

  Future<String> register({
    required String email,
    required String password,
    required String firstName,
    required String lastName,
    String? phoneNumber,
  }) async {
    final body = await _client.post(
      '/api/v1/auth/register',
      skipAuth: true,
      data: {
        'email': email,
        'password': password,
        'firstName': firstName,
        'lastName': lastName,
        if (phoneNumber != null) 'phoneNumber': phoneNumber,
      },
    );
    return body!['userId'] as String;
  }

  Future<AuthTokens> login({required String email, required String password}) async {
    final body = await _client.post(
      '/api/v1/auth/login',
      skipAuth: true,
      data: {'email': email, 'password': password},
    );
    return AuthTokens.fromJson(body!);
  }

  Future<void> logout({required String refreshToken}) => _client.post(
    '/api/v1/auth/logout',
    skipAuth: true,
    data: {'refreshToken': refreshToken},
  );

  Future<void> verifyEmail({required String userId, required String token}) => _client.post(
    '/api/v1/auth/verify-email',
    skipAuth: true,
    data: {'userId': userId, 'token': token},
  );

  Future<void> resendVerificationEmail({required String email}) =>
      _client.post('/api/v1/auth/resend-verification-email', skipAuth: true, data: {'email': email});

  Future<void> requestPhoneOtp({required String phoneNumber}) =>
      _client.post('/api/v1/auth/request-phone-otp', data: {'phoneNumber': phoneNumber});

  Future<void> verifyPhone({required String code}) =>
      _client.post('/api/v1/auth/verify-phone', data: {'code': code});

  Future<void> forgotPassword({required String email}) =>
      _client.post('/api/v1/auth/forgot-password', skipAuth: true, data: {'email': email});

  Future<void> resetPassword({required String userId, required String token, required String newPassword}) =>
      _client.post(
        '/api/v1/auth/reset-password',
        skipAuth: true,
        data: {'userId': userId, 'token': token, 'newPassword': newPassword},
      );
}
