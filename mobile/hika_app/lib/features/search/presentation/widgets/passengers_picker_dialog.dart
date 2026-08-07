import 'package:flutter/material.dart';

import '../../../../core/theme/hika_colors.dart';
import '../../../../core/theme/hika_spacing.dart';
import '../../../../shared/widgets/hika_button.dart';

/// Simple +/- stepper dialog, capped to a minibus taxi's realistic seat range (matches the
/// backend's TotalSeatsOffered validation bound).
Future<int?> showPassengersPicker(BuildContext context, {required int initialValue}) {
  return showDialog<int>(
    context: context,
    builder: (context) => _PassengersPickerDialog(initialValue: initialValue),
  );
}

class _PassengersPickerDialog extends StatefulWidget {
  const _PassengersPickerDialog({required this.initialValue});

  final int initialValue;

  @override
  State<_PassengersPickerDialog> createState() => _PassengersPickerDialogState();
}

class _PassengersPickerDialogState extends State<_PassengersPickerDialog> {
  late int _value = widget.initialValue;

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text('Passengers'),
      content: Row(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          IconButton(
            icon: const Icon(Icons.remove_circle_outline),
            color: HikaColors.primary,
            onPressed: _value > 1 ? () => setState(() => _value--) : null,
          ),
          SizedBox(
            width: 56,
            child: Text('$_value', textAlign: TextAlign.center, style: Theme.of(context).textTheme.headlineSmall),
          ),
          IconButton(
            icon: const Icon(Icons.add_circle_outline),
            color: HikaColors.primary,
            onPressed: _value < 8 ? () => setState(() => _value++) : null,
          ),
        ],
      ),
      actions: [
        Padding(
          padding: const EdgeInsets.only(right: HikaSpacing.sm, bottom: HikaSpacing.xs),
          child: HikaButton(label: 'Done', onPressed: () => Navigator.pop(context, _value)),
        ),
      ],
    );
  }
}
