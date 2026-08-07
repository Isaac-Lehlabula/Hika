import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/providers.dart';
import '../../data/search_models.dart';

class SearchResultsController extends AsyncNotifier<PagedSearchResult<SearchTripResult>> {
  SearchTripsQuery? _lastQuery;

  @override
  Future<PagedSearchResult<SearchTripResult>> build() async =>
      const PagedSearchResult(items: [], page: 1, pageSize: 20, totalCount: 0);

  Future<void> search(SearchTripsQuery query) async {
    _lastQuery = query;
    state = const AsyncLoading();
    state = await AsyncValue.guard(() => ref.read(searchApiProvider).searchTrips(query));
  }

  /// Re-runs the last search with a different sort/filter — used by the results screen's
  /// sort dropdown and "verified only" chip so they don't need to remember the original query.
  Future<void> updateAndResearch({TripSearchSort? sort, bool? verifiedOnly}) async {
    final base = _lastQuery;
    if (base == null) {
      return;
    }
    await search(base.copyWith(sort: sort, verifiedOnly: verifiedOnly));
  }
}

final searchResultsControllerProvider =
    AsyncNotifierProvider<SearchResultsController, PagedSearchResult<SearchTripResult>>(SearchResultsController.new);
