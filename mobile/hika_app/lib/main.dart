import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'core/routing/app_router.dart';
import 'core/theme/hika_theme.dart';

void main() {
  runApp(const ProviderScope(child: HikaApp()));
}

class HikaApp extends ConsumerWidget {
  const HikaApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final router = ref.watch(goRouterProvider);

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
