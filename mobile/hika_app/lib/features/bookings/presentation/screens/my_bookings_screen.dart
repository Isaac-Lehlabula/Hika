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
import '../../data/booking.dart';
import '../providers/my_bookings_controller.dart';

/// The Bookings tab: trips the signed-in user has requested/booked as a passenger.
class MyBookingsScreen extends ConsumerWidget {
  const MyBookingsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final bookingsAsync = ref.watch(myBookingsControllerProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Your bookings')),
      body: bookingsAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => Center(
          child: HikaButton(
            label: 'Try again',
            variant: HikaButtonVariant.secondary,
            onPressed: () => ref.read(myBookingsControllerProvider.notifier).refresh(),
          ),
        ),
        data: (bookings) {
          if (bookings.isEmpty) {
            return const HikaEmptyState(
              icon: Icons.event_seat_outlined,
              title: 'No bookings yet',
              message: 'Find a hike from the Home tab to reserve your first seat.',
            );
          }

          return RefreshIndicator(
            onRefresh: () => ref.read(myBookingsControllerProvider.notifier).refresh(),
            child: ListView.separated(
              padding: const EdgeInsets.all(HikaSpacing.lg),
              itemCount: bookings.length,
              separatorBuilder: (_, _) => const SizedBox(height: HikaSpacing.md),
              itemBuilder: (context, index) => _BookingCard(booking: bookings[index]),
            ),
          );
        },
      ),
    );
  }
}

class _BookingCard extends StatelessWidget {
  const _BookingCard({required this.booking});

  final Booking booking;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return HikaCard(
      onTap: () => context.push('/bookings/${booking.id}'),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Expanded(
                child: Text(
                  '${booking.boardingStopName} → ${booking.alightingStopName}',
                  style: theme.textTheme.titleMedium,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
              booking.status.toTripStatusBadge(),
            ],
          ),
          const SizedBox(height: HikaSpacing.xxs),
          Text(DateFormat('EEE d MMM, HH:mm').format(booking.trip.departureAtUtc.toLocal()), style: theme.textTheme.bodySmall),
          const SizedBox(height: HikaSpacing.sm),
          Row(
            children: [
              Icon(Icons.event_seat_outlined, size: 16, color: HikaColors.accent),
              const SizedBox(width: HikaSpacing.xxs),
              Text('${booking.seatsRequested} seat${booking.seatsRequested == 1 ? '' : 's'}', style: theme.textTheme.bodySmall),
              const Spacer(),
              Text(
                'R${booking.totalPrice.toStringAsFixed(0)}',
                style: theme.textTheme.titleSmall?.copyWith(color: HikaColors.primary),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
