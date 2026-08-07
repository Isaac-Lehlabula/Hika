import 'package:flutter/material.dart';

/// Central color palette. Never reference a raw [Color] value outside this
/// file — every screen pulls from here (or the derived [ColorScheme] in
/// `hika_theme.dart`) so the palette can change in one place.
abstract final class HikaColors {
  // Brand — warm terracotta, evokes South African sunsets and open-road travel.
  static const Color primary = Color(0xFFE05C2A);
  static const Color primaryDark = Color(0xFFB8431A);
  static const Color primaryLight = Color(0xFFF7A97C);

  // Accent — deep teal, used for trust/verification signals.
  static const Color accent = Color(0xFF12786B);
  static const Color accentLight = Color(0xFFCFE9E4);

  // Semantic.
  static const Color success = Color(0xFF2E9E5B);
  static const Color successLight = Color(0xFFDFF3E6);
  static const Color warning = Color(0xFFDB9A1F);
  static const Color warningLight = Color(0xFFFBF0D9);
  static const Color danger = Color(0xFFD84C3E);
  static const Color dangerLight = Color(0xFFFBE2DE);

  // Light theme neutrals — warm off-whites, not clinical grey.
  static const Color backgroundLight = Color(0xFFFFFBF7);
  static const Color surfaceLight = Color(0xFFFFFFFF);
  static const Color surfaceAltLight = Color(0xFFF7F0EA);
  static const Color borderLight = Color(0xFFEBE0D6);
  static const Color textPrimaryLight = Color(0xFF241A12);
  static const Color textSecondaryLight = Color(0xFF7A6C60);

  // Dark theme neutrals.
  static const Color backgroundDark = Color(0xFF17130F);
  static const Color surfaceDark = Color(0xFF221C17);
  static const Color surfaceAltDark = Color(0xFF2C241D);
  static const Color borderDark = Color(0xFF3A3129);
  static const Color textPrimaryDark = Color(0xFFF5EDE6);
  static const Color textSecondaryDark = Color(0xFFB9AA9C);
}
