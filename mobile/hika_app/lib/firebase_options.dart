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
    apiKey: 'AIzaSyBb0TS2nP9zLkk4-jUtpVESyuHZy4tbUJs',
    appId: '1:254885278658:web:13a3d624aed5d4acf9dc5b',
    messagingSenderId: '254885278658',
    projectId: 'hiking-spot-f7640',
    authDomain: 'hiking-spot-f7640.firebaseapp.com',
    storageBucket: 'hiking-spot-f7640.firebasestorage.app',
    measurementId: 'G-S3QY1XJVH7',
  );

  static const FirebaseOptions android = FirebaseOptions(
    apiKey: 'AIzaSyCTXfHAwPJY9aWFBU7awvVzcJXG8X8Ylkk',
    appId: '1:254885278658:android:9a66be22b162bfabf9dc5b',
    messagingSenderId: '254885278658',
    projectId: 'hiking-spot-f7640',
    storageBucket: 'hiking-spot-f7640.firebasestorage.app',
  );
  static const FirebaseOptions ios = FirebaseOptions(
    apiKey: 'AIzaSyD0StLnHWZgoj-Mrd04FhxaocKiL9LytQA',
    appId: '1:254885278658:ios:fe4ad6e471482bc7f9dc5b',
    messagingSenderId: '254885278658',
    projectId: 'hiking-spot-f7640',
    storageBucket: 'hiking-spot-f7640.firebasestorage.app',
    iosBundleId: 'com.hika.hikaApp',
  );
}
