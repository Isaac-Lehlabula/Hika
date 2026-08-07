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
import '../../../../shared/widgets/hika_text_field.dart';
import '../../data/ride_alert.dart';
import '../providers/ride_alerts_controller.dart';

class MyRideAlertsScreen extends ConsumerWidget {
  const MyRideAlertsScreen({super.key});

  Future<void> _delete(BuildContext context, WidgetRef ref, RideAlert alert) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Remove this alert?'),
        content: Text('You\'ll no longer be notified about ${alert.label}.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Keep it')),
          TextButton(onPressed: () => Navigator.pop(context, true), child: const Text('Remove')),
        ],
      ),
    );
    if (confirmed == true) {
      await ref.read(rideAlertsControllerProvider.notifier).delete(alert.id);
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final alertsAsync = ref.watch(rideAlertsControllerProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Ride alerts')),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => CreateRideAlertSheet.show(context),
        icon: const Icon(Icons.add),
        label: const Text('New alert'),
      ),
      body: alertsAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => Center(
          child: HikaButton(
            label: 'Try again',
            variant: HikaButtonVariant.secondary,
            onPressed: () => ref.read(rideAlertsControllerProvider.notifier).refresh(),
          ),
        ),
        data: (alerts) {
          if (alerts.isEmpty) {
            return HikaEmptyState(
              icon: Icons.notifications_active_outlined,
              title: 'No alerts yet',
              message: 'We\'ll let you know the moment a driver posts a matching trip.',
              action: HikaButton(label: 'New alert', icon: Icons.add, onPressed: () => CreateRideAlertSheet.show(context)),
            );
          }

          return RefreshIndicator(
            onRefresh: () => ref.read(rideAlertsControllerProvider.notifier).refresh(),
            child: ListView.separated(
              padding: const EdgeInsets.fromLTRB(HikaSpacing.lg, HikaSpacing.lg, HikaSpacing.lg, HikaSpacing.huge),
              itemCount: alerts.length,
              separatorBuilder: (_, _) => const SizedBox(height: HikaSpacing.md),
              itemBuilder: (context, index) {
                final alert = alerts[index];
                return HikaCard(
                  child: Row(
                    children: [
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(alert.label, style: Theme.of(context).textTheme.titleMedium),
                            const SizedBox(height: HikaSpacing.xxs),
                            Text(
                              alert.travelDate == null ? 'Any date' : DateFormat.yMMMd().format(alert.travelDate!),
                              style: Theme.of(context).textTheme.bodySmall,
                            ),
                            const SizedBox(height: HikaSpacing.xs),
                            alert.status.toTripStatusBadge(),
                          ],
                        ),
                      ),
                      IconButton(
                        icon: const Icon(Icons.delete_outline, color: HikaColors.danger),
                        onPressed: () => _delete(context, ref, alert),
                      ),
                    ],
                  ),
                );
              },
            ),
          );
        },
      ),
    );
  }
}

/// Bottom sheet form for creating a ride alert — reused from both the standalone "My ride
/// alerts" screen and a no-results search (see SearchResultsScreen).
class CreateRideAlertSheet extends ConsumerStatefulWidget {
  const CreateRideAlertSheet({this.initialOrigin, this.initialDestination, this.initialTravelDate, super.key});

  final String? initialOrigin;
  final String? initialDestination;
  final DateTime? initialTravelDate;

  static Future<bool?> show(BuildContext context, {String? initialOrigin, String? initialDestination, DateTime? initialTravelDate}) {
    return showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      builder: (context) => CreateRideAlertSheet(
        initialOrigin: initialOrigin,
        initialDestination: initialDestination,
        initialTravelDate: initialTravelDate,
      ),
    );
  }

  @override
  ConsumerState<CreateRideAlertSheet> createState() => _CreateRideAlertSheetState();
}

class _CreateRideAlertSheetState extends ConsumerState<CreateRideAlertSheet> {
  late final _originController = TextEditingController(text: widget.initialOrigin);
  late final _destinationController = TextEditingController(text: widget.initialDestination);
  late DateTime? _travelDate = widget.initialTravelDate;
  bool _isSubmitting = false;
  String? _errorMessage;

  @override
  void dispose() {
    _originController.dispose();
    _destinationController.dispose();
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

    setState(() {
      _isSubmitting = true;
      _errorMessage = null;
    });

    try {
      await ref
          .read(rideAlertsControllerProvider.notifier)
          .create(origin: _originController.text.trim(), destination: _destinationController.text.trim(), travelDate: _travelDate);
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
          Text('Notify me about a route', style: theme.textTheme.titleLarge),
          const SizedBox(height: HikaSpacing.xs),
          Text(
            'We\'ll let you know the moment a driver posts a matching trip.',
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
              decoration: const InputDecoration(labelText: 'Date (optional)'),
              child: Text(_travelDate == null ? 'Any date' : DateFormat.yMMMd().format(_travelDate!)),
            ),
          ),
          if (_errorMessage != null) ...[
            const SizedBox(height: HikaSpacing.md),
            Text(_errorMessage!, style: TextStyle(color: theme.colorScheme.error)),
          ],
          const SizedBox(height: HikaSpacing.xl),
          HikaButton(label: 'Create alert', isLoading: _isSubmitting, onPressed: _submit),
        ],
      ),
    );
  }
}
