import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/networking/api_exception.dart';
import '../../../../core/providers.dart';
import '../../../../core/theme/hika_spacing.dart';
import '../../../../shared/widgets/hika_button.dart';
import '../../../../shared/widgets/hika_text_field.dart';
import '../widgets/auth_scaffold.dart';

class VerifyPhoneScreen extends ConsumerStatefulWidget {
  const VerifyPhoneScreen({super.key});

  @override
  ConsumerState<VerifyPhoneScreen> createState() => _VerifyPhoneScreenState();
}

class _VerifyPhoneScreenState extends ConsumerState<VerifyPhoneScreen> {
  final _phoneController = TextEditingController(text: '+27');
  final _codeController = TextEditingController();

  bool _codeSent = false;
  bool _isSubmitting = false;
  String? _errorMessage;

  @override
  void dispose() {
    _phoneController.dispose();
    _codeController.dispose();
    super.dispose();
  }

  Future<void> _sendCode() async {
    setState(() {
      _isSubmitting = true;
      _errorMessage = null;
    });

    try {
      await ref.read(authApiProvider).requestPhoneOtp(phoneNumber: _phoneController.text.trim());
      setState(() => _codeSent = true);
    } on ApiException catch (e) {
      setState(() => _errorMessage = e.message);
    } finally {
      if (mounted) {
        setState(() => _isSubmitting = false);
      }
    }
  }

  Future<void> _verifyCode() async {
    setState(() {
      _isSubmitting = true;
      _errorMessage = null;
    });

    try {
      await ref.read(authApiProvider).verifyPhone(code: _codeController.text.trim());
      if (mounted) {
        context.pop(true);
      }
    } on ApiException catch (e) {
      setState(() => _errorMessage = e.message);
    } finally {
      if (mounted) {
        setState(() => _isSubmitting = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return AuthScaffold(
      title: 'Verify your phone',
      subtitle: _codeSent
          ? 'Enter the 6-digit code we sent to ${_phoneController.text}.'
          : 'A verified number helps drivers and passengers trust each other.',
      children: [
        if (!_codeSent) ...[
          HikaTextField(
            label: 'Phone number',
            controller: _phoneController,
            keyboardType: TextInputType.phone,
            prefixIcon: Icons.phone_outlined,
            hintText: '+27821234567',
          ),
          if (_errorMessage != null) ...[
            const SizedBox(height: HikaSpacing.md),
            Text(_errorMessage!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
          ],
          const SizedBox(height: HikaSpacing.xl),
          HikaButton(label: 'Send code', isLoading: _isSubmitting, onPressed: _sendCode),
        ] else ...[
          HikaTextField(
            label: '6-digit code',
            controller: _codeController,
            keyboardType: TextInputType.number,
            onSubmitted: (_) => _verifyCode(),
          ),
          if (_errorMessage != null) ...[
            const SizedBox(height: HikaSpacing.md),
            Text(_errorMessage!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
          ],
          const SizedBox(height: HikaSpacing.xl),
          HikaButton(label: 'Verify', isLoading: _isSubmitting, onPressed: _verifyCode),
          const SizedBox(height: HikaSpacing.sm),
          HikaButton(
            variant: HikaButtonVariant.text,
            label: 'Use a different number',
            onPressed: () => setState(() => _codeSent = false),
          ),
        ],
      ],
    );
  }
}
