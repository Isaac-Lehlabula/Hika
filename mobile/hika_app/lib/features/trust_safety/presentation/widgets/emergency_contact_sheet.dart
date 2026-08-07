import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/networking/api_exception.dart';
import '../../../../core/theme/hika_spacing.dart';
import '../../../../shared/widgets/hika_button.dart';
import '../../../../shared/widgets/hika_text_field.dart';
import '../../data/emergency_contact.dart';
import '../providers/emergency_contacts_controller.dart';

/// Bottom sheet for adding or editing an emergency contact. Pass [existing]
/// to edit; omit to create. Returns `true` if saved.
class EmergencyContactSheet extends ConsumerStatefulWidget {
  const EmergencyContactSheet({this.existing, super.key});

  final EmergencyContact? existing;

  static Future<bool?> show(BuildContext context, {EmergencyContact? existing}) {
    return showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      builder: (context) => EmergencyContactSheet(existing: existing),
    );
  }

  @override
  ConsumerState<EmergencyContactSheet> createState() => _EmergencyContactSheetState();
}

class _EmergencyContactSheetState extends ConsumerState<EmergencyContactSheet> {
  late final _nameController = TextEditingController(text: widget.existing?.name);
  late final _phoneController = TextEditingController(text: widget.existing?.phoneNumber);
  late final _relationshipController = TextEditingController(text: widget.existing?.relationship);
  bool _isSubmitting = false;
  String? _errorMessage;

  @override
  void dispose() {
    _nameController.dispose();
    _phoneController.dispose();
    _relationshipController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (_nameController.text.trim().isEmpty || _phoneController.text.trim().isEmpty) {
      setState(() => _errorMessage = 'Name and phone number are required.');
      return;
    }

    setState(() {
      _isSubmitting = true;
      _errorMessage = null;
    });

    try {
      final relationship = _relationshipController.text.trim();
      final existing = widget.existing;
      if (existing == null) {
        await ref
            .read(emergencyContactsControllerProvider.notifier)
            .create(
              name: _nameController.text.trim(),
              phoneNumber: _phoneController.text.trim(),
              relationship: relationship.isEmpty ? null : relationship,
            );
      } else {
        await ref
            .read(emergencyContactsControllerProvider.notifier)
            .updateContact(
              existing.id,
              name: _nameController.text.trim(),
              phoneNumber: _phoneController.text.trim(),
              relationship: relationship.isEmpty ? null : relationship,
            );
      }
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
          Text(widget.existing == null ? 'Add emergency contact' : 'Edit emergency contact', style: theme.textTheme.titleLarge),
          const SizedBox(height: HikaSpacing.xs),
          Text(
            "We'll only reach out to them if you need help during a trip.",
            style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurface.withValues(alpha: 0.6)),
          ),
          const SizedBox(height: HikaSpacing.lg),
          HikaTextField(label: 'Name', controller: _nameController),
          const SizedBox(height: HikaSpacing.md),
          HikaTextField(label: 'Phone number', controller: _phoneController, keyboardType: TextInputType.phone, hintText: '+27 82 123 4567'),
          const SizedBox(height: HikaSpacing.md),
          HikaTextField(label: 'Relationship (optional)', controller: _relationshipController, hintText: 'e.g. Spouse, Parent'),
          if (_errorMessage != null) ...[
            const SizedBox(height: HikaSpacing.sm),
            Text(_errorMessage!, style: TextStyle(color: theme.colorScheme.error)),
          ],
          const SizedBox(height: HikaSpacing.lg),
          HikaButton(label: 'Save', isLoading: _isSubmitting, onPressed: _submit),
        ],
      ),
    );
  }
}
