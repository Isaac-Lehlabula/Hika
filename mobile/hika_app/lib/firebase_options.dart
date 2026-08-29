// Placeholder — no real Firebase project exists in this environment yet.
//
// To enable real push delivery:
//   1. Create a Firebase project (console.firebase.google.com) and register the Android/iOS
//      apps (package name / bundle id must match android/app/build.gradle.kts and
//      ios/Runner.xcodeproj).
//   2. Run `flutterfire configure` from mobile/hika_app/ (needs the FlutterFire CLI and a
//      Firebase-account login — both only the project owner can do) and let it overwrite this
//      file with real values.
//   3. Generate a service-account key (Project Settings -> Service Accounts) and set it as
//      Firebase:ServiceAccountJson in the backend's config (see backend/src/Hika.Api/appsettings.json
//      and FirebaseOptions.cs) so the backend can actually send through FCM.
//
// Until then, PushService.init() calls Firebase.initializeApp() with these placeholder values,
// which fails — caught and logged, never crashing the app. Push notifications are additive on
// top of the in-app inbox (see docs/roadmap.md), so this is a safe degraded state.

import 'package:firebase_core/firebase_core.dart' show FirebaseOptions;
import 'package:flutter/foundation.dart' show TargetPlatform, defaultTargetPlatform, kIsWeb;

class DefaultFirebaseOptions {
  DefaultFirebaseOptions._();

  static FirebaseOptions get currentPlatform {
    if (kIsWeb) {
      return web;
    }
    switch (defaultTargetPlatform) {
      case TargetPlatform.android:
        return android;
      case TargetPlatform.iOS:
        return ios;
      default:
        throw UnsupportedError('DefaultFirebaseOptions have not been configured for $defaultTargetPlatform.');
    }
  }

  static const FirebaseOptions web = FirebaseOptions(
    apiKey: 'placeholder-not-configured',
    appId: '1:000000000000:web:0000000000000000000000',
    messagingSenderId: '000000000000',
    projectId: 'hiking-spot-placeholder',
  );

  static const FirebaseOptions android = FirebaseOptions(
    apiKey: 'placeholder-not-configured',
    appId: '1:000000000000:android:0000000000000000000000',
    messagingSenderId: '000000000000',
    projectId: 'hiking-spot-placeholder',
  );

  static const FirebaseOptions ios = FirebaseOptions(
    apiKey: 'placeholder-not-configured',
    appId: '1:000000000000:ios:0000000000000000000000',
    messagingSenderId: '000000000000',
    projectId: 'hiking-spot-placeholder',
    iosBundleId: 'com.hika.hikaApp',
  );
}
