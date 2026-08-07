import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../../core/networking/api_exception.dart';
import '../../../../core/theme/hika_colors.dart';
import '../../../../core/theme/hika_spacing.dart';
import '../../../../shared/widgets/hika_badge.dart';
import '../../../../shared/widgets/hika_button.dart';
import '../../../../shared/widgets/hika_card.dart';
import '../../../../shared/widgets/hika_empty_state.dart';
import '../../data/booking.dart';
import '../providers/trip_requests_controller.dart';

class TripRequestsScreen extends ConsumerStatefulWidget {
  const TripRequestsScreen({required this.tripId, super.key});

  final String tripId;

  @override
  ConsumerState<TripRequestsScreen> createState() => _TripRequestsScreenState();
}

class _TripRequestsScreenState extends ConsumerState<TripRequestsScreen> {
  String? _respondingToBookingId;

  Future<void> _respond(String bookingId, {required bool accept}) async {
    setState(() => _respondingToBookingId = bookingId);
    try {
      if (accept) {
        await ref.read(tripRequestsControllerProvider(widget.tripId).notifier).accept(bookingId);
      } else {
        await ref.read(tripRequestsControllerProvider(widget.tripId).notifier).decline(bookingId);
      }
    } on ApiException catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.message)));
      }
    } finally {
      if (mounted) {
        setState(() => _respondingToBookingId = null);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final requestsAsync = ref.watch(tripRequestsControllerProvider(widget.tripId));

    return Scaffold(
      appBar: AppBar(title: const Text('Booking requests')),
      body: requestsAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => Center(
          child: HikaButton(
            label: 'Try again',
            variant: HikaButtonVariant.secondary,
            onPressed: () => ref.read(tripRequestsControllerProvider(widget.tripId).notifier).refresh(),
          ),
        ),
        data: (bookings) {
          if (bookings.isEmpty) {
            return const HikaEmptyState(
              icon: Icons.event_seat_outlined,
              title: 'No requests yet',
              message: 'Booking requests from passengers will show up here.',
            );
          }

          return RefreshIndicator(
            onRefresh: () => ref.read(tripRequestsControllerProvider(widget.tripId).notifier).refresh(),
            child: ListView.separated(
              padding: const EdgeInsets.all(HikaSpacing.lg),
              itemCount: bookings.length,
              separatorBuilder: (_, _) => const SizedBox(height: HikaSpacing.md),
              itemBuilder: (context, index) {
                final booking = bookings[index];
                return _RequestCard(
                  booking: booking,
                  isResponding: _respondingToBookingId == booking.id,
                  onAccept: () => _respond(booking.id, accept: true),
                  onDecline: () => _respond(booking.id, accept: false),
                );
              },
            ),
          );
        },
      ),
    );
  }
}

class _RequestCard extends StatelessWidget {
  const _RequestCard({required this.booking, required this.isResponding, required this.onAccept, required this.onDecline});

  final Booking booking;
  final bool isResponding;
  final VoidCallback onAccept;
  final VoidCallback onDecline;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return HikaCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              CircleAvatar(
                radius: 20,
                backgroundColor: HikaColors.accentLight,
                backgroundImage: booking.passenger.photoUrl == null ? null : NetworkImage(booking.passenger.photoUrl!),
                child: booking.passenger.photoUrl == null
                    ? Text(booking.passenger.firstName.substring(0, 1), style: theme.textTheme.titleMedium)
                    : null,
              ),
              const SizedBox(width: HikaSpacing.sm),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(booking.passenger.fullName, style: theme.textTheme.titleSmall),
                    Text(
                      DateFormat('EEE d MMM').format(booking.requestedAtUtc.toLocal()),
                      style: theme.textTheme.bodySmall,
                    ),
                  ],
                ),
              ),
              booking.status.toTripStatusBadge(),
            ],
          ),
          const Divider(height: HikaSpacing.xl),
          Text('${booking.boardingStopName} → ${booking.alightingStopName}', style: theme.textTheme.bodyMedium),
          const SizedBox(height: HikaSpacing.xxs),
          Row(
            children: [
              Text('${booking.seatsRequested} seat${booking.seatsRequested == 1 ? '' : 's'}', style: theme.textTheme.bodySmall),
              const Spacer(),
              Text(
                'R${booking.totalPrice.toStringAsFixed(0)}',
                style: theme.textTheme.titleSmall?.copyWith(color: HikaColors.primary),
              ),
            ],
          ),
          if (booking.canRespond) ...[
            const SizedBox(height: HikaSpacing.md),
            Row(
              children: [
                Expanded(
                  child: HikaButton(
                    label: 'Decline',
                    variant: HikaButtonVariant.secondary,
                    isLoading: isResponding,
                    onPressed: onDecline,
                  ),
                ),
                const SizedBox(width: HikaSpacing.sm),
                Expanded(
                  child: HikaButton(label: 'Accept', isLoading: isResponding, onPressed: onAccept),
                ),
              ],
            ),
          ],
        ],
      ),
    );
  }
}
