import 'package:flutter/material.dart';

enum HikaButtonVariant { primary, secondary, text }

/// The one button widget every screen uses — carries its own loading state
/// so callers don't hand-roll a spinner-vs-label swap per screen.
class HikaButton extends StatelessWidget {
  const HikaButton({
    required this.label,
    required this.onPressed,
    super.key,
    this.variant = HikaButtonVariant.primary,
    this.isLoading = false,
    this.icon,
  });

  final String label;
  final VoidCallback? onPressed;
  final HikaButtonVariant variant;
  final bool isLoading;
  final IconData? icon;

  @override
  Widget build(BuildContext context) {
    final effectiveOnPressed = isLoading ? null : onPressed;
    final child = isLoading
        ? SizedBox(
            height: 22,
            width: 22,
            child: CircularProgressIndicator(
              strokeWidth: 2.5,
              color: variant == HikaButtonVariant.primary
                  ? Theme.of(context).colorScheme.onPrimary
                  : Theme.of(context).colorScheme.primary,
            ),
          )
        : Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              if (icon != null) ...[Icon(icon, size: 20), const SizedBox(width: 8)],
              Text(label),
            ],
          );

    return switch (variant) {
      HikaButtonVariant.primary => ElevatedButton(onPressed: effectiveOnPressed, child: child),
      HikaButtonVariant.secondary => OutlinedButton(onPressed: effectiveOnPressed, child: child),
      HikaButtonVariant.text => TextButton(onPressed: effectiveOnPressed, child: child),
    };
  }
}
