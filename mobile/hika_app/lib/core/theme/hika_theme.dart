import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

import 'hika_colors.dart';
import 'hika_spacing.dart';

/// The Hika design system's ThemeData. Material 3 as the base, customized
/// enough (palette, type, component shapes) that it doesn't read as a stock
/// Flutter app — see docs/mobile-architecture.md §6.
abstract final class HikaTheme {
  static TextTheme _textTheme(Color primaryText, Color secondaryText) {
    final base = GoogleFonts.plusJakartaSansTextTheme();
    return base
        .copyWith(
          displayLarge: base.displayLarge?.copyWith(fontWeight: FontWeight.w700, color: primaryText, height: 1.1),
          displayMedium: base.displayMedium?.copyWith(fontWeight: FontWeight.w700, color: primaryText, height: 1.15),
          headlineLarge: base.headlineLarge?.copyWith(fontWeight: FontWeight.w700, color: primaryText, height: 1.2),
          headlineMedium: base.headlineMedium?.copyWith(fontWeight: FontWeight.w700, color: primaryText),
          headlineSmall: base.headlineSmall?.copyWith(fontWeight: FontWeight.w600, color: primaryText),
          titleLarge: base.titleLarge?.copyWith(fontWeight: FontWeight.w600, color: primaryText),
          titleMedium: base.titleMedium?.copyWith(fontWeight: FontWeight.w600, color: primaryText),
          titleSmall: base.titleSmall?.copyWith(fontWeight: FontWeight.w600, color: primaryText),
          bodyLarge: base.bodyLarge?.copyWith(color: primaryText, height: 1.4),
          bodyMedium: base.bodyMedium?.copyWith(color: primaryText, height: 1.4),
          bodySmall: base.bodySmall?.copyWith(color: secondaryText, height: 1.35),
          labelLarge: base.labelLarge?.copyWith(fontWeight: FontWeight.w600, color: primaryText),
          labelMedium: base.labelMedium?.copyWith(fontWeight: FontWeight.w600, color: secondaryText),
        )
        .apply(bodyColor: primaryText, displayColor: primaryText);
  }

  static ThemeData light() {
    const colorScheme = ColorScheme.light(
      primary: HikaColors.primary,
      onPrimary: Colors.white,
      secondary: HikaColors.accent,
      onSecondary: Colors.white,
      error: HikaColors.danger,
      onError: Colors.white,
      surface: HikaColors.surfaceLight,
      onSurface: HikaColors.textPrimaryLight,
    );

    return _build(
      colorScheme: colorScheme,
      scaffoldBackground: HikaColors.backgroundLight,
      cardColor: HikaColors.surfaceLight,
      borderColor: HikaColors.borderLight,
      textTheme: _textTheme(HikaColors.textPrimaryLight, HikaColors.textSecondaryLight),
      secondaryText: HikaColors.textSecondaryLight,
    );
  }

  static ThemeData dark() {
    const colorScheme = ColorScheme.dark(
      primary: HikaColors.primaryLight,
      onPrimary: HikaColors.backgroundDark,
      secondary: HikaColors.accent,
      onSecondary: Colors.white,
      error: HikaColors.danger,
      onError: Colors.white,
      surface: HikaColors.surfaceDark,
      onSurface: HikaColors.textPrimaryDark,
    );

    return _build(
      colorScheme: colorScheme,
      scaffoldBackground: HikaColors.backgroundDark,
      cardColor: HikaColors.surfaceDark,
      borderColor: HikaColors.borderDark,
      textTheme: _textTheme(HikaColors.textPrimaryDark, HikaColors.textSecondaryDark),
      secondaryText: HikaColors.textSecondaryDark,
    );
  }

  static ThemeData _build({
    required ColorScheme colorScheme,
    required Color scaffoldBackground,
    required Color cardColor,
    required Color borderColor,
    required TextTheme textTheme,
    required Color secondaryText,
  }) {
    return ThemeData(
      useMaterial3: true,
      colorScheme: colorScheme,
      scaffoldBackgroundColor: scaffoldBackground,
      textTheme: textTheme,
      dividerColor: borderColor,
      splashFactory: InkSparkle.splashFactory,
      appBarTheme: AppBarTheme(
        backgroundColor: scaffoldBackground,
        foregroundColor: colorScheme.onSurface,
        elevation: 0,
        centerTitle: false,
        titleTextStyle: textTheme.titleLarge,
      ),
      cardTheme: CardThemeData(
        color: cardColor,
        elevation: 0,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(HikaRadius.lg),
          side: BorderSide(color: borderColor),
        ),
        margin: EdgeInsets.zero,
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: colorScheme.primary,
          foregroundColor: colorScheme.onPrimary,
          disabledBackgroundColor: colorScheme.primary.withValues(alpha: 0.4),
          disabledForegroundColor: colorScheme.onPrimary.withValues(alpha: 0.8),
          minimumSize: const Size.fromHeight(52),
          textStyle: textTheme.labelLarge?.copyWith(fontSize: 16),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(HikaRadius.md)),
          elevation: 0,
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: colorScheme.onSurface,
          minimumSize: const Size.fromHeight(52),
          side: BorderSide(color: borderColor),
          textStyle: textTheme.labelLarge?.copyWith(fontSize: 16),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(HikaRadius.md)),
        ),
      ),
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(
          foregroundColor: colorScheme.primary,
          textStyle: textTheme.labelLarge,
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: cardColor,
        contentPadding: const EdgeInsets.symmetric(horizontal: HikaSpacing.md, vertical: HikaSpacing.md),
        hintStyle: textTheme.bodyLarge?.copyWith(color: secondaryText),
        labelStyle: textTheme.bodyMedium?.copyWith(color: secondaryText),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(HikaRadius.md),
          borderSide: BorderSide(color: borderColor),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(HikaRadius.md),
          borderSide: BorderSide(color: borderColor),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(HikaRadius.md),
          borderSide: BorderSide(color: colorScheme.primary, width: 1.5),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(HikaRadius.md),
          borderSide: BorderSide(color: colorScheme.error),
        ),
        focusedErrorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(HikaRadius.md),
          borderSide: BorderSide(color: colorScheme.error, width: 1.5),
        ),
      ),
      chipTheme: ChipThemeData(
        backgroundColor: cardColor,
        side: BorderSide(color: borderColor),
        labelStyle: textTheme.labelMedium,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(HikaRadius.pill)),
        padding: const EdgeInsets.symmetric(horizontal: HikaSpacing.sm, vertical: HikaSpacing.xxs),
      ),
      bottomNavigationBarTheme: BottomNavigationBarThemeData(
        backgroundColor: cardColor,
        selectedItemColor: colorScheme.primary,
        unselectedItemColor: secondaryText,
        type: BottomNavigationBarType.fixed,
        showUnselectedLabels: true,
      ),
      snackBarTheme: SnackBarThemeData(
        backgroundColor: colorScheme.onSurface,
        contentTextStyle: textTheme.bodyMedium?.copyWith(color: scaffoldBackground),
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(HikaRadius.md)),
      ),
      progressIndicatorTheme: ProgressIndicatorThemeData(color: colorScheme.primary),
      dividerTheme: DividerThemeData(color: borderColor, thickness: 1, space: 1),
    );
  }
}
