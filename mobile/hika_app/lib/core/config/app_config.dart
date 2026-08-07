import 'dart:io' show Platform;

import 'package:flutter/foundation.dart' show kIsWeb;

/// Central place for environment-dependent config. Override at build/run
/// time with `--dart-define=API_BASE_URL=https://...` for staging/prod;
/// the defaults below are for local development only.
abstract final class AppConfig {
  static const String _override = String.fromEnvironment('API_BASE_URL');

  /// The Android emulator's host loopback is 10.0.2.2, not localhost — iOS
  /// simulators and web (Chrome, used for dev/visual verification) can use
  /// localhost directly.
  static String get apiBaseUrl {
    if (_override.isNotEmpty) {
      return _override;
    }
    if (kIsWeb) {
      return 'http://localhost:5080';
    }
    if (Platform.isAndroid) {
      return 'http://10.0.2.2:5080';
    }
    return 'http://localhost:5080';
  }
}
