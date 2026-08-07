import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../../core/networking/api_exception.dart';
import '../../../../core/theme/hika_colors.dart';
import '../../../../core/theme/hika_spacing.dart';
import '../../../../shared/widgets/hika_badge.dart';
import '../../../../shared/widgets/hika_button.dart';
import '../../../../shared/widgets/hika_card.dart';
import '../../../profile/presentation/providers/profile_controller.dart';
import '../../data/province.dart';
import '../../data/trip.dart';
import '../providers/my_trips_controller.dart';
import '../providers/trip_detail_controller.dart';

class TripDetailScreen extends ConsumerStatefulWidget {
  const TripDetailScreen({required this.tripId, super.key});

  final String tripId;

  @override
  ConsumerState<TripDetailScreen> createState() => _TripDetailScreenState();
}

class _TripDetailScreenState extends ConsumerState<TripDetailScreen> {
  bool _isCancelling = false;

  Future<void> _cancel() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Cancel this trip?'),
        content: const Text('Passengers who booked seats will be notified. This cannot be undone.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Keep trip')),
          TextButton(onPressed: () => Navigator.pop(context, true), child: const Text('Cancel trip')),
        ],
      ),
    );
    if (confirmed != true) {
      return;
    }

    setState(() => _isCancelling = true);
    try {
      await ref.read(tripDetailControllerProvider(widget.tripId).notifier).cancel();
      ref.invalidate(myTripsControllerProvider);
    } on ApiException catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.message)));
      }
    } finally {
      if (mounted) {
        setState(() => _isCancelling = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final tripAsync = ref.watch(tripDetailControllerProvider(widget.tripId));
    final userId = ref.watch(profileControllerProvider).value?.userId;

    return Scaffold(
      appBar: AppBar(title: const Text('Trip details')),
      body: tripAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => Center(
          child: HikaButton(
            label: 'Try again',
            variant: HikaButtonVariant.secondary,
            onPressed: () => ref.read(tripDetailControllerProvider(widget.tripId).notifier).refresh(),
          ),
        ),
        data: (trip) {
          final isOwner = trip.driver.userId == userId;

          return RefreshIndicator(
            onRefresh: () => ref.read(tripDetailControllerProvider(widget.tripId).notifier).refresh(),
            child: ListView(
              padding: const EdgeInsets.all(HikaSpacing.lg),
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text('${trip.origin.name} → ${trip.destination.name}', style: Theme.of(context).textTheme.headlineSmall),
                    trip.status.toTripStatusBadge(),
                  ],
                ),
                const SizedBox(height: HikaSpacing.xs),
                Text(
                  DateFormat('EEEE d MMMM, HH:mm').format(trip.departureAtUtc.toLocal()),
                  style: Theme.of(context).textTheme.bodyLarge?.copyWith(color: HikaColors.textSecondaryLight),
                ),
                const SizedBox(height: HikaSpacing.xl),
                _DriverCard(driver: trip.driver, vehicle: trip.vehicle),
                const SizedBox(height: HikaSpacing.lg),
                _RouteCard(trip: trip),
                const SizedBox(height: HikaSpacing.lg),
                _DetailsCard(trip: trip),
                if (isOwner) ...[
                  const SizedBox(height: HikaSpacing.xl),
                  HikaButton(
                    label: 'Booking requests',
                    icon: Icons.event_seat_outlined,
                    onPressed: () => context.push('/trips/${trip.id}/requests'),
                  ),
                  if (trip.status == 'Scheduled') ...[
                    const SizedBox(height: HikaSpacing.sm),
                    HikaButton(
                      label: 'Cancel trip',
                      variant: HikaButtonVariant.secondary,
                      icon: Icons.cancel_outlined,
                      isLoading: _isCancelling,
                      onPressed: _cancel,
                    ),
                  ],
                ] else if (trip.status == 'Scheduled' && trip.minSeatsAvailable > 0) ...[
                  const SizedBox(height: HikaSpacing.xl),
                  HikaButton(
                    label: 'Reserve a seat',
                    icon: Icons.event_seat_outlined,
                    onPressed: () => context.push('/trips/${trip.id}/reserve', extra: trip),
                  ),
                ],
              ],
            ),
          );
        },
      ),
    );
  }
}

class _DriverCard extends StatelessWidget {
  const _DriverCard({required this.driver, required this.vehicle});

  final TripDriverSummary driver;
  final TripVehicleSummary vehicle;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return HikaCard(
      child: Row(
        children: [
          CircleAvatar(
            radius: 28,
            backgroundColor: HikaColors.accentLight,
            backgroundImage: driver.photoUrl == null ? null : NetworkImage(driver.photoUrl!),
            child: driver.photoUrl == null ? Text(driver.firstName.substring(0, 1), style: theme.textTheme.titleLarge) : null,
          ),
          const SizedBox(width: HikaSpacing.md),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Flexible(child: Text(driver.fullName, style: theme.textTheme.titleMedium, overflow: TextOverflow.ellipsis)),
                    if (driver.isVerifiedDriver) ...[
                      const SizedBox(width: HikaSpacing.xxs),
                      const Icon(Icons.verified_rounded, size: 16, color: HikaColors.accent),
                    ],
                  ],
                ),
                Text(
                  driver.averageRating == null
                      ? '${driver.completedTripCount} trips completed'
                      : '★ ${driver.averageRating!.toStringAsFixed(1)} · ${driver.completedTripCount} trips',
                  style: theme.textTheme.bodySmall,
                ),
                const SizedBox(height: HikaSpacing.xxs),
                Text('${vehicle.displayName} · ${vehicle.color}', style: theme.textTheme.bodySmall),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _RouteCard extends StatelessWidget {
  const _RouteCard({required this.trip});

  final Trip trip;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return HikaCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Route', style: theme.textTheme.titleMedium),
          const SizedBox(height: HikaSpacing.md),
          for (var i = 0; i < trip.stops.length; i++) _StopRow(stop: trip.stops[i], isLast: i == trip.stops.length - 1),
        ],
      ),
    );
  }
}

class _StopRow extends StatelessWidget {
  const _StopRow({required this.stop, required this.isLast});

  final TripStop stop;
  final bool isLast;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return IntrinsicHeight(
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Column(
            children: [
              Container(
                width: 10,
                height: 10,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: stop.sequence == 0 ? HikaColors.primary : HikaColors.accent,
                ),
              ),
              if (!isLast) Expanded(child: Container(width: 2, color: HikaColors.borderLight)),
            ],
          ),
          const SizedBox(width: HikaSpacing.md),
          Expanded(
            child: Padding(
              padding: EdgeInsets.only(bottom: isLast ? 0 : HikaSpacing.lg),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(stop.name, style: theme.textTheme.bodyLarge),
                  Text(Province.fromWireValue(stop.province).displayName, style: theme.textTheme.bodySmall),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _DetailsCard extends StatelessWidget {
  const _DetailsCard({required this.trip});

  final Trip trip;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return HikaCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _DetailRow(icon: Icons.payments_outlined, label: 'Price per seat', value: 'R${trip.pricePerSeat.toStringAsFixed(0)}'),
          const SizedBox(height: HikaSpacing.sm),
          _DetailRow(
            icon: Icons.event_seat_outlined,
            label: 'Seats',
            value: '${trip.minSeatsAvailable} of ${trip.totalSeatsOffered} available',
          ),
          if (trip.luggageAllowance != null) ...[
            const SizedBox(height: HikaSpacing.sm),
            _DetailRow(icon: Icons.luggage_outlined, label: 'Luggage', value: trip.luggageAllowance!),
          ],
          if (trip.notes != null) ...[
            const SizedBox(height: HikaSpacing.md),
            const Divider(),
            const SizedBox(height: HikaSpacing.sm),
            Text('Notes', style: theme.textTheme.labelMedium),
            const SizedBox(height: HikaSpacing.xxs),
            Text(trip.notes!, style: theme.textTheme.bodyMedium),
          ],
        ],
      ),
    );
  }
}

class _DetailRow extends StatelessWidget {
  const _DetailRow({required this.icon, required this.label, required this.value});

  final IconData icon;
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Row(
      children: [
        Icon(icon, size: 18, color: HikaColors.accent),
        const SizedBox(width: HikaSpacing.sm),
        Text(label, style: theme.textTheme.bodyMedium),
        const Spacer(),
        Text(value, style: theme.textTheme.bodyMedium?.copyWith(fontWeight: FontWeight.w600)),
      ],
    );
  }
}
