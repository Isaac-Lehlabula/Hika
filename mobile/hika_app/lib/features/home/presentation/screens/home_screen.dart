import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../../core/theme/hika_colors.dart';
import '../../../../core/theme/hika_spacing.dart';
import '../../../../shared/widgets/hika_button.dart';
import '../../../../shared/widgets/hika_card.dart';
import '../../../profile/presentation/providers/profile_controller.dart';
import '../../../search/data/search_models.dart';
import '../../../search/presentation/providers/popular_routes_provider.dart';
import '../../../search/presentation/widgets/location_picker_sheet.dart';
import '../../../search/presentation/widgets/passengers_picker_dialog.dart';

/// The flagship screen: "Where are you going home to?" Search is wired to the real
/// /api/v1/search/trips endpoint (see Phase 5) — the From/To fields autocomplete against the
/// backend's seeded Location table while still accepting free text.
class HomeScreen extends ConsumerStatefulWidget {
  const HomeScreen({super.key});

  @override
  ConsumerState<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends ConsumerState<HomeScreen> {
  String? _from;
  String? _to;
  DateTime? _date;
  int _passengers = 1;

  Future<void> _pickFrom() async {
    final result = await LocationPickerSheet.show(context, title: 'Leaving from', initialValue: _from);
    if (result != null && result.isNotEmpty) {
      setState(() => _from = result);
    }
  }

  Future<void> _pickTo() async {
    final result = await LocationPickerSheet.show(context, title: 'Going to', initialValue: _to);
    if (result != null && result.isNotEmpty) {
      setState(() => _to = result);
    }
  }

  Future<void> _pickDate() async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: _date ?? now,
      firstDate: now,
      lastDate: now.add(const Duration(days: 180)),
    );
    if (picked != null) {
      setState(() => _date = picked);
    }
  }

  Future<void> _pickPassengers() async {
    final picked = await showPassengersPicker(context, initialValue: _passengers);
    if (picked != null) {
      setState(() => _passengers = picked);
    }
  }

  void _search() {
    if (_from == null || _to == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Choose where you\'re leaving from and going to.')),
      );
      return;
    }

    context.push(
      '/search/results',
      extra: SearchTripsQuery(from: _from!, to: _to!, date: _date, passengers: _passengers),
    );
  }

  @override
  Widget build(BuildContext context) {
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
                  _SearchFieldRow(
                    icon: Icons.trip_origin,
                    label: 'From',
                    value: _from,
                    hint: 'Johannesburg',
                    onTap: _pickFrom,
                  ),
                  const Divider(height: HikaSpacing.xl),
                  _SearchFieldRow(
                    icon: Icons.location_on_outlined,
                    label: 'To',
                    value: _to,
                    hint: 'Giyani',
                    onTap: _pickTo,
                  ),
                  const Divider(height: HikaSpacing.xl),
                  Row(
                    children: [
                      Expanded(
                        child: _SearchFieldRow(
                          icon: Icons.calendar_today_outlined,
                          label: 'Date',
                          value: _date == null ? null : DateFormat('d MMM').format(_date!),
                          hint: 'Any date',
                          onTap: _pickDate,
                        ),
                      ),
                      const SizedBox(width: HikaSpacing.md),
                      Expanded(
                        child: _SearchFieldRow(
                          icon: Icons.person_outline,
                          label: 'Passengers',
                          value: '$_passengers',
                          hint: '1',
                          onTap: _pickPassengers,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: HikaSpacing.lg),
                  HikaButton(label: 'Find a Hike', icon: Icons.search, onPressed: _search),
                ],
              ),
            ),
            const SizedBox(height: HikaSpacing.md),
            Center(
              child: HikaButton(
                label: "I'm Driving",
                variant: HikaButtonVariant.text,
                icon: Icons.drive_eta_outlined,
                onPressed: () => context.push('/become-driver'),
              ),
            ),
            const SizedBox(height: HikaSpacing.xl),
            Text('Popular this month', style: theme.textTheme.titleMedium),
            const SizedBox(height: HikaSpacing.sm),
            const _PopularRoutesPreview(),
          ],
        ),
      ),
    );
  }
}

class _SearchFieldRow extends StatelessWidget {
  const _SearchFieldRow({required this.icon, required this.label, required this.hint, required this.onTap, this.value});

  final IconData icon;
  final String label;
  final String? value;
  final String hint;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(HikaRadius.sm),
      child: Row(
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
                Text(
                  value ?? hint,
                  style: theme.textTheme.titleMedium?.copyWith(
                    color: value == null ? theme.colorScheme.onSurface.withValues(alpha: 0.4) : null,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _PopularRoutesPreview extends ConsumerWidget {
  const _PopularRoutesPreview();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final routesAsync = ref.watch(popularRoutesProvider);

    return SizedBox(
      height: 48,
      child: routesAsync.when(
        loading: () => const Center(child: SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))),
        error: (_, _) => const SizedBox.shrink(),
        data: (routes) {
          if (routes.isEmpty) {
            return Center(
              child: Text(
                'No trips posted yet this month — be the first!',
                style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurface.withValues(alpha: 0.5)),
              ),
            );
          }

          return ListView.separated(
            scrollDirection: Axis.horizontal,
            itemCount: routes.length,
            separatorBuilder: (_, _) => const SizedBox(width: HikaSpacing.sm),
            itemBuilder: (context, index) {
              final route = routes[index];
              return ActionChip(
                label: Text(route.label, style: theme.textTheme.labelMedium),
                onPressed: () => context.push(
                  '/search/results',
                  extra: SearchTripsQuery(from: route.originName, to: route.destinationName),
                ),
              );
            },
          );
        },
      ),
    );
  }
}
