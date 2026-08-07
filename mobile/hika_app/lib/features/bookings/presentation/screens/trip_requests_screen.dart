import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../../core/networking/api_exception.dart';
import '../../../../core/providers.dart';
import '../../../../core/theme/hika_colors.dart';
import '../../../../core/theme/hika_spacing.dart';
import '../../../../shared/widgets/hika_badge.dart';
import '../../../../shared/widgets/hika_button.dart';
import '../../../../shared/widgets/hika_card.dart';
import '../../../../shared/widgets/hika_empty_state.dart';
import '../../data/booking.dart';
import '../providers/payment_controller.dart';
import '../providers/trip_requests_controller.dart';

class TripRequestsScreen extends ConsumerStatefulWidget {
  const TripRequestsScreen({required this.tripId, super.key});

  final String tripId;

  @override
  ConsumerState<TripRequestsScreen> createState() => _TripRequestsScreenState();
}

class _TripRequestsScreenState extends ConsumerState<TripRequestsScreen> {
  String? _respondingToBookingId;
  String? _refundingBookingId;

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

  Future<void> _refund(String bookingId) async {
    final reasonController = TextEditingController();
    final reason = await showDialog<String>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Refund this booking?'),
        content: TextField(
          controller: reasonController,
          decoration: const InputDecoration(labelText: 'Reason', hintText: 'e.g. Trip no longer running'),
          autofocus: true,
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context), child: const Text('Cancel')),
          TextButton(
            onPressed: () => Navigator.pop(context, reasonController.text.trim()),
            child: const Text('Refund'),
          ),
        ],
      ),
    );
    if (reason == null || reason.isEmpty) {
      return;
    }

    setState(() => _refundingBookingId = bookingId);
    try {
      await ref.read(paymentsApiProvider).refundPayment(bookingId, reason: reason);
      ref.invalidate(paymentControllerProvider(bookingId));
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Booking refunded.')));
      }
    } on ApiException catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.message)));
      }
    } finally {
      if (mounted) {
        setState(() => _refundingBookingId = null);
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
                  isRefunding: _refundingBookingId == booking.id,
                  onAccept: () => _respond(booking.id, accept: true),
                  onDecline: () => _respond(booking.id, accept: false),
                  onRefund: () => _refund(booking.id),
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
  const _RequestCard({
    required this.booking,
    required this.isResponding,
    required this.isRefunding,
    required this.onAccept,
    required this.onDecline,
    required this.onRefund,
  });

  final Booking booking;
  final bool isResponding;
  final bool isRefunding;
  final VoidCallback onAccept;
  final VoidCallback onDecline;
  final VoidCallback onRefund;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return HikaCard(
      onTap: () => context.push('/bookings/${booking.id}'),
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
          if (booking.status == 'Confirmed') ...[
            const SizedBox(height: HikaSpacing.md),
            HikaButton(
              label: 'Refund',
              variant: HikaButtonVariant.secondary,
              icon: Icons.replay_rounded,
              isLoading: isRefunding,
              onPressed: onRefund,
            ),
          ],
        ],
      ),
    );
  }
}
