import 'package:flutter/material.dart';

import '../../core/theme/hika_spacing.dart';

/// Consistent card padding on top of the themed [Card] shape/border.
class HikaCard extends StatelessWidget {
  const HikaCard({required this.child, super.key, this.padding = const EdgeInsets.all(HikaSpacing.lg), this.onTap});

  final Widget child;
  final EdgeInsets padding;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final content = Padding(padding: padding, child: child);

    return Card(
      clipBehavior: Clip.antiAlias,
      child: onTap == null ? content : InkWell(onTap: onTap, child: content),
    );
  }
}
