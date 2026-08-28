import '../../../core/networking/api_client.dart';
import 'chat.dart';

/// Mirrors backend/src/Hika.Api/Controllers/ChatController.cs 1:1.
class ChatApi {
  ChatApi(this._client);

  final ApiClient _client;

  Future<Conversation> getConversation(String bookingId) async {
    final body = await _client.get('/api/v1/bookings/$bookingId/conversation');
    return Conversation.fromJson(body!);
  }

  Future<ChatMessageItem> sendMessage(String bookingId, String message) async {
    final body = await _client.post('/api/v1/bookings/$bookingId/conversation/messages', data: {'message': message});
    return ChatMessageItem.fromJson(body!);
  }
}
