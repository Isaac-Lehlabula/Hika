import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../../core/theme/hika_colors.dart';
import '../../../../core/theme/hika_spacing.dart';
import '../../../../shared/widgets/hika_button.dart';
import '../../../../shared/widgets/hika_card.dart';
import '../../../../shared/widgets/hika_empty_state.dart';
import '../../../ride_alerts/presentation/screens/my_ride_alerts_screen.dart';
import '../../../ride_requests/presentation/screens/ride_requests_screen.dart';
import '../../data/search_models.dart';
import '../providers/search_results_controller.dart';

class SearchResultsScreen extends ConsumerStatefulWidget {
  const SearchResultsScreen({required this.query, super.key});

  final SearchTripsQuery query;

  @override
  ConsumerState<SearchResultsScreen> createState() => _SearchResultsScreenState();
}

class _SearchResultsScreenState extends ConsumerState<SearchResultsScreen> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(searchResultsControllerProvider.notifier).search(widget.query));
  }

  @override
  Widget build(BuildContext context) {
    final resultsAsync = ref.watch(searchResultsControllerProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text('${widget.query.from} → ${widget.query.to}'),
        actions: [
          PopupMenuButton<TripSearchSort>(
            icon: const Icon(Icons.sort),
            onSelected: (sort) => ref.read(searchResultsControllerProvider.notifier).updateAndResearch(sort: sort),
            itemBuilder: (context) => [
              for (final sort in TripSearchSort.values) PopupMenuItem(value: sort, child: Text(sort.displayName)),
            ],
          ),
        ],
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: HikaSpacing.lg, vertical: HikaSpacing.sm),
            child: Align(
              alignment: Alignment.centerLeft,
              child: FilterChip(
                label: const Text('Verified drivers only'),
                avatar: const Icon(Icons.verified_rounded, size: 16),
                selected: widget.query.verifiedOnly,
                onSelected: (selected) =>
                    ref.read(searchResultsControllerProvider.notifier).updateAndResearch(verifiedOnly: selected),
              ),
            ),
          ),
          Expanded(
            child: resultsAsync.when(
              loading: () => const Center(child: CircularProgressIndicator()),
              error: (error, _) => Center(
                child: HikaButton(
                  label: 'Try again',
                  variant: HikaButtonVariant.secondary,
                  onPressed: () => ref.read(searchResultsControllerProvider.notifier).search(widget.query),
                ),
              ),
              data: (result) {
                if (result.items.isEmpty) {
                  return HikaEmptyState(
                    icon: Icons.search_off_rounded,
                    title: 'No hikes found',
                    message: 'Post what you need so a driver can claim it directly, or just get notified when a match appears.',
                    action: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        HikaButton(
                          label: 'Post a ride request',
                          icon: Icons.campaign_outlined,
                          onPressed: () => PostRideRequestSheet.show(
                            context,
                            initialOrigin: widget.query.from,
                            initialDestination: widget.query.to,
                            initialTravelDate: widget.query.date,
                          ),
                        ),
                        const SizedBox(height: HikaSpacing.sm),
                        HikaButton(
                          label: 'Notify me instead',
                          variant: HikaButtonVariant.text,
                          icon: Icons.notifications_active_outlined,
                          onPressed: () => CreateRideAlertSheet.show(
                            context,
                            initialOrigin: widget.query.from,
                            initialDestination: widget.query.to,
                            initialTravelDate: widget.query.date,
                          ),
                        ),
                      ],
                    ),
                  );
                }

                return ListView.separated(
                  padding: const EdgeInsets.fromLTRB(HikaSpacing.lg, 0, HikaSpacing.lg, HikaSpacing.lg),
                  itemCount: result.items.length,
                  separatorBuilder: (_, _) => const SizedBox(height: HikaSpacing.md),
                  itemBuilder: (context, index) => _SearchResultCard(result: result.items[index]),
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}

class _SearchResultCard extends StatelessWidget {
  const _SearchResultCard({required this.result});

  final SearchTripResult result;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return HikaCard(
      onTap: () => context.push('/trips/${result.id}'),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              CircleAvatar(
                radius: 20,
                backgroundColor: HikaColors.accentLight,
                backgroundImage: result.driver.photoUrl == null ? null : NetworkImage(result.driver.photoUrl!),
                child: result.driver.photoUrl == null
                    ? Text(result.driver.firstName.substring(0, 1), style: theme.textTheme.titleMedium)
                    : null,
              ),
              const SizedBox(width: HikaSpacing.sm),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Flexible(child: Text(result.driver.fullName, style: theme.textTheme.titleSmall, overflow: TextOverflow.ellipsis)),
                        if (result.driver.isVerifiedDriver) ...[
                          const SizedBox(width: HikaSpacing.xxs),
                          const Icon(Icons.verified_rounded, size: 14, color: HikaColors.accent),
                        ],
                      ],
                    ),
                    Text(
                      result.driver.averageRating == null
                          ? '${result.driver.completedTripCount} trips'
                          : '★ ${result.driver.averageRating!.toStringAsFixed(1)} · ${result.driver.completedTripCount} trips',
                      style: theme.textTheme.bodySmall,
                    ),
                  ],
                ),
              ),
              Text(
                'R${result.pricePerSeat.toStringAsFixed(0)}',
                style: theme.textTheme.titleMedium?.copyWith(color: HikaColors.primary),
              ),
            ],
          ),
          const Divider(height: HikaSpacing.xl),
          Row(
            children: [
              const Icon(Icons.trip_origin, size: 14, color: HikaColors.accent),
              const SizedBox(width: HikaSpacing.xs),
              Expanded(child: Text(result.boardingStopName, style: theme.textTheme.bodyMedium)),
            ],
          ),
          const Padding(
            padding: EdgeInsets.only(left: 6),
            child: SizedBox(height: 16, child: VerticalDivider(width: 2, thickness: 2)),
          ),
          Row(
            children: [
              const Icon(Icons.location_on, size: 14, color: HikaColors.primary),
              const SizedBox(width: HikaSpacing.xs),
              Expanded(child: Text(result.alightingStopName, style: theme.textTheme.bodyMedium)),
            ],
          ),
          const SizedBox(height: HikaSpacing.sm),
          Row(
            children: [
              Icon(Icons.schedule, size: 14, color: theme.colorScheme.onSurface.withValues(alpha: 0.5)),
              const SizedBox(width: HikaSpacing.xxs),
              Text(DateFormat('EEE d MMM, HH:mm').format(result.departureAtUtc.toLocal()), style: theme.textTheme.bodySmall),
              const Spacer(),
              Icon(Icons.event_seat_outlined, size: 14, color: theme.colorScheme.onSurface.withValues(alpha: 0.5)),
              const SizedBox(width: HikaSpacing.xxs),
              Text('${result.seatsAvailable} seat${result.seatsAvailable == 1 ? '' : 's'} left', style: theme.textTheme.bodySmall),
            ],
          ),
        ],
      ),
    );
  }
}
