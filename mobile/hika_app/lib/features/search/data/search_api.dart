import 'package:intl/intl.dart';

import '../../../core/networking/api_client.dart';
import 'search_models.dart';

/// Mirrors backend/src/Hika.Api/Controllers/SearchController.cs 1:1.
class SearchApi {
  SearchApi(this._client);

  final ApiClient _client;

  Future<PagedSearchResult<SearchTripResult>> searchTrips(SearchTripsQuery query, {int page = 1, int pageSize = 20}) async {
    final params = <String, dynamic>{
      'from': query.from,
      'to': query.to,
      'passengers': query.passengers,
      'sort': query.sort.wireValue,
      'verifiedOnly': query.verifiedOnly,
      'page': page,
      'pageSize': pageSize,
    };
    if (query.date != null) {
      params['date'] = DateFormat('yyyy-MM-dd').format(query.date!);
    }
    if (query.maxPrice != null) {
      params['maxPrice'] = query.maxPrice;
    }

    final body = await _client.get('/api/v1/search/trips', query: params);
    final items = (body!['items'] as List<dynamic>)
        .map((t) => SearchTripResult.fromJson(t as Map<String, dynamic>))
        .toList();

    return PagedSearchResult(
      items: items,
      page: body['page'] as int,
      pageSize: body['pageSize'] as int,
      totalCount: body['totalCount'] as int,
    );
  }

  Future<List<LocationSuggestion>> searchLocations(String query) async {
    if (query.trim().isEmpty) {
      return [];
    }
    final list = await _client.getList('/api/v1/search/locations', query: {'query': query});
    return list.map((l) => LocationSuggestion.fromJson(l as Map<String, dynamic>)).toList();
  }

  Future<List<PopularRoute>> getPopularRoutes({DateTime? month}) async {
    final params = month == null ? null : {'month': DateFormat('yyyy-MM-dd').format(DateTime(month.year, month.month, 1))};
    final list = await _client.getList('/api/v1/search/popular-routes', query: params);
    return list.map((r) => PopularRoute.fromJson(r as Map<String, dynamic>)).toList();
  }
}
