/// A typed failure translated from the backend's RFC 7807 ProblemDetails
/// responses (see docs/api-design.md) or from a transport-level failure —
/// UI code should always be able to show something more specific than
/// "something went wrong."
class ApiException implements Exception {
  ApiException({required this.statusCode, required this.message, this.fieldErrors});

  factory ApiException.network() =>
      ApiException(statusCode: 0, message: 'No internet connection. Check your network and try again.');

  factory ApiException.timeout() =>
      ApiException(statusCode: 0, message: 'That took too long. Check your connection and try again.');

  factory ApiException.unknown() =>
      ApiException(statusCode: -1, message: 'Something went wrong. Please try again.');

  final int statusCode;
  final String message;

  /// Field name -> error messages, from ValidationProblemDetails.errors.
  final Map<String, List<String>>? fieldErrors;

  bool get isValidation => fieldErrors != null && fieldErrors!.isNotEmpty;

  bool get isRetryable => statusCode == 0 || statusCode == 408 || statusCode == 429 || statusCode >= 500;

  String? firstFieldError(String field) => fieldErrors?[field]?.firstOrNull;

  @override
  String toString() => message;
}
