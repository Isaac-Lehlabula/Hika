import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../../../core/networking/api_exception.dart';
import '../../../../core/theme/hika_colors.dart';
import '../../../../core/theme/hika_spacing.dart';
import '../../../../shared/widgets/hika_badge.dart';
import '../../../../shared/widgets/hika_button.dart';
import '../../../../shared/widgets/hika_card.dart';
import '../../../profile/presentation/providers/profile_controller.dart';
import '../../../reviews/presentation/widgets/submit_review_sheet.dart';
import '../../data/booking.dart';
import '../providers/booking_detail_controller.dart';
import '../providers/my_bookings_controller.dart';
import '../providers/payment_controller.dart';

class BookingDetailScreen extends ConsumerStatefulWidget {
  const BookingDetailScreen({required this.bookingId, super.key});

  final String bookingId;

  @override
  ConsumerState<BookingDetailScreen> createState() => _BookingDetailScreenState();
}

class _BookingDetailScreenState extends ConsumerState<BookingDetailScreen> with WidgetsBindingObserver {
  bool _isCancelling = false;
  bool _hasReviewed = false;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    // The passenger completes Ozow payment in an external browser, then returns to the app —
    // refresh here so the AwaitingPayment/Pending state clears without a manual pull-to-refresh.
    if (state == AppLifecycleState.resumed) {
      ref.read(bookingDetailControllerProvider(widget.bookingId).notifier).refresh();
      ref.read(paymentControllerProvider(widget.bookingId).notifier).refresh();
    }
  }

  Future<void> _leaveReview(Booking booking, bool isDriver) async {
    final revieweeLabel = isDriver ? booking.passenger.firstName : booking.trip.driver.firstName;
    final submitted = await SubmitReviewSheet.show(context, bookingId: widget.bookingId, revieweeLabel: revieweeLabel);
    if (submitted == true && mounted) {
      setState(() => _hasReviewed = true);
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Thanks for your review!')));
    }
  }

  Future<void> _cancel() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Cancel this booking?'),
        content: const Text('The seats will be released back to the trip. This cannot be undone.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Keep booking')),
          TextButton(onPressed: () => Navigator.pop(context, true), child: const Text('Cancel booking')),
        ],
      ),
    );
    if (confirmed != true) {
      return;
    }

    setState(() => _isCancelling = true);
    try {
      await ref.read(bookingDetailControllerProvider(widget.bookingId).notifier).cancel();
      ref.invalidate(myBookingsControllerProvider);
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
    final bookingAsync = ref.watch(bookingDetailControllerProvider(widget.bookingId));
    final currentUserId = ref.watch(profileControllerProvider).value?.userId;

    return Scaffold(
      appBar: AppBar(title: const Text('Booking')),
      body: bookingAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => Center(
          child: HikaButton(
            label: 'Try again',
            variant: HikaButtonVariant.secondary,
            onPressed: () => ref.read(bookingDetailControllerProvider(widget.bookingId).notifier).refresh(),
          ),
        ),
        data: (booking) {
          final isDriver = currentUserId != null && currentUserId == booking.trip.driver.userId;

          return RefreshIndicator(
            onRefresh: () => ref.read(bookingDetailControllerProvider(widget.bookingId).notifier).refresh(),
            child: ListView(
              padding: const EdgeInsets.all(HikaSpacing.lg),
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Expanded(
                      child: Text(
                        '${booking.trip.originName} → ${booking.trip.destinationName}',
                        style: Theme.of(context).textTheme.headlineSmall,
                      ),
                    ),
                    booking.status.toTripStatusBadge(),
                  ],
                ),
                const SizedBox(height: HikaSpacing.xs),
                Text(
                  DateFormat('EEEE d MMMM, HH:mm').format(booking.trip.departureAtUtc.toLocal()),
                  style: Theme.of(context).textTheme.bodyLarge?.copyWith(color: HikaColors.textSecondaryLight),
                ),
                if (booking.status == 'Pending') ...[
                  const SizedBox(height: HikaSpacing.sm),
                  Text(
                    'Waiting for the driver to approve your request.',
                    style: Theme.of(context).textTheme.bodyMedium?.copyWith(color: HikaColors.warning),
                  ),
                ],
                if (booking.status == 'AwaitingPayment') ...[
                  const SizedBox(height: HikaSpacing.sm),
                  Text(
                    'The driver accepted your request — complete payment to confirm your seats.',
                    style: Theme.of(context).textTheme.bodyMedium?.copyWith(color: HikaColors.warning),
                  ),
                ],
                const SizedBox(height: HikaSpacing.xl),
                _DriverCard(trip: booking.trip),
                const SizedBox(height: HikaSpacing.lg),
                _BookingDetailsCard(booking: booking),
                if (booking.status != 'Pending') ...[
                  const SizedBox(height: HikaSpacing.lg),
                  _PaymentCard(bookingId: widget.bookingId),
                  const SizedBox(height: HikaSpacing.lg),
                  HikaButton(
                    label: 'Chat with ${isDriver ? booking.passenger.firstName : booking.trip.driver.firstName}',
                    variant: HikaButtonVariant.secondary,
                    icon: Icons.chat_bubble_outline_rounded,
                    onPressed: () => context.push(
                      '/bookings/${widget.bookingId}/chat',
                      extra: isDriver ? booking.passenger.firstName : booking.trip.driver.firstName,
                    ),
                  ),
                ],
                if (booking.canCancel) ...[
                  const SizedBox(height: HikaSpacing.xl),
                  HikaButton(
                    label: 'Cancel booking',
                    variant: HikaButtonVariant.secondary,
                    icon: Icons.cancel_outlined,
                    isLoading: _isCancelling,
                    onPressed: _cancel,
                  ),
                ],
                if (booking.status == 'Completed' && !_hasReviewed) ...[
                  const SizedBox(height: HikaSpacing.xl),
                  HikaButton(
                    label: 'Leave a review',
                    icon: Icons.star_outline_rounded,
                    onPressed: () => _leaveReview(booking, isDriver),
                  ),
                ],
                const SizedBox(height: HikaSpacing.md),
                HikaButton(
                  label: 'View trip',
                  variant: HikaButtonVariant.text,
                  onPressed: () => context.push('/trips/${booking.trip.tripId}'),
                ),
              ],
            ),
          );
        },
      ),
    );
  }
}

class _DriverCard extends StatelessWidget {
  const _DriverCard({required this.trip});

  final BookingTripSummary trip;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return HikaCard(
      child: Row(
        children: [
          CircleAvatar(
            radius: 24,
            backgroundColor: HikaColors.accentLight,
            backgroundImage: trip.driver.photoUrl == null ? null : NetworkImage(trip.driver.photoUrl!),
            child: trip.driver.photoUrl == null
                ? Text(trip.driver.firstName.substring(0, 1), style: theme.textTheme.titleMedium)
                : null,
          ),
          const SizedBox(width: HikaSpacing.md),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Flexible(child: Text(trip.driver.fullName, style: theme.textTheme.titleSmall, overflow: TextOverflow.ellipsis)),
                    if (trip.driver.isVerifiedDriver) ...[
                      const SizedBox(width: HikaSpacing.xxs),
                      const Icon(Icons.verified_rounded, size: 14, color: HikaColors.accent),
                    ],
                  ],
                ),
                Text('${trip.vehicle.displayName} · ${trip.vehicle.color}', style: theme.textTheme.bodySmall),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _BookingDetailsCard extends StatelessWidget {
  const _BookingDetailsCard({required this.booking});

  final Booking booking;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return HikaCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _Row(icon: Icons.trip_origin, label: 'Board at', value: booking.boardingStopName),
          const SizedBox(height: HikaSpacing.sm),
          _Row(icon: Icons.location_on_outlined, label: 'Alight at', value: booking.alightingStopName),
          const SizedBox(height: HikaSpacing.sm),
          _Row(icon: Icons.event_seat_outlined, label: 'Seats', value: '${booking.seatsRequested}'),
          const SizedBox(height: HikaSpacing.sm),
          _Row(icon: Icons.payments_outlined, label: 'Total price', value: 'R${booking.totalPrice.toStringAsFixed(0)}'),
          if (booking.cancellationReason != null) ...[
            const SizedBox(height: HikaSpacing.md),
            const Divider(),
            const SizedBox(height: HikaSpacing.sm),
            Text('Cancellation reason', style: theme.textTheme.labelMedium),
            const SizedBox(height: HikaSpacing.xxs),
            Text(booking.cancellationReason!, style: theme.textTheme.bodyMedium),
          ],
        ],
      ),
    );
  }
}

class _PaymentCard extends ConsumerWidget {
  const _PaymentCard({required this.bookingId});

  final String bookingId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final paymentAsync = ref.watch(paymentControllerProvider(bookingId));
    final theme = Theme.of(context);

    return paymentAsync.when(
      loading: () => const HikaCard(
        child: Center(child: SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))),
      ),
      error: (_, _) => HikaCard(
        child: Row(
          children: [
            Text("Couldn't load payment details.", style: theme.textTheme.bodyMedium),
            const Spacer(),
            TextButton(
              onPressed: () => ref.read(paymentControllerProvider(bookingId).notifier).refresh(),
              child: const Text('Retry'),
            ),
          ],
        ),
      ),
      data: (payment) {
        if (payment == null) {
          return const SizedBox.shrink();
        }

        return HikaCard(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text('Payment', style: theme.textTheme.titleMedium),
                  _paymentStatusBadge(payment.status),
                ],
              ),
              const SizedBox(height: HikaSpacing.md),
              _Row(icon: Icons.payments_outlined, label: 'Fare', value: 'R${payment.amount.toStringAsFixed(2)}'),
              const SizedBox(height: HikaSpacing.sm),
              _Row(icon: Icons.percent, label: 'Platform fee', value: 'R${payment.platformFee.toStringAsFixed(2)}'),
              const SizedBox(height: HikaSpacing.sm),
              _Row(icon: Icons.account_balance_wallet_outlined, label: 'Driver payout', value: 'R${payment.driverPayoutAmount.toStringAsFixed(2)}'),
              if (payment.providerReference != null) ...[
                const SizedBox(height: HikaSpacing.sm),
                _Row(icon: Icons.receipt_long_outlined, label: 'Reference', value: payment.providerReference!),
              ],
              if (payment.status == 'Pending' && payment.redirectUrl != null) ...[
                const SizedBox(height: HikaSpacing.md),
                HikaButton(
                  label: 'Pay now',
                  icon: Icons.open_in_new_rounded,
                  onPressed: () => _openPaymentUrl(context, payment.redirectUrl!),
                ),
              ],
            ],
          ),
        );
      },
    );
  }

  Future<void> _openPaymentUrl(BuildContext context, String redirectUrl) async {
    final uri = Uri.parse(redirectUrl);
    final opened = await launchUrl(uri, mode: LaunchMode.externalApplication);
    if (!opened && context.mounted) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text("Couldn't open the payment page.")));
    }
  }

  Widget _paymentStatusBadge(String status) => switch (status) {
    'Succeeded' => const HikaBadge(label: 'Paid', tone: HikaBadgeTone.success, icon: Icons.check_circle_rounded),
    'Refunded' => const HikaBadge(label: 'Refunded', tone: HikaBadgeTone.neutral, icon: Icons.replay_rounded),
    'Failed' => const HikaBadge(label: 'Failed', tone: HikaBadgeTone.danger, icon: Icons.error_outline_rounded),
    _ => const HikaBadge(label: 'Pending', tone: HikaBadgeTone.warning, icon: Icons.hourglass_top_rounded),
  };
}

class _Row extends StatelessWidget {
  const _Row({required this.icon, required this.label, required this.value});

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
