/// Mirrors backend Hika.Application.Chat.Dtos.ChatMessageResponse.
class ChatMessageItem {
  const ChatMessageItem({required this.id, required this.senderUserId, required this.isMine, required this.body, required this.sentAtUtc});

  factory ChatMessageItem.fromJson(Map<String, dynamic> json) => ChatMessageItem(
    id: json['id'] as String,
    senderUserId: json['senderUserId'] as String,
    isMine: json['isMine'] as bool,
    body: json['body'] as String,
    sentAtUtc: DateTime.parse(json['sentAtUtc'] as String),
  );

  final String id;
  final String senderUserId;
  final bool isMine;
  final String body;
  final DateTime sentAtUtc;
}

/// Mirrors backend Hika.Application.Chat.Dtos.ConversationResponse.
class Conversation {
  const Conversation({required this.id, required this.bookingId, required this.isOpen, required this.messages});

  factory Conversation.fromJson(Map<String, dynamic> json) => Conversation(
    id: json['id'] as String,
    bookingId: json['bookingId'] as String,
    isOpen: json['isOpen'] as bool,
    messages: (json['messages'] as List<dynamic>).map((m) => ChatMessageItem.fromJson(m as Map<String, dynamic>)).toList(),
  );

  final String id;
  final String bookingId;
  final bool isOpen;
  final List<ChatMessageItem> messages;
}
