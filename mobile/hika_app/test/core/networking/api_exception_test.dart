import 'package:flutter_test/flutter_test.dart';
import 'package:hika_app/core/networking/api_exception.dart';

void main() {
  group('isRetryable', () {
    test('network/timeout/5xx/408/429 are retryable', () {
      expect(ApiException.network().isRetryable, isTrue);
      expect(ApiException.timeout().isRetryable, isTrue);
      expect(ApiException(statusCode: 500, message: 'x').isRetryable, isTrue);
      expect(ApiException(statusCode: 408, message: 'x').isRetryable, isTrue);
      expect(ApiException(statusCode: 429, message: 'x').isRetryable, isTrue);
    });

    test('4xx other than 408/429 are not retryable', () {
      expect(ApiException(statusCode: 400, message: 'x').isRetryable, isFalse);
      expect(ApiException(statusCode: 401, message: 'x').isRetryable, isFalse);
      expect(ApiException(statusCode: 404, message: 'x').isRetryable, isFalse);
      expect(ApiException(statusCode: 409, message: 'x').isRetryable, isFalse);
    });
  });

  group('field errors', () {
    test('isValidation is true only when fieldErrors is non-empty', () {
      expect(ApiException(statusCode: 400, message: 'x').isValidation, isFalse);
      expect(ApiException(statusCode: 400, message: 'x', fieldErrors: const {}).isValidation, isFalse);
      expect(
        ApiException(statusCode: 400, message: 'x', fieldErrors: const {'email': ['Invalid']}).isValidation,
        isTrue,
      );
    });

    test('firstFieldError returns the first message for a field, or null', () {
      final exception = ApiException(
        statusCode: 400,
        message: 'x',
        fieldErrors: const {
          'password': ['Too short', 'Needs a digit'],
        },
      );

      expect(exception.firstFieldError('password'), 'Too short');
      expect(exception.firstFieldError('email'), isNull);
    });
  });
}
