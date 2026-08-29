import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'core/push/push_service.dart';
import 'core/routing/app_router.dart';
import 'core/theme/hika_theme.dart';
import 'features/auth/presentation/providers/auth_controller.dart';

void main() {
  runApp(const ProviderScope(child: HikaApp()));
}

class HikaApp extends ConsumerStatefulWidget {
  const HikaApp({super.key});

  @override
  ConsumerState<HikaApp> createState() => _HikaAppState();
}

class _HikaAppState extends ConsumerState<HikaApp> {
  @override
  void initState() {
    super.initState();
    // Fire-and-forget — see PushService's remarks for why a placeholder Firebase config
    // degrades to a no-op here rather than blocking app startup.
    unawaited(ref.read(pushServiceProvider).init());
  }

  @override
  Widget build(BuildContext context) {
    final router = ref.watch(goRouterProvider);

    // A fresh sign-in on an already-initialized app needs its device token (re-)attached to
    // whichever user just signed in — init() above only covers the already-logged-in-at-launch
    // case.
    ref.listen(authControllerProvider, (previous, next) {
      if (next.status == AuthStatus.authenticated && previous?.status != AuthStatus.authenticated) {
        ref.read(pushServiceProvider).registerCurrentToken();
      }
    });

    return MaterialApp.router(
      title: 'Hiking Spot',
      debugShowCheckedModeBanner: false,
      theme: HikaTheme.light(),
      darkTheme: HikaTheme.dark(),
      themeMode: ThemeMode.system,
      routerConfig: router,
    );
  }
}
