import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/theme/hika_spacing.dart';
import '../../../../shared/widgets/hika_empty_state.dart';
import '../../../../shared/widgets/hika_text_field.dart';
import '../../../trips/data/province.dart';
import '../providers/location_suggestions_provider.dart';

/// Bottom sheet for picking a From/To location: autocompletes against the backend's seeded
/// Location table, but always lets the rider fall back to whatever free text they typed — an
/// unlisted village must never block a search (same principle as trip posting).
class LocationPickerSheet extends ConsumerStatefulWidget {
  const LocationPickerSheet({required this.title, this.initialValue, super.key});

  final String title;
  final String? initialValue;

  static Future<String?> show(BuildContext context, {required String title, String? initialValue}) {
    return showModalBottomSheet<String>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      builder: (context) => LocationPickerSheet(title: title, initialValue: initialValue),
    );
  }

  @override
  ConsumerState<LocationPickerSheet> createState() => _LocationPickerSheetState();
}

class _LocationPickerSheetState extends ConsumerState<LocationPickerSheet> {
  late final _controller = TextEditingController(text: widget.initialValue);
  late String _query = widget.initialValue ?? '';

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final suggestionsAsync = ref.watch(locationSuggestionsProvider(_query));

    return DraggableScrollableSheet(
      initialChildSize: 0.75,
      minChildSize: 0.5,
      maxChildSize: 0.95,
      expand: false,
      builder: (context, scrollController) => Padding(
        padding: EdgeInsets.only(
          left: HikaSpacing.lg,
          right: HikaSpacing.lg,
          top: HikaSpacing.lg,
          bottom: MediaQuery.of(context).viewInsets.bottom + HikaSpacing.lg,
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(widget.title, style: theme.textTheme.titleLarge),
            const SizedBox(height: HikaSpacing.md),
            HikaTextField(
              label: 'Town or suburb',
              controller: _controller,
              prefixIcon: Icons.search,
              onChanged: (value) => setState(() => _query = value),
              onSubmitted: (value) => Navigator.pop(context, value.trim()),
            ),
            const SizedBox(height: HikaSpacing.sm),
            Expanded(
              child: _query.trim().isEmpty
                  ? const HikaEmptyState(
                      icon: Icons.location_on_outlined,
                      title: 'Start typing',
                      message: 'Search for a city, town, or suburb.',
                    )
                  : suggestionsAsync.when(
                      loading: () => const Center(child: CircularProgressIndicator()),
                      error: (_, _) => const Center(child: Text("Couldn't load suggestions.")),
                      data: (suggestions) => ListView(
                        controller: scrollController,
                        children: [
                          if (suggestions.isEmpty)
                            ListTile(
                              leading: const Icon(Icons.edit_location_alt_outlined),
                              title: Text('Use "${_controller.text.trim()}"'),
                              subtitle: const Text('Not in our list — that still works fine.'),
                              onTap: () => Navigator.pop(context, _controller.text.trim()),
                            ),
                          for (final suggestion in suggestions)
                            ListTile(
                              leading: const Icon(Icons.location_on_outlined),
                              title: Text(suggestion.name),
                              subtitle: Text(Province.fromWireValue(suggestion.province).displayName),
                              onTap: () => Navigator.pop(context, suggestion.name),
                            ),
                        ],
                      ),
                    ),
            ),
          ],
        ),
      ),
    );
  }
}
