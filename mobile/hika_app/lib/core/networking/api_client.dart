import 'package:dio/dio.dart';

import '../config/app_config.dart';
import '../storage/token_storage.dart';
import 'api_exception.dart';

/// Thin wrapper around Dio: attaches the bearer token to every request,
/// transparently refreshes an expired access token once and retries, and
/// converts every failure into an [ApiException] so feature code never has
/// to know about Dio/HTTP specifics.
class ApiClient {
  ApiClient({required TokenStorage tokenStorage, this.onSessionExpired})
    : _tokenStorage = tokenStorage,
      _dio = Dio(
        BaseOptions(
          baseUrl: AppConfig.apiBaseUrl,
          connectTimeout: const Duration(seconds: 15),
          receiveTimeout: const Duration(seconds: 15),
          headers: {'Content-Type': 'application/json'},
        ),
      ) {
    _dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) async {
          if (!options.extra.containsKey('skipAuth')) {
            final token = await _tokenStorage.readAccessToken();
            if (token != null) {
              options.headers['Authorization'] = 'Bearer $token';
            }
          }
          handler.next(options);
        },
        onError: (error, handler) async {
          final isAuthFailure = error.response?.statusCode == 401;
          final isRetry = error.requestOptions.extra['isRetry'] == true;

          if (isAuthFailure && !isRetry) {
            final refreshed = await _tryRefresh();
            if (refreshed) {
              final options = error.requestOptions;
              options.extra['isRetry'] = true;
              try {
                final response = await _dio.fetch(options);
                handler.resolve(response);
                return;
              } catch (_) {
                // fall through to the original error below
              }
            } else {
              await _tokenStorage.clear();
              onSessionExpired?.call();
            }
          }

          handler.next(error);
        },
      ),
    );
  }

  final Dio _dio;
  final TokenStorage _tokenStorage;

  /// Called when a session can no longer be refreshed — the app should
  /// route back to the login screen.
  final void Function()? onSessionExpired;

  Future<bool> _tryRefresh() async {
    final refreshToken = await _tokenStorage.readRefreshToken();
    if (refreshToken == null) {
      return false;
    }

    try {
      final response = await _dio.post<Map<String, dynamic>>(
        '/api/v1/auth/refresh',
        data: {'refreshToken': refreshToken},
        options: Options(extra: {'skipAuth': true, 'isRetry': true}),
      );

      final data = response.data!;
      await _tokenStorage.saveTokens(
        accessToken: data['accessToken'] as String,
        refreshToken: data['refreshToken'] as String,
      );
      return true;
    } catch (_) {
      return false;
    }
  }

  Future<Map<String, dynamic>?> get(String path, {Map<String, dynamic>? query}) =>
      _send(() => _dio.get<Map<String, dynamic>>(path, queryParameters: query));

  Future<Map<String, dynamic>?> post(String path, {Object? data, bool skipAuth = false}) => _send(
    () => _dio.post<Map<String, dynamic>>(
      path,
      data: data,
      options: skipAuth ? Options(extra: {'skipAuth': true}) : null,
    ),
  );

  Future<Map<String, dynamic>?> put(String path, {Object? data}) =>
      _send(() => _dio.put<Map<String, dynamic>>(path, data: data));

  Future<Map<String, dynamic>?> _send(Future<Response<Map<String, dynamic>>> Function() request) async {
    try {
      final response = await request();
      return response.data;
    } on DioException catch (error) {
      throw _mapError(error);
    }
  }

  ApiException _mapError(DioException error) {
    switch (error.type) {
      case DioExceptionType.connectionError:
        return ApiException.network();
      case DioExceptionType.connectionTimeout:
      case DioExceptionType.sendTimeout:
      case DioExceptionType.receiveTimeout:
        return ApiException.timeout();
      default:
        break;
    }

    final response = error.response;
    if (response == null) {
      return ApiException.unknown();
    }

    final body = response.data;
    if (body is Map<String, dynamic>) {
      final title = body['title'] as String?;
      final detail = body['detail'] as String?;
      final rawErrors = body['errors'];

      Map<String, List<String>>? fieldErrors;
      if (rawErrors is Map) {
        fieldErrors = rawErrors.map(
          (key, value) => MapEntry(key as String, (value as List).map((e) => e.toString()).toList()),
        );
      }

      return ApiException(
        statusCode: response.statusCode ?? -1,
        message: detail ?? title ?? 'Something went wrong. Please try again.',
        fieldErrors: fieldErrors,
      );
    }

    return ApiException(statusCode: response.statusCode ?? -1, message: 'Something went wrong. Please try again.');
  }
}
