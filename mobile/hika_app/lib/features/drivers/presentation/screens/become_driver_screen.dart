import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:image_picker/image_picker.dart';
import 'package:intl/intl.dart';

import '../../../../core/networking/api_exception.dart';
import '../../../../core/theme/hika_colors.dart';
import '../../../../core/theme/hika_spacing.dart';
import '../../../../shared/widgets/hika_badge.dart';
import '../../../../shared/widgets/hika_button.dart';
import '../../../../shared/widgets/hika_card.dart';
import '../../../../shared/widgets/hika_text_field.dart';
import '../../data/driver_profile.dart';
import '../providers/driver_profile_controller.dart';

class BecomeDriverScreen extends ConsumerWidget {
  const BecomeDriverScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final profileAsync = ref.watch(driverProfileControllerProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Driving')),
      body: profileAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => Center(
          child: Padding(
            padding: const EdgeInsets.all(HikaSpacing.xl),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                const Text("Couldn't load your driver profile."),
                const SizedBox(height: HikaSpacing.md),
                HikaButton(
                  label: 'Try again',
                  variant: HikaButtonVariant.secondary,
                  onPressed: () => ref.read(driverProfileControllerProvider.notifier).refresh(),
                ),
              ],
            ),
          ),
        ),
        data: (profile) =>
            profile == null ? const _DriverProfileForm() : _DriverProfileSummary(profile: profile),
      ),
    );
  }
}

class _DriverProfileForm extends ConsumerStatefulWidget {
  const _DriverProfileForm();

  @override
  ConsumerState<_DriverProfileForm> createState() => _DriverProfileFormState();
}

class _DriverProfileFormState extends ConsumerState<_DriverProfileForm> {
  final _licenseController = TextEditingController();
  DateTime? _expiryDate;
  bool _isSubmitting = false;
  String? _errorMessage;

  @override
  void dispose() {
    _licenseController.dispose();
    super.dispose();
  }

  Future<void> _pickExpiryDate() async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: now.add(const Duration(days: 365)),
      firstDate: now,
      lastDate: now.add(const Duration(days: 365 * 15)),
    );
    if (picked != null) {
      setState(() => _expiryDate = picked);
    }
  }

  Future<void> _submit() async {
    if (_licenseController.text.trim().isEmpty || _expiryDate == null) {
      setState(() => _errorMessage = 'Enter your license number and expiry date.');
      return;
    }

    setState(() {
      _isSubmitting = true;
      _errorMessage = null;
    });

    try {
      await ref
          .read(driverProfileControllerProvider.notifier)
          .createOrUpdate(licenseNumber: _licenseController.text.trim(), licenseExpiryDate: _expiryDate!);
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

    return SingleChildScrollView(
      padding: const EdgeInsets.all(HikaSpacing.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('I want to drive', style: theme.textTheme.headlineSmall),
          const SizedBox(height: HikaSpacing.xs),
          Text(
            "Add your driver's license details to start posting trips. You'll verify your license and vehicle afterwards.",
            style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurface.withValues(alpha: 0.6)),
          ),
          const SizedBox(height: HikaSpacing.xl),
          HikaTextField(label: "Driver's license number", controller: _licenseController, prefixIcon: Icons.badge_outlined),
          const SizedBox(height: HikaSpacing.md),
          InkWell(
            onTap: _pickExpiryDate,
            borderRadius: BorderRadius.circular(HikaRadius.md),
            child: InputDecorator(
              decoration: const InputDecoration(labelText: 'License expiry date'),
              child: Text(
                _expiryDate == null ? 'Select a date' : DateFormat.yMMMd().format(_expiryDate!),
              ),
            ),
          ),
          if (_errorMessage != null) ...[
            const SizedBox(height: HikaSpacing.md),
            Text(_errorMessage!, style: TextStyle(color: theme.colorScheme.error)),
          ],
          const SizedBox(height: HikaSpacing.xl),
          HikaButton(label: 'Continue', isLoading: _isSubmitting, onPressed: _submit),
        ],
      ),
    );
  }
}

class _DriverProfileSummary extends ConsumerStatefulWidget {
  const _DriverProfileSummary({required this.profile});

  final DriverProfile profile;

  @override
  ConsumerState<_DriverProfileSummary> createState() => _DriverProfileSummaryState();
}

class _DriverProfileSummaryState extends ConsumerState<_DriverProfileSummary> {
  bool _isUploading = false;

  Future<void> _submitDocument() async {
    final picker = ImagePicker();
    final picked = await picker.pickImage(source: ImageSource.gallery, imageQuality: 85);
    if (picked == null) {
      return;
    }

    setState(() => _isUploading = true);
    try {
      await ref.read(driverProfileControllerProvider.notifier).submitLicenseVerification(picked.path);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('License submitted for review.')));
      }
    } on ApiException catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.message)));
      }
    } finally {
      if (mounted) {
        setState(() => _isUploading = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final profile = widget.profile;

    return RefreshIndicator(
      onRefresh: () => ref.read(driverProfileControllerProvider.notifier).refresh(),
      child: ListView(
        padding: const EdgeInsets.all(HikaSpacing.lg),
        children: [
          HikaCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text('Driver license', style: theme.textTheme.titleMedium),
                    profile.verificationStatus.toVerificationBadge(),
                  ],
                ),
                const SizedBox(height: HikaSpacing.sm),
                Text('License number: ${profile.licenseNumber}'),
                Text('Expires: ${DateFormat.yMMMd().format(profile.licenseExpiryDate)}'),
                const SizedBox(height: HikaSpacing.md),
                HikaButton(
                  label: profile.verificationStatus == 'NotSubmitted' ? 'Submit license photo' : 'Resubmit license photo',
                  variant: HikaButtonVariant.secondary,
                  icon: Icons.camera_alt_outlined,
                  isLoading: _isUploading,
                  onPressed: _submitDocument,
                ),
              ],
            ),
          ),
          const SizedBox(height: HikaSpacing.lg),
          HikaCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Vehicles', style: theme.textTheme.titleMedium),
                const SizedBox(height: HikaSpacing.xs),
                Text(
                  'Add at least one vehicle and get it verified before posting trips.',
                  style: theme.textTheme.bodyMedium?.copyWith(color: HikaColors.textSecondaryLight),
                ),
                const SizedBox(height: HikaSpacing.md),
                HikaButton(
                  label: 'Manage vehicles',
                  icon: Icons.directions_car_outlined,
                  onPressed: () => context.push('/vehicles'),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
