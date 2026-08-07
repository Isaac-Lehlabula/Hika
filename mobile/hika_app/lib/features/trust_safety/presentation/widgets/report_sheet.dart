import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/networking/api_exception.dart';
import '../../../../core/providers.dart';
import '../../../../core/theme/hika_spacing.dart';
import '../../../../shared/widgets/hika_button.dart';
import '../../../../shared/widgets/hika_text_field.dart';
import '../../data/report.dart';

/// Bottom sheet for reporting a user or a trip — exactly one of [reportedUserId]/
/// [reportedTripId] is set by the caller. Returns `true` if a report was filed.
class ReportSheet extends ConsumerStatefulWidget {
  const ReportSheet({required this.title, this.reportedUserId, this.reportedTripId, super.key});

  final String title;
  final String? reportedUserId;
  final String? reportedTripId;

  static Future<bool?> show(BuildContext context, {required String title, String? reportedUserId, String? reportedTripId}) {
    assert(reportedUserId != null || reportedTripId != null, 'Must report either a user or a trip');
    return showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      builder: (context) => ReportSheet(title: title, reportedUserId: reportedUserId, reportedTripId: reportedTripId),
    );
  }

  @override
  ConsumerState<ReportSheet> createState() => _ReportSheetState();
}

class _ReportSheetState extends ConsumerState<ReportSheet> {
  ReportReason _reason = ReportReason.harassment;
  final _descriptionController = TextEditingController();
  bool _isSubmitting = false;
  String? _errorMessage;

  @override
  void dispose() {
    _descriptionController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (_descriptionController.text.trim().isEmpty) {
      setState(() => _errorMessage = 'Tell us what happened.');
      return;
    }

    setState(() {
      _isSubmitting = true;
      _errorMessage = null;
    });

    try {
      await ref
          .read(trustSafetyApiProvider)
          .fileReport(
            reportedUserId: widget.reportedUserId,
            reportedTripId: widget.reportedTripId,
            reason: _reason,
            description: _descriptionController.text.trim(),
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
          Text(widget.title, style: theme.textTheme.titleLarge),
          const SizedBox(height: HikaSpacing.xs),
          Text(
            'Our team reviews every report. Thanks for helping keep Hika safe.',
            style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurface.withValues(alpha: 0.6)),
          ),
          const SizedBox(height: HikaSpacing.lg),
          DropdownButtonFormField<ReportReason>(
            initialValue: _reason,
            decoration: const InputDecoration(labelText: 'Reason'),
            items: [for (final reason in ReportReason.values) DropdownMenuItem(value: reason, child: Text(reason.displayName))],
            onChanged: (value) => setState(() => _reason = value ?? _reason),
          ),
          const SizedBox(height: HikaSpacing.md),
          HikaTextField(
            label: 'What happened?',
            controller: _descriptionController,
            hintText: 'Give us as much detail as you can.',
          ),
          if (_errorMessage != null) ...[
            const SizedBox(height: HikaSpacing.sm),
            Text(_errorMessage!, style: TextStyle(color: theme.colorScheme.error)),
          ],
          const SizedBox(height: HikaSpacing.lg),
          HikaButton(label: 'Submit report', isLoading: _isSubmitting, onPressed: _submit),
        ],
      ),
    );
  }
}
