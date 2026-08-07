import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/theme/hika_colors.dart';
import '../../../../core/theme/hika_spacing.dart';
import '../../../../shared/widgets/hika_button.dart';
import '../../../../shared/widgets/hika_card.dart';
import '../../../profile/presentation/providers/profile_controller.dart';

/// The flagship screen: "Where are you going home to?" The search fields
/// are visually complete per the product brief but not yet wired to a
/// backend — trip search lands in Phase 5 (see docs/roadmap.md). Submitting
/// is honest about that rather than pretending to search.
class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final firstName = ref.watch(profileControllerProvider).value?.firstName;

    return Scaffold(
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(HikaSpacing.lg),
          children: [
            Text(
              firstName == null ? 'Where are you going home to?' : 'Hi $firstName, where are you going home to?',
              style: theme.textTheme.headlineMedium,
            ),
            const SizedBox(height: HikaSpacing.xs),
            Text(
              'Find someone already heading your way.',
              style: theme.textTheme.bodyLarge?.copyWith(color: theme.colorScheme.onSurface.withValues(alpha: 0.6)),
            ),
            const SizedBox(height: HikaSpacing.xl),
            HikaCard(
              child: Column(
                children: [
                  const _SearchFieldRow(icon: Icons.trip_origin, label: 'From', hint: 'Johannesburg'),
                  const Divider(height: HikaSpacing.xl),
                  const _SearchFieldRow(icon: Icons.location_on_outlined, label: 'To', hint: 'Giyani'),
                  const Divider(height: HikaSpacing.xl),
                  Row(
                    children: [
                      Expanded(
                        child: _SearchFieldRow(icon: Icons.calendar_today_outlined, label: 'Date', hint: '20 Dec'),
                      ),
                      const SizedBox(width: HikaSpacing.md),
                      Expanded(
                        child: _SearchFieldRow(icon: Icons.person_outline, label: 'Passengers', hint: '1'),
                      ),
                    ],
                  ),
                  const SizedBox(height: HikaSpacing.lg),
                  HikaButton(
                    label: 'Find a Hike',
                    icon: Icons.search,
                    onPressed: () => _showComingSoon(context, 'Trip search'),
                  ),
                ],
              ),
            ),
            const SizedBox(height: HikaSpacing.md),
            Center(
              child: HikaButton(
                label: "I'm Driving",
                variant: HikaButtonVariant.text,
                icon: Icons.drive_eta_outlined,
                onPressed: () => _showComingSoon(context, 'Posting a trip'),
              ),
            ),
            const SizedBox(height: HikaSpacing.xl),
            Text('Popular this December', style: theme.textTheme.titleMedium),
            const SizedBox(height: HikaSpacing.sm),
            const _PopularRoutesPreview(),
          ],
        ),
      ),
    );
  }

  void _showComingSoon(BuildContext context, String feature) {
    ScaffoldMessenger.of(
      context,
    ).showSnackBar(SnackBar(content: Text('$feature is coming soon — the auth flow is fully working today.')));
  }
}

class _SearchFieldRow extends StatelessWidget {
  const _SearchFieldRow({required this.icon, required this.label, required this.hint});

  final IconData icon;
  final String label;
  final String hint;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Row(
      children: [
        Icon(icon, color: HikaColors.accent, size: 20),
        const SizedBox(width: HikaSpacing.sm),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                label.toUpperCase(),
                style: theme.textTheme.labelMedium?.copyWith(letterSpacing: 0.5),
              ),
              Text(hint, style: theme.textTheme.titleMedium),
            ],
          ),
        ),
      ],
    );
  }
}

class _PopularRoutesPreview extends StatelessWidget {
  const _PopularRoutesPreview();

  static const _routes = [
    'Johannesburg → Polokwane',
    'Johannesburg → Giyani',
    'Pretoria → Thohoyandou',
    'Johannesburg → Durban',
    'Cape Town → Mthatha',
  ];

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return SizedBox(
      height: 48,
      child: ListView.separated(
        scrollDirection: Axis.horizontal,
        itemCount: _routes.length,
        separatorBuilder: (_, _) => const SizedBox(width: HikaSpacing.sm),
        itemBuilder: (context, index) => Chip(label: Text(_routes[index], style: theme.textTheme.labelMedium)),
      ),
    );
  }
}
