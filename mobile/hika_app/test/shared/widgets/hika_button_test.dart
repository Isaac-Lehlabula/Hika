import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:hika_app/shared/widgets/hika_button.dart';

void main() {
  Widget wrap(Widget child) => MaterialApp(home: Scaffold(body: child));

  testWidgets('tapping calls onPressed', (tester) async {
    var tapped = false;

    await tester.pumpWidget(wrap(HikaButton(label: 'Log in', onPressed: () => tapped = true)));
    await tester.tap(find.text('Log in'));

    expect(tapped, isTrue);
  });

  testWidgets('loading state shows a spinner and disables the button', (tester) async {
    var tapped = false;

    await tester.pumpWidget(wrap(HikaButton(label: 'Log in', isLoading: true, onPressed: () => tapped = true)));

    expect(find.text('Log in'), findsNothing);
    expect(find.byType(CircularProgressIndicator), findsOneWidget);

    await tester.tap(find.byType(ElevatedButton));
    expect(tapped, isFalse);
  });

  testWidgets('secondary variant renders an OutlinedButton', (tester) async {
    await tester.pumpWidget(
      wrap(HikaButton(label: 'Cancel', variant: HikaButtonVariant.secondary, onPressed: () {})),
    );

    expect(find.byType(OutlinedButton), findsOneWidget);
  });
}
