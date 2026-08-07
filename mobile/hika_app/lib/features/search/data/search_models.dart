import '../../trips/data/trip.dart';

/// Mirrors backend Hika.Application.Search.Dtos.TripSearchSort — serialized by member name.
enum TripSearchSort {
  departureTime('DepartureTime', 'Departure time'),
  price('Price', 'Price'),
  driverRating('DriverRating', 'Driver rating'),
  seatsAvailable('SeatsAvailable', 'Seats available');

  const TripSearchSort(this.wireValue, this.displayName);

  final String wireValue;
  final String displayName;
}

class SearchTripsQuery {
  const SearchTripsQuery({
    required this.from,
    required this.to,
    this.date,
    this.passengers = 1,
    this.sort = TripSearchSort.departureTime,
    this.verifiedOnly = false,
    this.maxPrice,
  });

  final String from;
  final String to;
  final DateTime? date;
  final int passengers;
  final TripSearchSort sort;
  final bool verifiedOnly;
  final double? maxPrice;

  SearchTripsQuery copyWith({TripSearchSort? sort, bool? verifiedOnly}) => SearchTripsQuery(
    from: from,
    to: to,
    date: date,
    passengers: passengers,
    sort: sort ?? this.sort,
    verifiedOnly: verifiedOnly ?? this.verifiedOnly,
    maxPrice: maxPrice,
  );
}

/// One search result — boarding/alighting describe the requested sub-leg of the trip, which
/// isn't necessarily the trip's full origin/destination (see docs/domain-model.md §4).
class SearchTripResult {
  const SearchTripResult({
    required this.id,
    required this.departureAtUtc,
    required this.boardingStopName,
    required this.boardingProvince,
    required this.alightingStopName,
    required this.alightingProvince,
    required this.totalSeatsOffered,
    required this.seatsAvailable,
    required this.pricePerSeat,
    required this.driver,
  });

  factory SearchTripResult.fromJson(Map<String, dynamic> json) => SearchTripResult(
    id: json['id'] as String,
    departureAtUtc: DateTime.parse(json['departureAtUtc'] as String),
    boardingStopName: json['boardingStopName'] as String,
    boardingProvince: json['boardingProvince'] as String,
    alightingStopName: json['alightingStopName'] as String,
    alightingProvince: json['alightingProvince'] as String,
    totalSeatsOffered: json['totalSeatsOffered'] as int,
    seatsAvailable: json['seatsAvailable'] as int,
    pricePerSeat: (json['pricePerSeat'] as num).toDouble(),
    driver: TripDriverSummary.fromJson(json['driver'] as Map<String, dynamic>),
  );

  final String id;
  final DateTime departureAtUtc;
  final String boardingStopName;
  final String boardingProvince;
  final String alightingStopName;
  final String alightingProvince;
  final int totalSeatsOffered;
  final int seatsAvailable;
  final double pricePerSeat;
  final TripDriverSummary driver;
}

class PagedSearchResult<T> {
  const PagedSearchResult({required this.items, required this.page, required this.pageSize, required this.totalCount});

  final List<T> items;
  final int page;
  final int pageSize;
  final int totalCount;
}

class LocationSuggestion {
  const LocationSuggestion({required this.id, required this.name, required this.province, required this.type});

  factory LocationSuggestion.fromJson(Map<String, dynamic> json) => LocationSuggestion(
    id: json['id'] as String,
    name: json['name'] as String,
    province: json['province'] as String,
    type: json['type'] as String,
  );

  final String id;
  final String name;
  final String province;
  final String type;
}

class PopularRoute {
  const PopularRoute({required this.originName, required this.destinationName, required this.tripCount});

  factory PopularRoute.fromJson(Map<String, dynamic> json) => PopularRoute(
    originName: json['originName'] as String,
    destinationName: json['destinationName'] as String,
    tripCount: json['tripCount'] as int,
  );

  final String originName;
  final String destinationName;
  final int tripCount;

  String get label => '$originName → $destinationName';
}
