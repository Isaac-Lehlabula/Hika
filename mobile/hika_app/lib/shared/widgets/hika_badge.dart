import 'package:flutter/material.dart';

import '../../core/theme/hika_colors.dart';
import '../../core/theme/hika_spacing.dart';

enum HikaBadgeTone { success, warning, danger, neutral }

/// Small pill used for verification/status labels ("Verified", "Pending", "Rejected") —
/// one widget so every badge in the app looks the same rather than ad hoc Container styling.
class HikaBadge extends StatelessWidget {
  const HikaBadge({required this.label, required this.tone, super.key, this.icon});

  final String label;
  final HikaBadgeTone tone;
  final IconData? icon;

  (Color background, Color foreground) _colors() => switch (tone) {
    HikaBadgeTone.success => (HikaColors.successLight, HikaColors.success),
    HikaBadgeTone.warning => (HikaColors.warningLight, HikaColors.warning),
    HikaBadgeTone.danger => (HikaColors.dangerLight, HikaColors.danger),
    HikaBadgeTone.neutral => (HikaColors.accentLight, HikaColors.accent),
  };

  @override
  Widget build(BuildContext context) {
    final (background, foreground) = _colors();

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: HikaSpacing.sm, vertical: HikaSpacing.xxs),
      decoration: BoxDecoration(color: background, borderRadius: BorderRadius.circular(HikaRadius.pill)),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (icon != null) ...[Icon(icon, size: 14, color: foreground), const SizedBox(width: 4)],
          Text(
            label,
            style: Theme.of(
              context,
            ).textTheme.labelMedium?.copyWith(color: foreground, fontWeight: FontWeight.w600),
          ),
        ],
      ),
    );
  }
}

extension VerificationStatusBadge on String {
  /// Maps a backend VerificationStatus string ("NotSubmitted"/"Pending"/"Verified"/"Rejected")
  /// to a badge.
  HikaBadge toVerificationBadge() {
    return switch (this) {
      'Verified' => const HikaBadge(label: 'Verified', tone: HikaBadgeTone.success, icon: Icons.verified_rounded),
      'Pending' => const HikaBadge(label: 'Pending review', tone: HikaBadgeTone.warning, icon: Icons.hourglass_top_rounded),
      'Rejected' => const HikaBadge(label: 'Rejected', tone: HikaBadgeTone.danger, icon: Icons.error_outline_rounded),
      _ => const HikaBadge(label: 'Not submitted', tone: HikaBadgeTone.neutral, icon: Icons.upload_file_rounded),
    };
  }
}
