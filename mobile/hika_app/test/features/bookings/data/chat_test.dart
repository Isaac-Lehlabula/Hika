import 'package:flutter_test/flutter_test.dart';
import 'package:hika_app/features/bookings/data/chat.dart';

void main() {
  test('Conversation.fromJson round-trips the backend ConversationResponse shape', () {
    final conversation = Conversation.fromJson({
      'id': 'c1',
      'bookingId': 'b1',
      'isOpen': true,
      'messages': [
        {
          'id': 'm1',
          'senderUserId': 'u1',
          'isMine': true,
          'body': 'Running 10 minutes late',
          'sentAtUtc': '2026-12-01T10:00:00Z',
        },
        {
          'id': 'm2',
          'senderUserId': 'u2',
          'isMine': false,
          'body': 'No worries, see you soon',
          'sentAtUtc': '2026-12-01T10:01:00Z',
        },
      ],
    });

    expect(conversation.id, 'c1');
    expect(conversation.bookingId, 'b1');
    expect(conversation.isOpen, isTrue);
    expect(conversation.messages, hasLength(2));
    expect(conversation.messages[0].body, 'Running 10 minutes late');
    expect(conversation.messages[0].isMine, isTrue);
    expect(conversation.messages[1].isMine, isFalse);
  });

  test('Conversation.fromJson handles an empty message list', () {
    final conversation = Conversation.fromJson({'id': 'c1', 'bookingId': 'b1', 'isOpen': false, 'messages': <dynamic>[]});

    expect(conversation.isOpen, isFalse);
    expect(conversation.messages, isEmpty);
  });
}
