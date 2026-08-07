/// 4pt-based spacing scale. Never hardcode a raw padding/gap value in a
/// widget — pick the nearest token here, so spacing stays visually
/// consistent across every screen.
abstract final class HikaSpacing {
  static const double xxs = 4;
  static const double xs = 8;
  static const double sm = 12;
  static const double md = 16;
  static const double lg = 20;
  static const double xl = 24;
  static const double xxl = 32;
  static const double xxxl = 40;
  static const double huge = 64;
}

/// Corner-radius scale for cards, buttons, sheets, chips.
abstract final class HikaRadius {
  static const double sm = 8;
  static const double md = 12;
  static const double lg = 16;
  static const double xl = 24;
  static const double pill = 999;
}
