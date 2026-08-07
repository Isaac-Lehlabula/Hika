import 'package:flutter/material.dart';

import '../../core/theme/hika_colors.dart';
import '../../core/theme/hika_spacing.dart';

/// Standard "nothing here yet" / "coming soon" state — used for both true
/// empty lists and not-yet-built features, so the app never shows a blank
/// screen or a raw error where a friendly explanation belongs instead.
class HikaEmptyState extends StatelessWidget {
  const HikaEmptyState({required this.icon, required this.title, required this.message, super.key, this.action});

  final IconData icon;
  final String title;
  final String message;
  final Widget? action;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Center(
      child: Padding(
        padding: const EdgeInsets.all(HikaSpacing.xxl),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              padding: const EdgeInsets.all(HikaSpacing.lg),
              decoration: BoxDecoration(color: HikaColors.accentLight, shape: BoxShape.circle),
              child: Icon(icon, size: 36, color: HikaColors.accent),
            ),
            const SizedBox(height: HikaSpacing.lg),
            Text(title, style: theme.textTheme.titleLarge, textAlign: TextAlign.center),
            const SizedBox(height: HikaSpacing.xs),
            Text(
              message,
              style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurface.withValues(alpha: 0.6)),
              textAlign: TextAlign.center,
            ),
            if (action != null) ...[const SizedBox(height: HikaSpacing.lg), action!],
          ],
        ),
      ),
    );
  }
}
