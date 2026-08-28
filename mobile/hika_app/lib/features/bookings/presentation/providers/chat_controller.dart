import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/networking/api_exception.dart';
import '../../../../core/providers.dart';
import '../../data/chat.dart';

/// Null state means "no conversation yet" (the booking hasn't been accepted) — same pattern as
/// PaymentController. No push/real-time infra exists yet (see ChatService's remarks
/// backend-side), so ChatScreen drives freshness itself: a manual [refresh] for pull-to-refresh,
/// and periodic [pollSilently] calls while the screen is open.
class ChatController extends AsyncNotifier<Conversation?> {
  ChatController(this.bookingId);

  final String bookingId;

  @override
  Future<Conversation?> build() => _load();

  Future<Conversation?> _load() async {
    try {
      return await ref.read(chatApiProvider).getConversation(bookingId);
    } on ApiException catch (e) {
      if (e.statusCode == 404) {
        return null;
      }
      rethrow;
    }
  }

  Future<void> refresh() async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(_load);
  }

  /// Updates the message list without flashing a loading spinner over it. Failures are
  /// swallowed — a background poll failing shouldn't disturb what's already on screen; the
  /// next successful poll (or a manual pull-to-refresh) recovers.
  Future<void> pollSilently() async {
    try {
      final conversation = await _load();
      state = AsyncData(conversation);
    } catch (_) {
      // Intentionally ignored — see doc comment above.
    }
  }

  Future<void> sendMessage(String message) async {
    await ref.read(chatApiProvider).sendMessage(bookingId, message);
    await refresh();
  }
}

final chatControllerProvider = AsyncNotifierProvider.family<ChatController, Conversation?, String>(ChatController.new);
