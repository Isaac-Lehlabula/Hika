import 'package:flutter/material.dart';

import '../../../../core/theme/hika_spacing.dart';

/// Shared layout for every auth screen: scrollable (keyboard-safe), a back
/// button when there's somewhere to go back to, a title/subtitle pair, and
/// consistent horizontal padding.
class AuthScaffold extends StatelessWidget {
  const AuthScaffold({
    required this.title,
    required this.children,
    super.key,
    this.subtitle,
    this.showBackButton = true,
  });

  final String title;
  final String? subtitle;
  final List<Widget> children;
  final bool showBackButton;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final canPop = Navigator.of(context).canPop();

    return Scaffold(
      appBar: showBackButton && canPop ? AppBar() : null,
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.fromLTRB(HikaSpacing.xl, HikaSpacing.xl, HikaSpacing.xl, HikaSpacing.xxxl),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              if (!showBackButton || !canPop) const SizedBox(height: HikaSpacing.xl),
              Text(title, style: theme.textTheme.displaySmall),
              if (subtitle != null) ...[
                const SizedBox(height: HikaSpacing.xs),
                Text(
                  subtitle!,
                  style: theme.textTheme.bodyLarge?.copyWith(color: theme.colorScheme.onSurface.withValues(alpha: 0.6)),
                ),
              ],
              const SizedBox(height: HikaSpacing.xxl),
              ...children,
            ],
          ),
        ),
      ),
    );
  }
}
