import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../../core/networking/api_exception.dart';
import '../../../../core/providers.dart';
import '../../../../core/theme/hika_colors.dart';
import '../../../../core/theme/hika_spacing.dart';
import '../../../../shared/widgets/hika_button.dart';
import '../../../../shared/widgets/hika_card.dart';
import '../../../trips/data/trip.dart';
import '../../../trips/presentation/providers/my_trips_controller.dart';
import '../../data/ride_request.dart';
import '../providers/open_ride_requests_controller.dart';

/// A driver fulfils an open request by picking one of their own trips and which leg of it
/// covers the request — this is the accept-equivalent for the ride-request path (see
/// RideRequestService.ClaimAsync's remarks backend-side): it produces a real, confirmed booking
/// the same way accepting a normal search-and-request does.
class ClaimRideRequestScreen extends ConsumerStatefulWidget {
  const ClaimRideRequestScreen({required this.request, super.key});

  final RideRequest request;

  @override
  ConsumerState<ClaimRideRequestScreen> createState() => _ClaimRideRequestScreenState();
}

class _ClaimRideRequestScreenState extends ConsumerState<ClaimRideRequestScreen> {
  String? _selectedTripId;
  Trip? _selectedTrip;
  int? _boardingSequence;
  int? _alightingSequence;
  bool _isLoadingTrip = false;
  bool _isSubmitting = false;
  String? _errorMessage;

  Future<void> _selectTrip(String tripId) async {
    setState(() {
      _selectedTripId = tripId;
      _selectedTrip = null;
      _isLoadingTrip = true;
      _errorMessage = null;
    });

    try {
      final trip = await ref.read(tripsApiProvider).getTrip(tripId);
      setState(() {
        _selectedTrip = trip;
        _boardingSequence = 0;
        _alightingSequence = trip.stops.length - 1;
      });
    } on ApiException catch (e) {
      setState(() => _errorMessage = e.message);
    } finally {
      if (mounted) {
        setState(() => _isLoadingTrip = false);
      }
    }
  }

  Future<void> _submit() async {
    final tripId = _selectedTripId;
    final boarding = _boardingSequence;
    final alighting = _alightingSequence;
    if (tripId == null || boarding == null || alighting == null) {
      return;
    }

    setState(() {
      _isSubmitting = true;
      _errorMessage = null;
    });

    try {
      final booking = await ref
          .read(rideRequestsApiProvider)
          .claimRequest(requestId: widget.request.id, tripId: tripId, boardingStopSequence: boarding, alightingStopSequence: alighting);
      ref.invalidate(openRideRequestsControllerProvider);
      if (mounted) {
        context.pushReplacement('/bookings/${booking.id}');
      }
    } on ApiException catch (e) {
      setState(() => _errorMessage = e.message);
    } finally {
      if (mounted) {
        setState(() => _isSubmitting = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final tripsAsync = ref.watch(myTripsControllerProvider);
    final trip = _selectedTrip;

    return Scaffold(
      appBar: AppBar(title: const Text('Claim request')),
      body: ListView(
        padding: const EdgeInsets.all(HikaSpacing.lg),
        children: [
          HikaCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(widget.request.label, style: theme.textTheme.titleMedium),
                const SizedBox(height: HikaSpacing.xxs),
                Text(
                  '${DateFormat.yMMMd().format(widget.request.travelDate)} · ${widget.request.seatsNeeded} seat${widget.request.seatsNeeded == 1 ? '' : 's'}'
                  '${widget.request.proposedPricePerSeat != null ? ' · offering R${widget.request.proposedPricePerSeat!.toStringAsFixed(0)}/seat' : ''}',
                  style: theme.textTheme.bodySmall?.copyWith(color: HikaColors.textSecondaryLight),
                ),
              ],
            ),
          ),
          const SizedBox(height: HikaSpacing.lg),
          Text('Which of your trips covers this?', style: theme.textTheme.titleMedium),
          const SizedBox(height: HikaSpacing.sm),
          tripsAsync.when(
            loading: () => const Center(child: CircularProgressIndicator()),
            error: (error, _) => const Text('Couldn\'t load your trips.'),
            data: (trips) {
              final scheduled = trips.where((t) => t.status == 'Scheduled').toList();
              if (scheduled.isEmpty) {
                return Text(
                  'You don\'t have a scheduled trip yet — post one on the right date, then come back here to claim.',
                  style: theme.textTheme.bodyMedium?.copyWith(color: HikaColors.textSecondaryLight),
                );
              }

              return DropdownButtonFormField<String>(
                initialValue: _selectedTripId,
                decoration: const InputDecoration(labelText: 'Your trip'),
                items: [
                  for (final summary in scheduled)
                    DropdownMenuItem(
                      value: summary.id,
                      child: Text('${summary.originName} → ${summary.destinationName} · ${DateFormat.yMMMd().format(summary.departureAtUtc.toLocal())}'),
                    ),
                ],
                onChanged: (value) => value == null ? null : _selectTrip(value),
              );
            },
          ),
          if (_isLoadingTrip) ...[const SizedBox(height: HikaSpacing.lg), const Center(child: CircularProgressIndicator())],
          if (trip != null) ...[
            const SizedBox(height: HikaSpacing.lg),
            HikaCard(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('Which leg?', style: theme.textTheme.titleMedium),
                  const SizedBox(height: HikaSpacing.md),
                  DropdownButtonFormField<int>(
                    initialValue: _boardingSequence,
                    decoration: const InputDecoration(labelText: 'Board at'),
                    items: [
                      for (final stop in trip.stops.where((s) => s.sequence < trip.stops.length - 1))
                        DropdownMenuItem(value: stop.sequence, child: Text(stop.name)),
                    ],
                    onChanged: (value) => setState(() {
                      _boardingSequence = value;
                      if (_alightingSequence != null && _alightingSequence! <= (value ?? 0)) {
                        _alightingSequence = (value ?? 0) + 1;
                      }
                    }),
                  ),
                  const SizedBox(height: HikaSpacing.md),
                  DropdownButtonFormField<int>(
                    initialValue: _alightingSequence,
                    decoration: const InputDecoration(labelText: 'Alight at'),
                    items: [
                      for (final stop in trip.stops.where((s) => s.sequence > (_boardingSequence ?? -1)))
                        DropdownMenuItem(value: stop.sequence, child: Text(stop.name)),
                    ],
                    onChanged: (value) => setState(() => _alightingSequence = value),
                  ),
                ],
              ),
            ),
          ],
          if (_errorMessage != null) ...[
            const SizedBox(height: HikaSpacing.md),
            Text(_errorMessage!, style: TextStyle(color: theme.colorScheme.error)),
          ],
          const SizedBox(height: HikaSpacing.xl),
          Text(
            'Claiming books the rider in directly — no separate request/accept step, and chat opens right away.',
            style: theme.textTheme.bodySmall?.copyWith(color: HikaColors.textSecondaryLight),
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: HikaSpacing.md),
          HikaButton(
            label: 'Claim this request',
            isLoading: _isSubmitting,
            onPressed: trip == null ? null : _submit,
          ),
        ],
      ),
    );
  }
}
