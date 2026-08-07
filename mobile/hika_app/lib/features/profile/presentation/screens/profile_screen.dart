import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../../core/theme/hika_colors.dart';
import '../../../../core/theme/hika_spacing.dart';
import '../../../../shared/widgets/hika_button.dart';
import '../../../../shared/widgets/hika_card.dart';
import '../../../auth/presentation/providers/auth_controller.dart';
import '../../data/user_profile.dart';
import '../providers/profile_controller.dart';

class ProfileScreen extends ConsumerWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final profileAsync = ref.watch(profileControllerProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Profile')),
      body: profileAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => _ErrorState(onRetry: () => ref.read(profileControllerProvider.notifier).refresh()),
        data: (profile) => _ProfileContent(profile: profile),
      ),
    );
  }
}

class _ErrorState extends StatelessWidget {
  const _ErrorState({required this.onRetry});

  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(HikaSpacing.xl),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.cloud_off_outlined, size: 40, color: HikaColors.textSecondaryLight),
            const SizedBox(height: HikaSpacing.md),
            const Text("Couldn't load your profile."),
            const SizedBox(height: HikaSpacing.md),
            HikaButton(label: 'Try again', variant: HikaButtonVariant.secondary, onPressed: onRetry),
          ],
        ),
      ),
    );
  }
}

class _ProfileContent extends ConsumerWidget {
  const _ProfileContent({required this.profile});

  final UserProfile profile;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);

    return RefreshIndicator(
      onRefresh: () => ref.read(profileControllerProvider.notifier).refresh(),
      child: ListView(
        padding: const EdgeInsets.all(HikaSpacing.lg),
        children: [
          Center(
            child: Column(
              children: [
                CircleAvatar(
                  radius: 44,
                  backgroundColor: HikaColors.primaryLight,
                  child: Text(
                    profile.initials,
                    style: theme.textTheme.headlineMedium?.copyWith(color: Colors.white),
                  ),
                ),
                const SizedBox(height: HikaSpacing.md),
                Text(profile.fullName, style: theme.textTheme.headlineSmall),
                const SizedBox(height: HikaSpacing.xxs),
                Text(
                  'Member since ${DateFormat.yMMMM().format(profile.memberSinceUtc)}',
                  style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurface.withValues(alpha: 0.6)),
                ),
                const SizedBox(height: HikaSpacing.sm),
                Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(Icons.star_rounded, size: 18, color: HikaColors.warning),
                    const SizedBox(width: 4),
                    Text(
                      profile.averageRating == null ? 'No ratings yet' : profile.averageRating!.toStringAsFixed(1),
                      style: theme.textTheme.bodyMedium,
                    ),
                    const SizedBox(width: HikaSpacing.md),
                    Text('${profile.completedTripCount} trips', style: theme.textTheme.bodyMedium),
                  ],
                ),
              ],
            ),
          ),
          const SizedBox(height: HikaSpacing.xl),
          HikaCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Verification', style: theme.textTheme.titleMedium),
                const SizedBox(height: HikaSpacing.sm),
                _VerificationRow(label: 'Email', verified: profile.emailVerified, subtitle: profile.email),
                const SizedBox(height: HikaSpacing.sm),
                _VerificationRow(
                  label: 'Phone',
                  verified: profile.phoneVerified,
                  subtitle: profile.phoneNumber ?? 'Not added yet',
                  action: profile.phoneVerified
                      ? null
                      : HikaButton(
                          label: 'Verify',
                          variant: HikaButtonVariant.text,
                          onPressed: () async {
                            final verified = await context.push<bool>('/verify-phone');
                            if (verified == true) {
                              ref.read(profileControllerProvider.notifier).refresh();
                            }
                          },
                        ),
                ),
              ],
            ),
          ),
          const SizedBox(height: HikaSpacing.xl),
          HikaButton(
            label: 'Log out',
            variant: HikaButtonVariant.secondary,
            onPressed: () => ref.read(authControllerProvider.notifier).signOut(),
          ),
        ],
      ),
    );
  }
}

class _VerificationRow extends StatelessWidget {
  const _VerificationRow({required this.label, required this.verified, required this.subtitle, this.action});

  final String label;
  final bool verified;
  final String subtitle;
  final Widget? action;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Row(
      children: [
        Icon(
          verified ? Icons.verified_rounded : Icons.radio_button_unchecked,
          color: verified ? HikaColors.success : theme.colorScheme.onSurface.withValues(alpha: 0.35),
          size: 22,
        ),
        const SizedBox(width: HikaSpacing.sm),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(label, style: theme.textTheme.bodyMedium),
              Text(
                subtitle,
                style: theme.textTheme.bodySmall,
                overflow: TextOverflow.ellipsis,
              ),
            ],
          ),
        ),
        if (action != null) action!,
      ],
    );
  }
}
