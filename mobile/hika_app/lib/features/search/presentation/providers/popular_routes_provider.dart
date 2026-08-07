import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/providers.dart';
import '../../data/search_models.dart';

/// Backs the home screen's "Popular this month" strip — aggregated from real posted trips,
/// never hardcoded (see backend SearchService.GetPopularRoutesAsync).
final popularRoutesProvider = FutureProvider<List<PopularRoute>>(
  (ref) => ref.read(searchApiProvider).getPopularRoutes(),
);
