import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/networking/api_exception.dart';
import '../../../../core/providers.dart';
import '../../../../core/theme/hika_colors.dart';
import '../../../../core/theme/hika_spacing.dart';
import '../../../../shared/widgets/hika_button.dart';
import '../../../../shared/widgets/hika_text_field.dart';
import '../widgets/auth_scaffold.dart';

class ForgotPasswordScreen extends ConsumerStatefulWidget {
  const ForgotPasswordScreen({super.key});

  @override
  ConsumerState<ForgotPasswordScreen> createState() => _ForgotPasswordScreenState();
}

class _ForgotPasswordScreenState extends ConsumerState<ForgotPasswordScreen> {
  final _emailController = TextEditingController();

  bool _isSubmitting = false;
  bool _sent = false;
  String? _errorMessage;

  @override
  void dispose() {
    _emailController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    setState(() {
      _isSubmitting = true;
      _errorMessage = null;
    });

    try {
      await ref.read(authApiProvider).forgotPassword(email: _emailController.text.trim());
      setState(() => _sent = true);
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
      title: 'Reset your password',
      subtitle: "Enter your email and we'll send you a reset link.",
      children: [
        if (_sent) ...[
          Container(
            padding: const EdgeInsets.all(HikaSpacing.md),
            decoration: BoxDecoration(color: HikaColors.successLight, borderRadius: BorderRadius.circular(HikaRadius.md)),
            child: const Row(
              children: [
                Icon(Icons.check_circle_outline, color: HikaColors.success),
                SizedBox(width: HikaSpacing.sm),
                Expanded(child: Text('If that email is registered, a reset link is on its way.')),
              ],
            ),
          ),
          const SizedBox(height: HikaSpacing.xl),
          HikaButton(
            label: 'I have the code',
            onPressed: () => context.push('/reset-password'),
          ),
        ] else ...[
          HikaTextField(
            label: 'Email',
            controller: _emailController,
            keyboardType: TextInputType.emailAddress,
            prefixIcon: Icons.mail_outline,
            autofillHints: const [AutofillHints.email],
            onSubmitted: (_) => _submit(),
          ),
          if (_errorMessage != null) ...[
            const SizedBox(height: HikaSpacing.md),
            Text(_errorMessage!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
          ],
          const SizedBox(height: HikaSpacing.xl),
          HikaButton(label: 'Send reset link', isLoading: _isSubmitting, onPressed: _submit),
        ],
      ],
    );
  }
}
