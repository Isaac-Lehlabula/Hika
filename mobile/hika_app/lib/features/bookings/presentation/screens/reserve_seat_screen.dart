import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/networking/api_exception.dart';
import '../../../../core/theme/hika_colors.dart';
import '../../../../core/theme/hika_spacing.dart';
import '../../../../shared/widgets/hika_button.dart';
import '../../../../shared/widgets/hika_card.dart';
import '../../../trips/data/trip.dart';
import '../providers/my_bookings_controller.dart';

/// Reserve a seat on a contiguous sub-range of a trip's stops — mirrors the backend's
/// segment-based booking model (see docs/domain-model.md §4): a rider can book any
/// boarding->alighting leg of a longer trip, not just the full origin->destination.
class ReserveSeatScreen extends ConsumerStatefulWidget {
  const ReserveSeatScreen({required this.trip, super.key});

  final Trip trip;

  @override
  ConsumerState<ReserveSeatScreen> createState() => _ReserveSeatScreenState();
}

class _ReserveSeatScreenState extends ConsumerState<ReserveSeatScreen> {
  late int _boardingSequence = 0;
  late int _alightingSequence = widget.trip.stops.length - 1;
  int _seats = 1;
  bool _isSubmitting = false;
  String? _errorMessage;

  int get _seatsAvailable {
    final segments = widget.trip.segments.where(
      (s) => s.fromSequence >= _boardingSequence && s.toSequence <= _alightingSequence,
    );
    if (segments.isEmpty) {
      return 0;
    }
    return segments.map((s) => s.seatsAvailable).reduce((a, b) => a < b ? a : b);
  }

  void _onBoardingChanged(int sequence) {
    setState(() {
      _boardingSequence = sequence;
      if (_alightingSequence <= _boardingSequence) {
        _alightingSequence = _boardingSequence + 1;
      }
      _seats = _seats.clamp(1, _seatsAvailable == 0 ? 1 : _seatsAvailable);
    });
  }

  void _onAlightingChanged(int sequence) {
    setState(() {
      _alightingSequence = sequence;
      _seats = _seats.clamp(1, _seatsAvailable == 0 ? 1 : _seatsAvailable);
    });
  }

  Future<void> _submit() async {
    setState(() {
      _isSubmitting = true;
      _errorMessage = null;
    });

    try {
      final booking = await ref
          .read(myBookingsControllerProvider.notifier)
          .request(
            tripId: widget.trip.id,
            boardingStopSequence: _boardingSequence,
            alightingStopSequence: _alightingSequence,
            seatsRequested: _seats,
          );
      if (mounted) {
        context.pushReplacement('/bookings/${booking.id}');
      }
    } on ApiException catch (e) {
      setState(() {
        _errorMessage = e.statusCode == 409
            ? 'Not enough seats left for that leg of the trip. Try fewer seats or a shorter range.'
            : e.message;
      });
    } finally {
      if (mounted) {
        setState(() => _isSubmitting = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final trip = widget.trip;
    final seatsAvailable = _seatsAvailable;
    final totalPrice = trip.pricePerSeat * _seats;

    return Scaffold(
      appBar: AppBar(title: const Text('Reserve a seat')),
      body: ListView(
        padding: const EdgeInsets.all(HikaSpacing.lg),
        children: [
          HikaCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Where do you want to ride?', style: theme.textTheme.titleMedium),
                const SizedBox(height: HikaSpacing.md),
                DropdownButtonFormField<int>(
                  initialValue: _boardingSequence,
                  decoration: const InputDecoration(labelText: 'Board at'),
                  items: [
                    for (final stop in trip.stops.where((s) => s.sequence < trip.stops.length - 1))
                      DropdownMenuItem(value: stop.sequence, child: Text(stop.name)),
                  ],
                  onChanged: (value) => value == null ? null : _onBoardingChanged(value),
                ),
                const SizedBox(height: HikaSpacing.md),
                DropdownButtonFormField<int>(
                  initialValue: _alightingSequence,
                  decoration: const InputDecoration(labelText: 'Alight at'),
                  items: [
                    for (final stop in trip.stops.where((s) => s.sequence > _boardingSequence))
                      DropdownMenuItem(value: stop.sequence, child: Text(stop.name)),
                  ],
                  onChanged: (value) => value == null ? null : _onAlightingChanged(value),
                ),
              ],
            ),
          ),
          const SizedBox(height: HikaSpacing.lg),
          HikaCard(
            child: Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text('Seats', style: theme.textTheme.titleMedium),
                      Text(
                        seatsAvailable == 0 ? 'No seats left on this leg' : '$seatsAvailable available',
                        style: theme.textTheme.bodySmall?.copyWith(
                          color: seatsAvailable == 0 ? HikaColors.danger : HikaColors.textSecondaryLight,
                        ),
                      ),
                    ],
                  ),
                ),
                IconButton(
                  icon: const Icon(Icons.remove_circle_outline),
                  color: HikaColors.primary,
                  onPressed: _seats > 1 ? () => setState(() => _seats--) : null,
                ),
                Text('$_seats', style: theme.textTheme.headlineSmall),
                IconButton(
                  icon: const Icon(Icons.add_circle_outline),
                  color: HikaColors.primary,
                  onPressed: _seats < seatsAvailable ? () => setState(() => _seats++) : null,
                ),
              ],
            ),
          ),
          const SizedBox(height: HikaSpacing.lg),
          HikaCard(
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text('Total', style: theme.textTheme.titleMedium),
                Text(
                  'R${totalPrice.toStringAsFixed(0)}',
                  style: theme.textTheme.headlineSmall?.copyWith(color: HikaColors.primary),
                ),
              ],
            ),
          ),
          if (_errorMessage != null) ...[
            const SizedBox(height: HikaSpacing.md),
            Text(_errorMessage!, style: TextStyle(color: theme.colorScheme.error)),
          ],
          const SizedBox(height: HikaSpacing.xl),
          Text(
            'The driver needs to approve your request before it\'s confirmed.',
            style: theme.textTheme.bodySmall?.copyWith(color: HikaColors.textSecondaryLight),
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: HikaSpacing.md),
          HikaButton(
            label: 'Request booking',
            isLoading: _isSubmitting,
            onPressed: seatsAvailable == 0 ? null : _submit,
          ),
        ],
      ),
    );
  }
}
