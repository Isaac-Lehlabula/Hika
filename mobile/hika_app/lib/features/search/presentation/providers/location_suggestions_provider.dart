import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/providers.dart';
import '../../data/search_models.dart';

/// Cached per query string automatically by Riverpod's family — retyping a prefix the user
/// already searched doesn't re-hit the network.
final locationSuggestionsProvider = FutureProvider.family<List<LocationSuggestion>, String>(
  (ref, query) => ref.read(searchApiProvider).searchLocations(query),
);
