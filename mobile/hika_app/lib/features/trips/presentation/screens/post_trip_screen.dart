import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../../core/networking/api_exception.dart';
import '../../../../core/theme/hika_colors.dart';
import '../../../../core/theme/hika_spacing.dart';
import '../../../../shared/widgets/hika_button.dart';
import '../../../../shared/widgets/hika_card.dart';
import '../../../../shared/widgets/hika_empty_state.dart';
import '../../../../shared/widgets/hika_text_field.dart';
import '../../../drivers/data/vehicle.dart';
import '../../../drivers/presentation/providers/vehicles_controller.dart';
import '../../data/province.dart';
import '../../data/trip.dart';
import '../providers/my_trips_controller.dart';

const _stepTitles = ['Vehicle', 'Route', 'Details', 'Review'];

class _StopEntry {
  _StopEntry({String name = ''}) : nameController = TextEditingController(text: name);

  final TextEditingController nameController;
  Province province = Province.gauteng;
}

/// Guided 4-step "post a trip" flow: pick a vehicle, lay out the route stop by stop, set
/// seats/price/luggage/notes, then review before posting. Kept as one screen/PageView (not
/// separate routes per step) so back/forward is instant and in-progress input is never lost.
class PostTripScreen extends ConsumerStatefulWidget {
  const PostTripScreen({super.key});

  @override
  ConsumerState<PostTripScreen> createState() => _PostTripScreenState();
}

class _PostTripScreenState extends ConsumerState<PostTripScreen> {
  final _pageController = PageController();
  int _step = 0;

  String? _selectedVehicleId;
  final List<_StopEntry> _stops = [_StopEntry(), _StopEntry()];
  DateTime? _departureDate;
  TimeOfDay? _departureTime;
  final _seatsController = TextEditingController(text: '1');
  final _priceController = TextEditingController();
  final _luggageController = TextEditingController();
  final _notesController = TextEditingController();

  String? _stepError;
  bool _isSubmitting = false;

  @override
  void dispose() {
    _pageController.dispose();
    for (final stop in _stops) {
      stop.nameController.dispose();
    }
    _seatsController.dispose();
    _priceController.dispose();
    _luggageController.dispose();
    _notesController.dispose();
    super.dispose();
  }

  Vehicle? _selectedVehicle(List<Vehicle> vehicles) =>
      vehicles.where((v) => v.id == _selectedVehicleId).firstOrNull;

  DateTime? get _departureDateTime {
    if (_departureDate == null || _departureTime == null) {
      return null;
    }
    return DateTime(
      _departureDate!.year,
      _departureDate!.month,
      _departureDate!.day,
      _departureTime!.hour,
      _departureTime!.minute,
    );
  }

  void _addStop() => setState(() => _stops.insert(_stops.length - 1, _StopEntry()));

  void _removeStop(int index) => setState(() {
    _stops[index].nameController.dispose();
    _stops.removeAt(index);
  });

  String? _validateStep(List<Vehicle> vehicles) {
    switch (_step) {
      case 0:
        return _selectedVehicleId == null ? 'Select a vehicle to continue.' : null;
      case 1:
        return _stops.any((s) => s.nameController.text.trim().isEmpty)
            ? 'Every stop needs a name.'
            : null;
      case 2:
        final vehicle = _selectedVehicle(vehicles);
        final seats = int.tryParse(_seatsController.text.trim());
        final price = double.tryParse(_priceController.text.trim());
        if (_departureDateTime == null) {
          return 'Choose a departure date and time.';
        }
        if (_departureDateTime!.isBefore(DateTime.now())) {
          return 'Departure must be in the future.';
        }
        if (seats == null || seats < 1 || (vehicle != null && seats > vehicle.seatCapacity)) {
          return vehicle == null ? 'Enter a valid seat count.' : 'This vehicle seats at most ${vehicle.seatCapacity} passengers.';
        }
        if (price == null || price <= 0) {
          return 'Enter a price per seat.';
        }
        return null;
      default:
        return null;
    }
  }

  Future<void> _next(List<Vehicle> vehicles) async {
    final error = _validateStep(vehicles);
    if (error != null) {
      setState(() => _stepError = error);
      return;
    }
    setState(() => _stepError = null);

    if (_step == _stepTitles.length - 1) {
      await _submit();
      return;
    }

    setState(() => _step++);
    await _pageController.nextPage(duration: const Duration(milliseconds: 250), curve: Curves.easeOut);
  }

  Future<void> _back() async {
    if (_step == 0) {
      context.pop();
      return;
    }
    setState(() {
      _step--;
      _stepError = null;
    });
    await _pageController.previousPage(duration: const Duration(milliseconds: 250), curve: Curves.easeOut);
  }

  Future<void> _submit() async {
    setState(() {
      _isSubmitting = true;
      _stepError = null;
    });

    try {
      final trip = await ref
          .read(myTripsControllerProvider.notifier)
          .create(
            vehicleId: _selectedVehicleId!,
            departureAtUtc: _departureDateTime!,
            totalSeatsOffered: int.parse(_seatsController.text.trim()),
            pricePerSeat: double.parse(_priceController.text.trim()),
            luggageAllowance: _luggageController.text.trim().isEmpty ? null : _luggageController.text.trim(),
            notes: _notesController.text.trim().isEmpty ? null : _notesController.text.trim(),
            stops: [
              for (final stop in _stops) TripStopInput(rawName: stop.nameController.text.trim(), province: stop.province),
            ],
          );
      if (mounted) {
        context.pushReplacement('/trips/${trip.id}');
      }
    } on ApiException catch (e) {
      setState(() => _stepError = e.message);
    } finally {
      if (mounted) {
        setState(() => _isSubmitting = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final vehiclesAsync = ref.watch(vehiclesControllerProvider);
    final vehicles = vehiclesAsync.value ?? [];

    return Scaffold(
      appBar: AppBar(
        title: Text('Post a trip · ${_stepTitles[_step]}'),
        leading: IconButton(icon: const Icon(Icons.arrow_back), onPressed: _back),
      ),
      body: Column(
        children: [
          LinearProgressIndicator(value: (_step + 1) / _stepTitles.length, minHeight: 3),
          Expanded(
            child: PageView(
              controller: _pageController,
              physics: const NeverScrollableScrollPhysics(),
              children: [
                _VehicleStep(
                  vehiclesAsync: vehiclesAsync,
                  selectedVehicleId: _selectedVehicleId,
                  onSelect: (id) => setState(() => _selectedVehicleId = id),
                ),
                _RouteStep(stops: _stops, onAddStop: _addStop, onRemoveStop: _removeStop),
                _DetailsStep(
                  vehicle: _selectedVehicle(vehicles),
                  departureDate: _departureDate,
                  departureTime: _departureTime,
                  seatsController: _seatsController,
                  priceController: _priceController,
                  luggageController: _luggageController,
                  notesController: _notesController,
                  onPickDate: (date) => setState(() => _departureDate = date),
                  onPickTime: (time) => setState(() => _departureTime = time),
                ),
                _ReviewStep(
                  vehicle: _selectedVehicle(vehicles),
                  stops: _stops,
                  departureDateTime: _departureDateTime,
                  seatsText: _seatsController.text,
                  priceText: _priceController.text,
                  luggageText: _luggageController.text,
                  notesText: _notesController.text,
                ),
              ],
            ),
          ),
          SafeArea(
            top: false,
            child: Padding(
              padding: const EdgeInsets.fromLTRB(HikaSpacing.lg, HikaSpacing.sm, HikaSpacing.lg, HikaSpacing.lg),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  if (_stepError != null) ...[
                    Text(_stepError!, style: TextStyle(color: theme.colorScheme.error)),
                    const SizedBox(height: HikaSpacing.sm),
                  ],
                  HikaButton(
                    label: _step == _stepTitles.length - 1 ? 'Post trip' : 'Continue',
                    isLoading: _isSubmitting,
                    onPressed: vehicles.isEmpty && _step == 0 ? null : () => _next(vehicles),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _VehicleStep extends StatelessWidget {
  const _VehicleStep({required this.vehiclesAsync, required this.selectedVehicleId, required this.onSelect});

  final AsyncValue<List<Vehicle>> vehiclesAsync;
  final String? selectedVehicleId;
  final ValueChanged<String> onSelect;

  @override
  Widget build(BuildContext context) {
    return vehiclesAsync.when(
      loading: () => const Center(child: CircularProgressIndicator()),
      error: (error, _) => const Center(child: Text("Couldn't load your vehicles.")),
      data: (vehicles) {
        if (vehicles.isEmpty) {
          return HikaEmptyState(
            icon: Icons.directions_car_outlined,
            title: 'Add a vehicle first',
            message: 'You need at least one vehicle on your account before you can post a trip.',
            action: HikaButton(
              label: 'Add a vehicle',
              icon: Icons.add,
              onPressed: () => context.push('/vehicles/new'),
            ),
          );
        }

        return ListView.separated(
          padding: const EdgeInsets.all(HikaSpacing.lg),
          itemCount: vehicles.length,
          separatorBuilder: (_, _) => const SizedBox(height: HikaSpacing.md),
          itemBuilder: (context, index) {
            final vehicle = vehicles[index];
            final isSelected = vehicle.id == selectedVehicleId;
            final theme = Theme.of(context);

            return HikaCard(
              onTap: () => onSelect(vehicle.id),
              child: Row(
                children: [
                  Icon(
                    isSelected ? Icons.radio_button_checked : Icons.radio_button_unchecked,
                    color: isSelected ? HikaColors.primary : HikaColors.textSecondaryLight,
                  ),
                  const SizedBox(width: HikaSpacing.md),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(vehicle.displayName, style: theme.textTheme.titleMedium),
                        Text('${vehicle.color} · ${vehicle.seatCapacity} seats', style: theme.textTheme.bodySmall),
                      ],
                    ),
                  ),
                ],
              ),
            );
          },
        );
      },
    );
  }
}

class _RouteStep extends StatelessWidget {
  const _RouteStep({required this.stops, required this.onAddStop, required this.onRemoveStop});

  final List<_StopEntry> stops;
  final VoidCallback onAddStop;
  final ValueChanged<int> onRemoveStop;

  String _labelFor(int index) {
    if (index == 0) {
      return 'Origin';
    }
    if (index == stops.length - 1) {
      return 'Destination';
    }
    return 'Stop along the way';
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return ListView(
      padding: const EdgeInsets.all(HikaSpacing.lg),
      children: [
        Text('Where does this trip go?', style: theme.textTheme.titleLarge),
        const SizedBox(height: HikaSpacing.xs),
        Text(
          'Add every stop in order. Passengers will be able to book any leg of the route.',
          style: theme.textTheme.bodyMedium?.copyWith(color: HikaColors.textSecondaryLight),
        ),
        const SizedBox(height: HikaSpacing.lg),
        for (var i = 0; i < stops.length; i++) ...[
          _StopFields(
            label: _labelFor(i),
            entry: stops[i],
            onRemove: i == 0 || i == stops.length - 1 ? null : () => onRemoveStop(i),
          ),
          const SizedBox(height: HikaSpacing.md),
        ],
        HikaButton(
          label: 'Add a stop',
          variant: HikaButtonVariant.secondary,
          icon: Icons.add_location_alt_outlined,
          onPressed: onAddStop,
        ),
      ],
    );
  }
}

class _StopFields extends StatefulWidget {
  const _StopFields({required this.label, required this.entry, required this.onRemove});

  final String label;
  final _StopEntry entry;
  final VoidCallback? onRemove;

  @override
  State<_StopFields> createState() => _StopFieldsState();
}

class _StopFieldsState extends State<_StopFields> {
  @override
  Widget build(BuildContext context) {
    return HikaCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(widget.label, style: Theme.of(context).textTheme.labelLarge),
              if (widget.onRemove != null)
                IconButton(
                  icon: const Icon(Icons.close, size: 18),
                  onPressed: widget.onRemove,
                  visualDensity: VisualDensity.compact,
                ),
            ],
          ),
          const SizedBox(height: HikaSpacing.xs),
          HikaTextField(label: 'Town or suburb', controller: widget.entry.nameController, hintText: 'e.g. Polokwane'),
          const SizedBox(height: HikaSpacing.sm),
          DropdownButtonFormField<Province>(
            initialValue: widget.entry.province,
            decoration: const InputDecoration(labelText: 'Province'),
            items: [
              for (final province in Province.values)
                DropdownMenuItem(value: province, child: Text(province.displayName)),
            ],
            onChanged: (value) => setState(() => widget.entry.province = value ?? widget.entry.province),
          ),
        ],
      ),
    );
  }
}

class _DetailsStep extends StatelessWidget {
  const _DetailsStep({
    required this.vehicle,
    required this.departureDate,
    required this.departureTime,
    required this.seatsController,
    required this.priceController,
    required this.luggageController,
    required this.notesController,
    required this.onPickDate,
    required this.onPickTime,
  });

  final Vehicle? vehicle;
  final DateTime? departureDate;
  final TimeOfDay? departureTime;
  final TextEditingController seatsController;
  final TextEditingController priceController;
  final TextEditingController luggageController;
  final TextEditingController notesController;
  final ValueChanged<DateTime> onPickDate;
  final ValueChanged<TimeOfDay> onPickTime;

  Future<void> _pickDate(BuildContext context) async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: departureDate ?? now.add(const Duration(days: 1)),
      firstDate: now,
      lastDate: now.add(const Duration(days: 180)),
    );
    if (picked != null) {
      onPickDate(picked);
    }
  }

  Future<void> _pickTime(BuildContext context) async {
    final picked = await showTimePicker(context: context, initialTime: departureTime ?? const TimeOfDay(hour: 7, minute: 0));
    if (picked != null) {
      onPickTime(picked);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return ListView(
      padding: const EdgeInsets.all(HikaSpacing.lg),
      children: [
        Text('Trip details', style: theme.textTheme.titleLarge),
        if (vehicle != null) ...[
          const SizedBox(height: HikaSpacing.xs),
          Text('Driving the ${vehicle!.displayName} (${vehicle!.seatCapacity} seats)', style: theme.textTheme.bodyMedium),
        ],
        const SizedBox(height: HikaSpacing.lg),
        Row(
          children: [
            Expanded(
              child: InkWell(
                onTap: () => _pickDate(context),
                borderRadius: BorderRadius.circular(HikaRadius.md),
                child: InputDecorator(
                  decoration: const InputDecoration(labelText: 'Date'),
                  child: Text(departureDate == null ? 'Select' : DateFormat.yMMMd().format(departureDate!)),
                ),
              ),
            ),
            const SizedBox(width: HikaSpacing.sm),
            Expanded(
              child: InkWell(
                onTap: () => _pickTime(context),
                borderRadius: BorderRadius.circular(HikaRadius.md),
                child: InputDecorator(
                  decoration: const InputDecoration(labelText: 'Time'),
                  child: Text(departureTime == null ? 'Select' : departureTime!.format(context)),
                ),
              ),
            ),
          ],
        ),
        const SizedBox(height: HikaSpacing.md),
        Row(
          children: [
            Expanded(
              child: HikaTextField(
                label: 'Seats for passengers',
                controller: seatsController,
                keyboardType: TextInputType.number,
              ),
            ),
            const SizedBox(width: HikaSpacing.sm),
            Expanded(
              child: HikaTextField(
                label: 'Price per seat (ZAR)',
                controller: priceController,
                keyboardType: const TextInputType.numberWithOptions(decimal: true),
                prefixIcon: Icons.payments_outlined,
              ),
            ),
          ],
        ),
        const SizedBox(height: HikaSpacing.md),
        HikaTextField(
          label: 'Luggage allowance (optional)',
          controller: luggageController,
          hintText: 'e.g. One bag per passenger',
        ),
        const SizedBox(height: HikaSpacing.md),
        HikaTextField(
          label: 'Notes for passengers (optional)',
          controller: notesController,
          hintText: 'e.g. No smoking, pick-up at the Engen garage',
        ),
      ],
    );
  }
}

class _ReviewStep extends StatelessWidget {
  const _ReviewStep({
    required this.vehicle,
    required this.stops,
    required this.departureDateTime,
    required this.seatsText,
    required this.priceText,
    required this.luggageText,
    required this.notesText,
  });

  final Vehicle? vehicle;
  final List<_StopEntry> stops;
  final DateTime? departureDateTime;
  final String seatsText;
  final String priceText;
  final String luggageText;
  final String notesText;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return ListView(
      padding: const EdgeInsets.all(HikaSpacing.lg),
      children: [
        Text('Review your trip', style: theme.textTheme.titleLarge),
        const SizedBox(height: HikaSpacing.lg),
        HikaCard(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              for (var i = 0; i < stops.length; i++)
                Padding(
                  padding: EdgeInsets.only(bottom: i == stops.length - 1 ? 0 : HikaSpacing.xs),
                  child: Row(
                    children: [
                      Icon(
                        i == 0
                            ? Icons.trip_origin
                            : i == stops.length - 1
                            ? Icons.location_on
                            : Icons.circle,
                        size: i == 0 || i == stops.length - 1 ? 16 : 8,
                        color: HikaColors.accent,
                      ),
                      const SizedBox(width: HikaSpacing.sm),
                      Text(
                        stops[i].nameController.text.trim().isEmpty ? '—' : stops[i].nameController.text.trim(),
                        style: theme.textTheme.bodyLarge,
                      ),
                      const SizedBox(width: HikaSpacing.xxs),
                      Text('(${stops[i].province.displayName})', style: theme.textTheme.bodySmall),
                    ],
                  ),
                ),
            ],
          ),
        ),
        const SizedBox(height: HikaSpacing.md),
        HikaCard(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              if (vehicle != null) _ReviewRow(label: 'Vehicle', value: vehicle!.displayName),
              _ReviewRow(
                label: 'Departure',
                value: departureDateTime == null ? '—' : DateFormat('EEE d MMM, HH:mm').format(departureDateTime!),
              ),
              _ReviewRow(label: 'Seats offered', value: seatsText.isEmpty ? '—' : seatsText),
              _ReviewRow(label: 'Price per seat', value: priceText.isEmpty ? '—' : 'R$priceText'),
              if (luggageText.isNotEmpty) _ReviewRow(label: 'Luggage', value: luggageText),
              if (notesText.isNotEmpty) _ReviewRow(label: 'Notes', value: notesText),
            ],
          ),
        ),
      ],
    );
  }
}

class _ReviewRow extends StatelessWidget {
  const _ReviewRow({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: HikaSpacing.xxs),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(width: 120, child: Text(label, style: theme.textTheme.bodyMedium)),
          Expanded(child: Text(value, style: theme.textTheme.bodyMedium?.copyWith(fontWeight: FontWeight.w600))),
        ],
      ),
    );
  }
}
