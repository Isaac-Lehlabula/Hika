import 'package:flutter/material.dart';

import '../../../core/theme/hika_colors.dart';

class SplashScreen extends StatelessWidget {
  const SplashScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      body: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              'Hika',
              style: theme.textTheme.displayLarge?.copyWith(color: HikaColors.primary),
            ),
            const SizedBox(height: 8),
            Text('Find your hike home.', style: theme.textTheme.bodyLarge),
            const SizedBox(height: 32),
            const CircularProgressIndicator(),
          ],
        ),
      ),
    );
  }
}
