import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../../core/networking/api_exception.dart';
import '../../../../core/theme/hika_colors.dart';
import '../../../../core/theme/hika_spacing.dart';
import '../../../../shared/widgets/hika_button.dart';
import '../../data/chat.dart';
import '../providers/chat_controller.dart';

/// No push/real-time infra exists yet — this screen polls on a timer while it's open (on top
/// of the app-resume refresh BookingDetailScreen already does) to feel reasonably live without
/// introducing the app's first WebSocket connection. See ChatController's remarks.
class ChatScreen extends ConsumerStatefulWidget {
  const ChatScreen({required this.bookingId, required this.otherPartyName, super.key});

  final String bookingId;
  final String otherPartyName;

  @override
  ConsumerState<ChatScreen> createState() => _ChatScreenState();
}

class _ChatScreenState extends ConsumerState<ChatScreen> {
  final _messageController = TextEditingController();
  final _scrollController = ScrollController();
  Timer? _pollTimer;
  bool _isSending = false;

  @override
  void initState() {
    super.initState();
    _pollTimer = Timer.periodic(const Duration(seconds: 5), (_) {
      ref.read(chatControllerProvider(widget.bookingId).notifier).pollSilently();
    });
  }

  @override
  void dispose() {
    _pollTimer?.cancel();
    _messageController.dispose();
    _scrollController.dispose();
    super.dispose();
  }

  void _scrollToBottom() {
    if (!_scrollController.hasClients) {
      return;
    }
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (_scrollController.hasClients) {
        _scrollController.jumpTo(_scrollController.position.maxScrollExtent);
      }
    });
  }

  Future<void> _send() async {
    final text = _messageController.text.trim();
    if (text.isEmpty || _isSending) {
      return;
    }

    setState(() => _isSending = true);
    _messageController.clear();
    try {
      await ref.read(chatControllerProvider(widget.bookingId).notifier).sendMessage(text);
      _scrollToBottom();
    } on ApiException catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.message)));
      }
    } finally {
      if (mounted) {
        setState(() => _isSending = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final conversationAsync = ref.watch(chatControllerProvider(widget.bookingId));

    ref.listen(chatControllerProvider(widget.bookingId), (previous, next) {
      final previousCount = previous?.value?.messages.length ?? 0;
      final nextCount = next.value?.messages.length ?? 0;
      if (nextCount > previousCount) {
        _scrollToBottom();
      }
    });

    return Scaffold(
      appBar: AppBar(title: Text(widget.otherPartyName)),
      body: conversationAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => Center(
          child: HikaButton(
            label: 'Try again',
            variant: HikaButtonVariant.secondary,
            onPressed: () => ref.read(chatControllerProvider(widget.bookingId).notifier).refresh(),
          ),
        ),
        data: (conversation) {
          if (conversation == null) {
            return const Center(child: Text('This conversation isn\'t open yet.'));
          }

          return Column(
            children: [
              Expanded(
                child: conversation.messages.isEmpty
                    ? Center(
                        child: Text(
                          'Say hello — coordinate pickup details here.',
                          style: Theme.of(context).textTheme.bodyMedium?.copyWith(color: HikaColors.textSecondaryLight),
                        ),
                      )
                    : ListView.builder(
                        controller: _scrollController,
                        padding: const EdgeInsets.all(HikaSpacing.lg),
                        itemCount: conversation.messages.length,
                        itemBuilder: (context, index) => _MessageBubble(message: conversation.messages[index]),
                      ),
              ),
              if (conversation.isOpen)
                _Composer(controller: _messageController, isSending: _isSending, onSend: _send)
              else
                Container(
                  width: double.infinity,
                  padding: const EdgeInsets.all(HikaSpacing.md),
                  color: HikaColors.surfaceAltLight,
                  child: Text(
                    'This conversation is closed.',
                    textAlign: TextAlign.center,
                    style: Theme.of(context).textTheme.bodySmall?.copyWith(color: HikaColors.textSecondaryLight),
                  ),
                ),
            ],
          );
        },
      ),
    );
  }
}

class _MessageBubble extends StatelessWidget {
  const _MessageBubble({required this.message});

  final ChatMessageItem message;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Padding(
      padding: const EdgeInsets.only(bottom: HikaSpacing.sm),
      child: Row(
        mainAxisAlignment: message.isMine ? MainAxisAlignment.end : MainAxisAlignment.start,
        children: [
          Flexible(
            child: Column(
              crossAxisAlignment: message.isMine ? CrossAxisAlignment.end : CrossAxisAlignment.start,
              children: [
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: HikaSpacing.md, vertical: HikaSpacing.sm),
                  decoration: BoxDecoration(
                    color: message.isMine ? HikaColors.primary : HikaColors.surfaceAltLight,
                    borderRadius: BorderRadius.circular(16),
                  ),
                  child: Text(
                    message.body,
                    style: theme.textTheme.bodyMedium?.copyWith(color: message.isMine ? Colors.white : HikaColors.textPrimaryLight),
                  ),
                ),
                const SizedBox(height: HikaSpacing.xxs),
                Text(
                  DateFormat('HH:mm').format(message.sentAtUtc.toLocal()),
                  style: theme.textTheme.labelSmall?.copyWith(color: HikaColors.textSecondaryLight),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _Composer extends StatelessWidget {
  const _Composer({required this.controller, required this.isSending, required this.onSend});

  final TextEditingController controller;
  final bool isSending;
  final VoidCallback onSend;

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.all(HikaSpacing.sm),
        child: Row(
          children: [
            Expanded(
              child: TextField(
                controller: controller,
                minLines: 1,
                maxLines: 4,
                textInputAction: TextInputAction.send,
                onSubmitted: (_) => onSend(),
                decoration: const InputDecoration(hintText: 'Message', isDense: true),
              ),
            ),
            const SizedBox(width: HikaSpacing.sm),
            IconButton.filled(
              onPressed: isSending ? null : onSend,
              icon: isSending
                  ? const SizedBox(height: 18, width: 18, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                  : const Icon(Icons.send_rounded),
            ),
          ],
        ),
      ),
    );
  }
}
