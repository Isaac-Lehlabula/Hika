import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/networking/api_exception.dart';
import '../../../../core/providers.dart';
import '../../../../core/theme/hika_colors.dart';
import '../../../../core/theme/hika_spacing.dart' show HikaSpacing, HikaRadius;
import '../../../../shared/widgets/hika_button.dart';
import '../../../../shared/widgets/hika_text_field.dart';
import '../widgets/auth_scaffold.dart';

class VerifyEmailScreen extends ConsumerStatefulWidget {
  const VerifyEmailScreen({required this.userId, required this.email, super.key});

  final String userId;
  final String email;

  @override
  ConsumerState<VerifyEmailScreen> createState() => _VerifyEmailScreenState();
}

class _VerifyEmailScreenState extends ConsumerState<VerifyEmailScreen> {
  final _tokenController = TextEditingController();

  bool _isSubmitting = false;
  bool _isResending = false;
  String? _errorMessage;
  String? _infoMessage;

  @override
  void dispose() {
    _tokenController.dispose();
    super.dispose();
  }

  Future<void> _verify() async {
    setState(() {
      _isSubmitting = true;
      _errorMessage = null;
      _infoMessage = null;
    });

    try {
      await ref.read(authApiProvider).verifyEmail(userId: widget.userId, token: _tokenController.text.trim());
      if (mounted) {
        context.go('/login');
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(const SnackBar(content: Text('Email verified — you can log in now.')));
      }
    } on ApiException catch (e) {
      setState(() => _errorMessage = e.message);
    } finally {
      if (mounted) {
        setState(() => _isSubmitting = false);
      }
    }
  }

  Future<void> _resend() async {
    setState(() {
      _isResending = true;
      _errorMessage = null;
      _infoMessage = null;
    });

    try {
      await ref.read(authApiProvider).resendVerificationEmail(email: widget.email);
      setState(() => _infoMessage = "If that address needs verifying, we've sent a new link.");
    } on ApiException catch (e) {
      setState(() => _errorMessage = e.message);
    } finally {
      if (mounted) {
        setState(() => _isResending = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return AuthScaffold(
      title: 'Check your inbox',
      subtitle: "We've sent a verification link to ${widget.email}.",
      children: [
        Container(
          padding: const EdgeInsets.all(HikaSpacing.md),
          decoration: BoxDecoration(color: HikaColors.accentLight, borderRadius: BorderRadius.circular(HikaRadius.md)),
          child: Row(
            children: [
              const Icon(Icons.info_outline, color: HikaColors.accent),
              const SizedBox(width: HikaSpacing.sm),
              const Expanded(
                child: Text(
                  'Tap the link in the email to verify. If it opens on another device, paste the code below instead.',
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: HikaSpacing.xl),
        HikaTextField(label: 'Verification code', controller: _tokenController, hintText: 'Paste the link\'s code'),
        if (_errorMessage != null) ...[
          const SizedBox(height: HikaSpacing.md),
          Text(_errorMessage!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
        ],
        if (_infoMessage != null) ...[
          const SizedBox(height: HikaSpacing.md),
          Text(_infoMessage!, style: TextStyle(color: HikaColors.accent)),
        ],
        const SizedBox(height: HikaSpacing.xl),
        HikaButton(label: 'Verify', isLoading: _isSubmitting, onPressed: _verify),
        const SizedBox(height: HikaSpacing.sm),
        HikaButton(
          variant: HikaButtonVariant.text,
          label: "Didn't get it? Resend email",
          isLoading: _isResending,
          onPressed: _resend,
        ),
      ],
    );
  }
}
