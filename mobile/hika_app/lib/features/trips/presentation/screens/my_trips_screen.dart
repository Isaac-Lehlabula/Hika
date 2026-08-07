import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../../core/theme/hika_colors.dart';
import '../../../../core/theme/hika_spacing.dart';
import '../../../../shared/widgets/hika_badge.dart';
import '../../../../shared/widgets/hika_button.dart';
import '../../../../shared/widgets/hika_card.dart';
import '../../../../shared/widgets/hika_empty_state.dart';
import '../../data/trip.dart';
import '../providers/my_trips_controller.dart';

/// The Trips tab: trips the signed-in user has posted as a driver. Booked-trip history joins
/// this list once Bookings (Phase 6) lands.
class MyTripsScreen extends ConsumerWidget {
  const MyTripsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final tripsAsync = ref.watch(myTripsControllerProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Your trips')),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => context.push('/trips/new'),
        icon: const Icon(Icons.add),
        label: const Text('Post a trip'),
      ),
      body: tripsAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => Center(
          child: HikaButton(
            label: 'Try again',
            variant: HikaButtonVariant.secondary,
            onPressed: () => ref.read(myTripsControllerProvider.notifier).refresh(),
          ),
        ),
        data: (trips) {
          if (trips.isEmpty) {
            return HikaEmptyState(
              icon: Icons.route_outlined,
              title: 'No trips posted yet',
              message: 'Post a trip to offer seats to passengers heading your way.',
              action: HikaButton(label: 'Post a trip', icon: Icons.add, onPressed: () => context.push('/trips/new')),
            );
          }

          return RefreshIndicator(
            onRefresh: () => ref.read(myTripsControllerProvider.notifier).refresh(),
            child: ListView.separated(
              padding: const EdgeInsets.fromLTRB(HikaSpacing.lg, HikaSpacing.lg, HikaSpacing.lg, HikaSpacing.huge),
              itemCount: trips.length,
              separatorBuilder: (_, _) => const SizedBox(height: HikaSpacing.md),
              itemBuilder: (context, index) => _TripCard(trip: trips[index]),
            ),
          );
        },
      ),
    );
  }
}

class _TripCard extends StatelessWidget {
  const _TripCard({required this.trip});

  final TripSummary trip;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return HikaCard(
      onTap: () => context.push('/trips/${trip.id}'),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Expanded(
                child: Text(
                  '${trip.originName} → ${trip.destinationName}',
                  style: theme.textTheme.titleMedium,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
              trip.status.toTripStatusBadge(),
            ],
          ),
          const SizedBox(height: HikaSpacing.xxs),
          Text(DateFormat('EEE d MMM, HH:mm').format(trip.departureAtUtc.toLocal()), style: theme.textTheme.bodySmall),
          const SizedBox(height: HikaSpacing.sm),
          Row(
            children: [
              Icon(Icons.event_seat_outlined, size: 16, color: HikaColors.accent),
              const SizedBox(width: HikaSpacing.xxs),
              Text('${trip.minSeatsAvailable} of ${trip.totalSeatsOffered} seats left', style: theme.textTheme.bodySmall),
              const Spacer(),
              Text(
                'R${trip.pricePerSeat.toStringAsFixed(0)} / seat',
                style: theme.textTheme.titleSmall?.copyWith(color: HikaColors.primary),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
