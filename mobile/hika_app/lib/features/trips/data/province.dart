/// Mirrors backend Hika.Domain.Common.Province — 9 SA provinces. The API serializes enums by
/// member name (JsonStringEnumConverter), so [wireValue] must match the C# member name exactly.
enum Province {
  easternCape('EasternCape', 'Eastern Cape'),
  freeState('FreeState', 'Free State'),
  gauteng('Gauteng', 'Gauteng'),
  kwaZuluNatal('KwaZuluNatal', 'KwaZulu-Natal'),
  limpopo('Limpopo', 'Limpopo'),
  mpumalanga('Mpumalanga', 'Mpumalanga'),
  northWest('NorthWest', 'North West'),
  northernCape('NorthernCape', 'Northern Cape'),
  westernCape('WesternCape', 'Western Cape');

  const Province(this.wireValue, this.displayName);

  final String wireValue;
  final String displayName;

  static Province fromWireValue(String value) =>
      Province.values.firstWhere((p) => p.wireValue == value, orElse: () => Province.gauteng);
}
