import 'package:flutter_test/flutter_test.dart';
import 'package:hika_app/features/bookings/data/payment.dart';

void main() {
  test('Payment.fromJson round-trips the backend PaymentResponse shape', () {
    final payment = Payment.fromJson({
      'id': 'pay1',
      'bookingId': 'b1',
      'amount': 800,
      'platformFee': 120,
      'driverPayoutAmount': 680,
      'provider': 'Mock',
      'providerReference': 'MOCK-ABC123',
      'status': 'Succeeded',
      'createdAtUtc': '2026-12-01T10:00:00Z',
    });

    expect(payment.amount, 800);
    expect(payment.platformFee, 120);
    expect(payment.driverPayoutAmount, 680);
    expect(payment.provider, 'Mock');
    expect(payment.providerReference, 'MOCK-ABC123');
    expect(payment.status, 'Succeeded');
  });

  test('Payment.fromJson handles a null providerReference (Failed payments)', () {
    final payment = Payment.fromJson({
      'id': 'pay1',
      'bookingId': 'b1',
      'amount': 300,
      'platformFee': 45,
      'driverPayoutAmount': 255,
      'provider': 'Mock',
      'providerReference': null,
      'status': 'Failed',
      'createdAtUtc': '2026-12-01T10:00:00Z',
    });

    expect(payment.providerReference, isNull);
    expect(payment.status, 'Failed');
  });

  test('Payment.fromJson reads a redirectUrl for a pending Ozow payment', () {
    final payment = Payment.fromJson({
      'id': 'pay1',
      'bookingId': 'b1',
      'amount': 800,
      'platformFee': 120,
      'driverPayoutAmount': 680,
      'provider': 'Ozow',
      'providerReference': null,
      'status': 'Pending',
      'createdAtUtc': '2026-12-01T10:00:00Z',
      'redirectUrl': 'https://pay.ozow.com/abc123',
    });

    expect(payment.provider, 'Ozow');
    expect(payment.status, 'Pending');
    expect(payment.redirectUrl, 'https://pay.ozow.com/abc123');
  });
}
