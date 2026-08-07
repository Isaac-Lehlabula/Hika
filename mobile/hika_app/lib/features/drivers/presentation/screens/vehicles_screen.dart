import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/theme/hika_colors.dart';
import '../../../../core/theme/hika_spacing.dart';
import '../../../../shared/widgets/hika_badge.dart';
import '../../../../shared/widgets/hika_button.dart';
import '../../../../shared/widgets/hika_card.dart';
import '../../../../shared/widgets/hika_empty_state.dart';
import '../../data/vehicle.dart';
import '../providers/vehicles_controller.dart';

class VehiclesScreen extends ConsumerWidget {
  const VehiclesScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final vehiclesAsync = ref.watch(vehiclesControllerProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('My vehicles')),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => context.push('/vehicles/new'),
        icon: const Icon(Icons.add),
        label: const Text('Add vehicle'),
      ),
      body: vehiclesAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => Center(
          child: HikaButton(
            label: 'Try again',
            variant: HikaButtonVariant.secondary,
            onPressed: () => ref.read(vehiclesControllerProvider.notifier).refresh(),
          ),
        ),
        data: (vehicles) {
          if (vehicles.isEmpty) {
            return const HikaEmptyState(
              icon: Icons.directions_car_outlined,
              title: 'No vehicles yet',
              message: 'Add a vehicle to start posting trips as a driver.',
            );
          }

          return RefreshIndicator(
            onRefresh: () => ref.read(vehiclesControllerProvider.notifier).refresh(),
            child: ListView.separated(
              padding: const EdgeInsets.all(HikaSpacing.lg),
              itemCount: vehicles.length,
              separatorBuilder: (_, _) => const SizedBox(height: HikaSpacing.md),
              itemBuilder: (context, index) => _VehicleCard(vehicle: vehicles[index]),
            ),
          );
        },
      ),
    );
  }
}

class _VehicleCard extends StatelessWidget {
  const _VehicleCard({required this.vehicle});

  final Vehicle vehicle;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return HikaCard(
      onTap: () => context.push('/vehicles/${vehicle.id}'),
      child: Row(
        children: [
          ClipRRect(
            borderRadius: BorderRadius.circular(HikaRadius.sm),
            child: vehicle.primaryPhoto == null
                ? Container(
                    width: 64,
                    height: 64,
                    color: HikaColors.surfaceAltLight,
                    child: const Icon(Icons.directions_car_outlined, color: HikaColors.textSecondaryLight),
                  )
                : Image.network(vehicle.primaryPhoto!.url, width: 64, height: 64, fit: BoxFit.cover),
          ),
          const SizedBox(width: HikaSpacing.md),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(vehicle.displayName, style: theme.textTheme.titleMedium),
                Text('${vehicle.color} · ${vehicle.registrationNumber}', style: theme.textTheme.bodySmall),
                const SizedBox(height: HikaSpacing.xxs),
                (vehicle.isVerified ? 'Verified' : 'NotSubmitted').toVerificationBadge(),
              ],
            ),
          ),
          const Icon(Icons.chevron_right, color: HikaColors.textSecondaryLight),
        ],
      ),
    );
  }
}
