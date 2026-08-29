import 'dart:io' show Platform;

import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../features/notifications/presentation/notification_routing.dart';
import '../../features/profile/presentation/providers/profile_controller.dart';
import '../../firebase_options.dart';
import '../providers.dart';
import '../routing/app_router.dart';

/// FCM setup: registers this device's token with the backend and deep-links notification taps.
/// Every step is wrapped so a placeholder firebase_options.dart (see that file — no real Firebase
/// project exists in this environment yet) degrades to a no-op instead of crashing the app; the
/// in-app inbox (InboxScreen) is the channel this app actually guarantees regardless.
class PushService {
  PushService(this._ref);

  final Ref _ref;
  bool _initialized = false;

  Future<void> init() async {
    if (_initialized) {
      return;
    }

    try {
      await Firebase.initializeApp(options: DefaultFirebaseOptions.currentPlatform);
    } catch (error) {
      debugPrint('Push notifications unavailable (no Firebase project configured yet): $error');
      return;
    }

    _initialized = true;

    final messaging = FirebaseMessaging.instance;
    final settings = await messaging.requestPermission();
    if (settings.authorizationStatus == AuthorizationStatus.denied) {
      return;
    }

    messaging.onTokenRefresh.listen(_registerToken);
    FirebaseMessaging.onMessageOpenedApp.listen(_handleTap);

    final initialMessage = await messaging.getInitialMessage();
    if (initialMessage != null) {
      _handleTap(initialMessage);
    }

    await registerCurrentToken();
  }

  /// Re-sent right after a fresh login (see main.dart's auth listener) — a device that already
  /// has a token from a previous session needs it re-attached to whichever user just signed in,
  /// since DeviceTokenService reassigns a reused token to its newest owner.
  Future<void> registerCurrentToken() async {
    if (!_initialized) {
      return;
    }

    try {
      final token = await FirebaseMessaging.instance.getToken();
      if (token != null) {
        await _registerToken(token);
      }
    } catch (error) {
      debugPrint('Push token registration failed: $error');
    }
  }

  Future<void> _registerToken(String token) async {
    try {
      await _ref.read(deviceTokensApiProvider).register(token: token, platform: _platform);
    } catch (error) {
      debugPrint('Push token registration failed: $error');
    }
  }

  void _handleTap(RemoteMessage message) {
    final route = notificationRoute(
      type: message.data['type'] as String? ?? '',
      relatedEntityId: message.data['relatedEntityId'] as String?,
      currentUserId: _ref.read(profileControllerProvider).value?.userId,
    );
    if (route != null) {
      _ref.read(goRouterProvider).push(route.path, extra: route.extra);
    }
  }

  String get _platform {
    if (kIsWeb) {
      return 'Web';
    }
    return Platform.isIOS ? 'Ios' : 'Android';
  }
}

final pushServiceProvider = Provider<PushService>((ref) => PushService(ref));
