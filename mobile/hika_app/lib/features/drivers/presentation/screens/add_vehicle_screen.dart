import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/networking/api_exception.dart';
import '../../../../core/theme/hika_spacing.dart';
import '../../../../shared/widgets/hika_button.dart';
import '../../../../shared/widgets/hika_text_field.dart';
import '../providers/vehicles_controller.dart';

class AddVehicleScreen extends ConsumerStatefulWidget {
  const AddVehicleScreen({super.key});

  @override
  ConsumerState<AddVehicleScreen> createState() => _AddVehicleScreenState();
}

class _AddVehicleScreenState extends ConsumerState<AddVehicleScreen> {
  final _makeController = TextEditingController();
  final _modelController = TextEditingController();
  final _yearController = TextEditingController(text: DateTime.now().year.toString());
  final _colorController = TextEditingController();
  final _registrationController = TextEditingController();
  final _seatsController = TextEditingController(text: '4');

  bool _isSubmitting = false;
  String? _generalError;
  Map<String, List<String>>? _fieldErrors;

  @override
  void dispose() {
    _makeController.dispose();
    _modelController.dispose();
    _yearController.dispose();
    _colorController.dispose();
    _registrationController.dispose();
    _seatsController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    setState(() {
      _isSubmitting = true;
      _generalError = null;
      _fieldErrors = null;
    });

    try {
      final vehicle = await ref
          .read(vehiclesControllerProvider.notifier)
          .create(
            make: _makeController.text.trim(),
            model: _modelController.text.trim(),
            year: int.tryParse(_yearController.text.trim()) ?? DateTime.now().year,
            color: _colorController.text.trim(),
            registrationNumber: _registrationController.text.trim(),
            seatCapacity: int.tryParse(_seatsController.text.trim()) ?? 4,
          );
      if (mounted) {
        context.pushReplacement('/vehicles/${vehicle.id}');
      }
    } on ApiException catch (e) {
      setState(() {
        _fieldErrors = e.fieldErrors;
        if (!e.isValidation) {
          _generalError = e.message;
        }
      });
    } finally {
      if (mounted) {
        setState(() => _isSubmitting = false);
      }
    }
  }

  String? _errorFor(String field) => _fieldErrors?[field]?.firstOrNull;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Add a vehicle')),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(HikaSpacing.lg),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: HikaTextField(label: 'Make', controller: _makeController, errorText: _errorFor('make') ?? _errorFor('Make')),
                  ),
                  const SizedBox(width: HikaSpacing.sm),
                  Expanded(
                    child: HikaTextField(label: 'Model', controller: _modelController, errorText: _errorFor('model') ?? _errorFor('Model')),
                  ),
                ],
              ),
              const SizedBox(height: HikaSpacing.md),
              Row(
                children: [
                  Expanded(
                    child: HikaTextField(
                      label: 'Year',
                      controller: _yearController,
                      keyboardType: TextInputType.number,
                      errorText: _errorFor('year') ?? _errorFor('Year'),
                    ),
                  ),
                  const SizedBox(width: HikaSpacing.sm),
                  Expanded(
                    child: HikaTextField(label: 'Color', controller: _colorController, errorText: _errorFor('color') ?? _errorFor('Color')),
                  ),
                ],
              ),
              const SizedBox(height: HikaSpacing.md),
              HikaTextField(
                label: 'Registration number',
                controller: _registrationController,
                hintText: 'e.g. CA123456',
                errorText: _errorFor('registrationNumber') ?? _errorFor('RegistrationNumber'),
              ),
              const SizedBox(height: HikaSpacing.md),
              HikaTextField(
                label: 'Seats available for passengers',
                controller: _seatsController,
                keyboardType: TextInputType.number,
                errorText: _errorFor('seatCapacity') ?? _errorFor('SeatCapacity'),
              ),
              if (_generalError != null) ...[
                const SizedBox(height: HikaSpacing.md),
                Text(_generalError!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
              ],
              const SizedBox(height: HikaSpacing.xl),
              HikaButton(label: 'Save vehicle', isLoading: _isSubmitting, onPressed: _submit),
            ],
          ),
        ),
      ),
    );
  }
}
