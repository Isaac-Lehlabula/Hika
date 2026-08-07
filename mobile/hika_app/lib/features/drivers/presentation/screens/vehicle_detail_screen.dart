import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:image_picker/image_picker.dart';

import '../../../../core/networking/api_exception.dart';
import '../../../../core/theme/hika_colors.dart';
import '../../../../core/theme/hika_spacing.dart';
import '../../../../shared/widgets/hika_badge.dart';
import '../../../../shared/widgets/hika_button.dart';
import '../../../../shared/widgets/hika_card.dart';
import '../providers/vehicle_detail_controller.dart';
import '../providers/vehicles_controller.dart';

class VehicleDetailScreen extends ConsumerStatefulWidget {
  const VehicleDetailScreen({required this.vehicleId, super.key});

  final String vehicleId;

  @override
  ConsumerState<VehicleDetailScreen> createState() => _VehicleDetailScreenState();
}

class _VehicleDetailScreenState extends ConsumerState<VehicleDetailScreen> {
  bool _isBusy = false;

  Future<void> _addPhoto({required bool isPrimary}) async {
    final picked = await ImagePicker().pickImage(source: ImageSource.gallery, imageQuality: 85);
    if (picked == null) {
      return;
    }

    setState(() => _isBusy = true);
    try {
      await ref
          .read(vehicleDetailControllerProvider(widget.vehicleId).notifier)
          .uploadPhoto(filePath: picked.path, isPrimary: isPrimary);
    } on ApiException catch (e) {
      _showError(e.message);
    } finally {
      if (mounted) {
        setState(() => _isBusy = false);
      }
    }
  }

  Future<void> _submitVerification() async {
    final picked = await ImagePicker().pickImage(source: ImageSource.gallery, imageQuality: 85);
    if (picked == null) {
      return;
    }

    setState(() => _isBusy = true);
    try {
      await ref.read(vehicleDetailControllerProvider(widget.vehicleId).notifier).submitVerification(picked.path);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Submitted for review.')));
      }
    } on ApiException catch (e) {
      _showError(e.message);
    } finally {
      if (mounted) {
        setState(() => _isBusy = false);
      }
    }
  }

  Future<void> _delete() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Remove vehicle?'),
        content: const Text('This removes the vehicle and its photos. This cannot be undone.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Cancel')),
          TextButton(onPressed: () => Navigator.pop(context, true), child: const Text('Remove')),
        ],
      ),
    );
    if (confirmed != true) {
      return;
    }

    setState(() => _isBusy = true);
    try {
      await ref.read(vehiclesControllerProvider.notifier).delete(widget.vehicleId);
      if (mounted) {
        context.pop();
      }
    } on ApiException catch (e) {
      _showError(e.message);
      if (mounted) {
        setState(() => _isBusy = false);
      }
    }
  }

  void _showError(String message) {
    if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
    }
  }

  @override
  Widget build(BuildContext context) {
    final vehicleAsync = ref.watch(vehicleDetailControllerProvider(widget.vehicleId));
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Vehicle'),
        actions: [IconButton(onPressed: _isBusy ? null : _delete, icon: const Icon(Icons.delete_outline))],
      ),
      body: vehicleAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => Center(
          child: HikaButton(
            label: 'Try again',
            variant: HikaButtonVariant.secondary,
            onPressed: () => ref.read(vehicleDetailControllerProvider(widget.vehicleId).notifier).refresh(),
          ),
        ),
        data: (vehicle) => ListView(
          padding: const EdgeInsets.all(HikaSpacing.lg),
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(vehicle.displayName, style: theme.textTheme.headlineSmall),
                (vehicle.isVerified ? 'Verified' : 'NotSubmitted').toVerificationBadge(),
              ],
            ),
            Text('${vehicle.color} · ${vehicle.registrationNumber} · ${vehicle.seatCapacity} seats'),
            const SizedBox(height: HikaSpacing.xl),
            Text('Photos', style: theme.textTheme.titleMedium),
            const SizedBox(height: HikaSpacing.sm),
            GridView.builder(
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                crossAxisCount: 3,
                crossAxisSpacing: HikaSpacing.xs,
                mainAxisSpacing: HikaSpacing.xs,
              ),
              itemCount: vehicle.photos.length + 1,
              itemBuilder: (context, index) {
                if (index == vehicle.photos.length) {
                  return _AddPhotoTile(isBusy: _isBusy, onTap: () => _addPhoto(isPrimary: vehicle.photos.isEmpty));
                }
                final photo = vehicle.photos[index];
                return ClipRRect(
                  borderRadius: BorderRadius.circular(HikaRadius.sm),
                  child: Stack(
                    fit: StackFit.expand,
                    children: [
                      Image.network(photo.url, fit: BoxFit.cover),
                      if (photo.isPrimary)
                        const Positioned(
                          top: 4,
                          left: 4,
                          child: HikaBadge(label: 'Primary', tone: HikaBadgeTone.neutral),
                        ),
                    ],
                  ),
                );
              },
            ),
            const SizedBox(height: HikaSpacing.xl),
            HikaCard(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('Verification', style: theme.textTheme.titleMedium),
                  const SizedBox(height: HikaSpacing.xs),
                  Text(
                    'Submit a photo of your vehicle registration (NATIS) document.',
                    style: theme.textTheme.bodyMedium?.copyWith(color: HikaColors.textSecondaryLight),
                  ),
                  const SizedBox(height: HikaSpacing.md),
                  HikaButton(
                    label: 'Submit registration document',
                    variant: HikaButtonVariant.secondary,
                    icon: Icons.description_outlined,
                    isLoading: _isBusy,
                    onPressed: _submitVerification,
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _AddPhotoTile extends StatelessWidget {
  const _AddPhotoTile({required this.isBusy, required this.onTap});

  final bool isBusy;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: isBusy ? null : onTap,
      borderRadius: BorderRadius.circular(HikaRadius.sm),
      child: DottedBorderBox(
        child: isBusy
            ? const Center(child: CircularProgressIndicator(strokeWidth: 2))
            : const Icon(Icons.add_a_photo_outlined, color: HikaColors.accent),
      ),
    );
  }
}

class DottedBorderBox extends StatelessWidget {
  const DottedBorderBox({required this.child, super.key});

  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: HikaColors.accentLight.withValues(alpha: 0.3),
        borderRadius: BorderRadius.circular(HikaRadius.sm),
        border: Border.all(color: HikaColors.accent.withValues(alpha: 0.4)),
      ),
      child: Center(child: child),
    );
  }
}
