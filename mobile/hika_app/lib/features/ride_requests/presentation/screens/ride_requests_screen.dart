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
import '../../../../shared/widgets/hika_empty_state.dart';
import '../../../../shared/widgets/hika_text_field.dart';
import '../../data/ride_request.dart';
import '../providers/my_ride_requests_controller.dart';
import '../providers/open_ride_requests_controller.dart';

/// "Ride requests" — a rider's own posted requests ("Mine"), and every other rider's open
/// request a driver could claim ("Open"). One screen, two tabs, since they're the same feature
/// viewed from each side rather than genuinely separate destinations.
class RideRequestsScreen extends StatefulWidget {
  const RideRequestsScreen({super.key});

  @override
  State<RideRequestsScreen> createState() => _RideRequestsScreenState();
}

class _RideRequestsScreenState extends State<RideRequestsScreen> with SingleTickerProviderStateMixin {
  late final _tabController = TabController(length: 2, vsync: this);

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Ride requests'),
        bottom: TabBar(controller: _tabController, tabs: const [Tab(text: 'Mine'), Tab(text: 'Open')]),
      ),
      floatingActionButton: AnimatedBuilder(
        animation: _tabController,
        builder: (context, _) => _tabController.index == 0
            ? FloatingActionButton.extended(
                onPressed: () => PostRideRequestSheet.show(context),
                icon: const Icon(Icons.add),
                label: const Text('New request'),
              )
            : const SizedBox.shrink(),
      ),
      body: TabBarView(controller: _tabController, children: const [_MyRequestsTab(), _OpenRequestsTab()]),
    );
  }
}

class _MyRequestsTab extends ConsumerWidget {
  const _MyRequestsTab();

  Future<void> _cancel(BuildContext context, WidgetRef ref, RideRequest request) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Cancel this request?'),
        content: Text('Drivers will no longer see your request for ${request.label}.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Keep it')),
          TextButton(onPressed: () => Navigator.pop(context, true), child: const Text('Cancel it')),
        ],
      ),
    );
    if (confirmed == true) {
      await ref.read(myRideRequestsControllerProvider.notifier).cancel(request.id);
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final requestsAsync = ref.watch(myRideRequestsControllerProvider);
    final theme = Theme.of(context);

    return requestsAsync.when(
      loading: () => const Center(child: CircularProgressIndicator()),
      error: (error, _) => Center(
        child: HikaButton(
          label: 'Try again',
          variant: HikaButtonVariant.secondary,
          onPressed: () => ref.read(myRideRequestsControllerProvider.notifier).refresh(),
        ),
      ),
      data: (requests) {
        if (requests.isEmpty) {
          return HikaEmptyState(
            icon: Icons.campaign_outlined,
            title: 'No requests yet',
            message: 'Post what you need and drivers posting a matching trip can claim it directly.',
            action: HikaButton(label: 'New request', icon: Icons.add, onPressed: () => PostRideRequestSheet.show(context)),
          );
        }

        return RefreshIndicator(
          onRefresh: () => ref.read(myRideRequestsControllerProvider.notifier).refresh(),
          child: ListView.separated(
            padding: const EdgeInsets.fromLTRB(HikaSpacing.lg, HikaSpacing.lg, HikaSpacing.lg, HikaSpacing.huge),
            itemCount: requests.length,
            separatorBuilder: (_, _) => const SizedBox(height: HikaSpacing.md),
            itemBuilder: (context, index) {
              final request = requests[index];
              return HikaCard(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Expanded(child: Text(request.label, style: theme.textTheme.titleMedium)),
                        (request.isExpired ? 'Expired' : request.status).toTripStatusBadge(),
                      ],
                    ),
                    const SizedBox(height: HikaSpacing.xxs),
                    Text(
                      '${DateFormat.yMMMd().format(request.travelDate)} · ${request.seatsNeeded} seat${request.seatsNeeded == 1 ? '' : 's'}'
                      '${request.proposedPricePerSeat != null ? ' · offering R${request.proposedPricePerSeat!.toStringAsFixed(0)}/seat' : ''}',
                      style: theme.textTheme.bodySmall?.copyWith(color: HikaColors.textSecondaryLight),
                    ),
                    if (request.isOpen) ...[
                      const SizedBox(height: HikaSpacing.sm),
                      Align(
                        alignment: Alignment.centerRight,
                        child: TextButton(onPressed: () => _cancel(context, ref, request), child: const Text('Cancel')),
                      ),
                    ],
                    if (request.status == 'Claimed' && request.claimedBookingId != null) ...[
                      const SizedBox(height: HikaSpacing.sm),
                      Align(
                        alignment: Alignment.centerRight,
                        child: TextButton(
                          onPressed: () => context.push('/bookings/${request.claimedBookingId}'),
                          child: const Text('View booking'),
                        ),
                      ),
                    ],
                  ],
                ),
              );
            },
          ),
        );
      },
    );
  }
}

class _OpenRequestsTab extends ConsumerWidget {
  const _OpenRequestsTab();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final requestsAsync = ref.watch(openRideRequestsControllerProvider);
    final theme = Theme.of(context);

    return requestsAsync.when(
      loading: () => const Center(child: CircularProgressIndicator()),
      error: (error, _) => Center(
        child: HikaButton(
          label: 'Try again',
          variant: HikaButtonVariant.secondary,
          onPressed: () => ref.read(openRideRequestsControllerProvider.notifier).refresh(),
        ),
      ),
      data: (requests) {
        if (requests.isEmpty) {
          return const HikaEmptyState(
            icon: Icons.explore_outlined,
            title: 'Nothing open right now',
            message: 'When a rider posts a request with no matching trip yet, it shows up here for any driver to claim.',
          );
        }

        return RefreshIndicator(
          onRefresh: () => ref.read(openRideRequestsControllerProvider.notifier).refresh(),
          child: ListView.separated(
            padding: const EdgeInsets.all(HikaSpacing.lg),
            itemCount: requests.length,
            separatorBuilder: (_, _) => const SizedBox(height: HikaSpacing.md),
            itemBuilder: (context, index) {
              final request = requests[index];
              return HikaCard(
                onTap: () => context.push('/ride-requests/${request.id}/claim', extra: request),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(request.label, style: theme.textTheme.titleMedium),
                    const SizedBox(height: HikaSpacing.xxs),
                    Text(
                      '${DateFormat.yMMMd().format(request.travelDate)} · ${request.seatsNeeded} seat${request.seatsNeeded == 1 ? '' : 's'}'
                      '${request.proposedPricePerSeat != null ? ' · offering R${request.proposedPricePerSeat!.toStringAsFixed(0)}/seat' : ''}',
                      style: theme.textTheme.bodySmall?.copyWith(color: HikaColors.textSecondaryLight),
                    ),
                    const SizedBox(height: HikaSpacing.sm),
                    Align(
                      alignment: Alignment.centerRight,
                      child: HikaButton(
                        label: 'Claim',
                        variant: HikaButtonVariant.secondary,
                        onPressed: () => context.push('/ride-requests/${request.id}/claim', extra: request),
                      ),
                    ),
                  ],
                ),
              );
            },
          ),
        );
      },
    );
  }
}

/// Bottom sheet form for posting a ride request — reused from both the "Ride requests" screen
/// and a no-results search (see SearchResultsScreen), same pattern as CreateRideAlertSheet.
/// Unlike a ride alert, the travel date is required (it's what drives auto-expiry) and there's
/// no "any date" option.
class PostRideRequestSheet extends ConsumerStatefulWidget {
  const PostRideRequestSheet({this.initialOrigin, this.initialDestination, this.initialTravelDate, super.key});

  final String? initialOrigin;
  final String? initialDestination;
  final DateTime? initialTravelDate;

  static Future<bool?> show(BuildContext context, {String? initialOrigin, String? initialDestination, DateTime? initialTravelDate}) {
    return showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      builder: (context) => PostRideRequestSheet(
        initialOrigin: initialOrigin,
        initialDestination: initialDestination,
        initialTravelDate: initialTravelDate,
      ),
    );
  }

  @override
  ConsumerState<PostRideRequestSheet> createState() => _PostRideRequestSheetState();
}

class _PostRideRequestSheetState extends ConsumerState<PostRideRequestSheet> {
  late final _originController = TextEditingController(text: widget.initialOrigin);
  late final _destinationController = TextEditingController(text: widget.initialDestination);
  final _priceController = TextEditingController();
  late DateTime? _travelDate = widget.initialTravelDate;
  int _seats = 1;
  bool _isSubmitting = false;
  String? _errorMessage;

  @override
  void dispose() {
    _originController.dispose();
    _destinationController.dispose();
    _priceController.dispose();
    super.dispose();
  }

  Future<void> _pickDate() async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: _travelDate ?? now,
      firstDate: now,
      lastDate: now.add(const Duration(days: 180)),
    );
    if (picked != null) {
      setState(() => _travelDate = picked);
    }
  }

  Future<void> _submit() async {
    if (_originController.text.trim().isEmpty || _destinationController.text.trim().isEmpty) {
      setState(() => _errorMessage = 'Enter where you\'re leaving from and going to.');
      return;
    }
    if (_travelDate == null) {
      setState(() => _errorMessage = 'Pick a travel date.');
      return;
    }

    setState(() {
      _isSubmitting = true;
      _errorMessage = null;
    });

    try {
      await ref
          .read(myRideRequestsControllerProvider.notifier)
          .create(
            origin: _originController.text.trim(),
            destination: _destinationController.text.trim(),
            travelDate: _travelDate!,
            seatsNeeded: _seats,
            proposedPricePerSeat: double.tryParse(_priceController.text.trim()),
          );
      if (mounted) {
        Navigator.pop(context, true);
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

    return Padding(
      padding: EdgeInsets.only(
        left: HikaSpacing.lg,
        right: HikaSpacing.lg,
        top: HikaSpacing.lg,
        bottom: MediaQuery.of(context).viewInsets.bottom + HikaSpacing.lg,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Post a ride request', style: theme.textTheme.titleLarge),
          const SizedBox(height: HikaSpacing.xs),
          Text(
            'Visible to every driver — one who posts a matching trip can claim it and book you in directly.',
            style: theme.textTheme.bodyMedium?.copyWith(color: HikaColors.textSecondaryLight),
          ),
          const SizedBox(height: HikaSpacing.lg),
          HikaTextField(label: 'From', controller: _originController, prefixIcon: Icons.trip_origin),
          const SizedBox(height: HikaSpacing.md),
          HikaTextField(label: 'To', controller: _destinationController, prefixIcon: Icons.location_on_outlined),
          const SizedBox(height: HikaSpacing.md),
          InkWell(
            onTap: _pickDate,
            borderRadius: BorderRadius.circular(HikaRadius.md),
            child: InputDecorator(
              decoration: const InputDecoration(labelText: 'Travel date'),
              child: Text(_travelDate == null ? 'Pick a date' : DateFormat.yMMMd().format(_travelDate!)),
            ),
          ),
          const SizedBox(height: HikaSpacing.md),
          Row(
            children: [
              Expanded(child: Text('Seats needed', style: theme.textTheme.titleSmall)),
              IconButton(
                icon: const Icon(Icons.remove_circle_outline),
                color: HikaColors.primary,
                onPressed: _seats > 1 ? () => setState(() => _seats--) : null,
              ),
              Text('$_seats', style: theme.textTheme.titleMedium),
              IconButton(
                icon: const Icon(Icons.add_circle_outline),
                color: HikaColors.primary,
                onPressed: _seats < 8 ? () => setState(() => _seats++) : null,
              ),
            ],
          ),
          const SizedBox(height: HikaSpacing.md),
          HikaTextField(
            label: 'Your price per seat (optional)',
            controller: _priceController,
            keyboardType: const TextInputType.numberWithOptions(decimal: true),
            prefixIcon: Icons.payments_outlined,
            hintText: 'What you\'re hoping to pay',
          ),
          if (_errorMessage != null) ...[
            const SizedBox(height: HikaSpacing.md),
            Text(_errorMessage!, style: TextStyle(color: theme.colorScheme.error)),
          ],
          const SizedBox(height: HikaSpacing.xl),
          HikaButton(label: 'Post request', isLoading: _isSubmitting, onPressed: _submit),
        ],
      ),
    );
  }
}
