import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:hika_app/shared/widgets/hika_badge.dart';

void main() {
  Widget wrap(Widget child) => MaterialApp(home: Scaffold(body: child));

  testWidgets('toVerificationBadge maps each backend status to the right label', (tester) async {
    await tester.pumpWidget(
      wrap(
        Column(
          children: [
            'Verified'.toVerificationBadge(),
            'Pending'.toVerificationBadge(),
            'Rejected'.toVerificationBadge(),
            'NotSubmitted'.toVerificationBadge(),
          ],
        ),
      ),
    );

    expect(find.text('Verified'), findsOneWidget);
    expect(find.text('Pending review'), findsOneWidget);
    expect(find.text('Rejected'), findsOneWidget);
    expect(find.text('Not submitted'), findsOneWidget);
  });

  testWidgets('an unrecognized status falls back to Not submitted', (tester) async {
    await tester.pumpWidget(wrap('SomeUnknownStatus'.toVerificationBadge()));

    expect(find.text('Not submitted'), findsOneWidget);
  });

  testWidgets('toTripStatusBadge maps each backend status to the right label', (tester) async {
    await tester.pumpWidget(
      wrap(
        Column(
          children: [
            'Scheduled'.toTripStatusBadge(),
            'InProgress'.toTripStatusBadge(),
            'Completed'.toTripStatusBadge(),
            'Cancelled'.toTripStatusBadge(),
          ],
        ),
      ),
    );

    expect(find.text('Scheduled'), findsOneWidget);
    expect(find.text('In progress'), findsOneWidget);
    expect(find.text('Completed'), findsOneWidget);
    expect(find.text('Cancelled'), findsOneWidget);
  });
}
