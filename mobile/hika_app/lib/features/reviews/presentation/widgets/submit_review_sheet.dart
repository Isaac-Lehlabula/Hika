import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/networking/api_exception.dart';
import '../../../../core/providers.dart';
import '../../../../core/theme/hika_colors.dart';
import '../../../../core/theme/hika_spacing.dart';
import '../../../../shared/widgets/hika_button.dart';
import '../../../../shared/widgets/hika_text_field.dart';

/// Bottom sheet for leaving a review on a completed booking. Returns `true` if a review was
/// submitted, `null`/`false` if the sheet was dismissed without submitting.
class SubmitReviewSheet extends ConsumerStatefulWidget {
  const SubmitReviewSheet({required this.bookingId, required this.revieweeLabel, super.key});

  final String bookingId;

  /// e.g. "your driver" or "your passenger" — shown in the sheet's title.
  final String revieweeLabel;

  static Future<bool?> show(BuildContext context, {required String bookingId, required String revieweeLabel}) {
    return showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      builder: (context) => SubmitReviewSheet(bookingId: bookingId, revieweeLabel: revieweeLabel),
    );
  }

  @override
  ConsumerState<SubmitReviewSheet> createState() => _SubmitReviewSheetState();
}

class _SubmitReviewSheetState extends ConsumerState<SubmitReviewSheet> {
  int _rating = 0;
  final _commentController = TextEditingController();
  bool _isSubmitting = false;
  String? _errorMessage;

  @override
  void dispose() {
    _commentController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (_rating == 0) {
      setState(() => _errorMessage = 'Tap a star to rate.');
      return;
    }

    setState(() {
      _isSubmitting = true;
      _errorMessage = null;
    });

    try {
      await ref
          .read(reviewsApiProvider)
          .submitReview(
            bookingId: widget.bookingId,
            rating: _rating,
            comment: _commentController.text.trim().isEmpty ? null : _commentController.text.trim(),
          );
      if (mounted) {
        Navigator.pop(context, true);
      }
    } on ApiException catch (e) {
      setState(() {
        _errorMessage = e.statusCode == 409 ? "You've already reviewed this trip." : e.message;
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
          Text('Rate ${widget.revieweeLabel}', style: theme.textTheme.titleLarge),
          const SizedBox(height: HikaSpacing.lg),
          Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              for (var star = 1; star <= 5; star++)
                IconButton(
                  iconSize: 36,
                  icon: Icon(
                    star <= _rating ? Icons.star_rounded : Icons.star_outline_rounded,
                    color: HikaColors.warning,
                  ),
                  onPressed: () => setState(() {
                    _rating = star;
                    _errorMessage = null;
                  }),
                ),
            ],
          ),
          const SizedBox(height: HikaSpacing.md),
          HikaTextField(
            label: 'Comment (optional)',
            controller: _commentController,
            hintText: 'How was the trip?',
          ),
          if (_errorMessage != null) ...[
            const SizedBox(height: HikaSpacing.sm),
            Text(_errorMessage!, style: TextStyle(color: theme.colorScheme.error)),
          ],
          const SizedBox(height: HikaSpacing.lg),
          HikaButton(label: 'Submit review', isLoading: _isSubmitting, onPressed: _submit),
        ],
      ),
    );
  }
}
