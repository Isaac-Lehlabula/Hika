import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/networking/api_exception.dart';
import '../../../../core/providers.dart';
import '../../../../core/theme/hika_spacing.dart';
import '../../../../shared/widgets/hika_button.dart';
import '../../../../shared/widgets/hika_text_field.dart';
import '../widgets/auth_scaffold.dart';

class ResetPasswordScreen extends ConsumerStatefulWidget {
  const ResetPasswordScreen({super.key});

  @override
  ConsumerState<ResetPasswordScreen> createState() => _ResetPasswordScreenState();
}

class _ResetPasswordScreenState extends ConsumerState<ResetPasswordScreen> {
  final _userIdController = TextEditingController();
  final _tokenController = TextEditingController();
  final _newPasswordController = TextEditingController();

  bool _isSubmitting = false;
  String? _generalError;
  Map<String, List<String>>? _fieldErrors;

  @override
  void dispose() {
    _userIdController.dispose();
    _tokenController.dispose();
    _newPasswordController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    setState(() {
      _isSubmitting = true;
      _generalError = null;
      _fieldErrors = null;
    });

    try {
      await ref
          .read(authApiProvider)
          .resetPassword(
            userId: _userIdController.text.trim(),
            token: _tokenController.text.trim(),
            newPassword: _newPasswordController.text,
          );
      if (mounted) {
        context.go('/login');
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(const SnackBar(content: Text('Password reset — log in with your new password.')));
      }
    } on ApiException catch (e) {
      setState(() {
        _fieldErrors = e.fieldErrors;
        if (!e.isValidation) {
          _generalError = e.message;
        }
      });
    } finally {
      if (mounted) {
        setState(() => _isSubmitting = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return AuthScaffold(
      title: 'Enter your reset code',
      subtitle: 'Paste the details from the link we emailed you.',
      children: [
        HikaTextField(label: 'User ID', controller: _userIdController, hintText: 'From the reset link'),
        const SizedBox(height: HikaSpacing.md),
        HikaTextField(label: 'Reset code', controller: _tokenController, hintText: 'From the reset link'),
        const SizedBox(height: HikaSpacing.md),
        HikaTextField(
          label: 'New password',
          controller: _newPasswordController,
          obscureText: true,
          autofillHints: const [AutofillHints.newPassword],
          errorText: _fieldErrors?['newPassword']?.firstOrNull,
          onSubmitted: (_) => _submit(),
        ),
        if (_generalError != null) ...[
          const SizedBox(height: HikaSpacing.md),
          Text(_generalError!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
        ],
        const SizedBox(height: HikaSpacing.xl),
        HikaButton(label: 'Reset password', isLoading: _isSubmitting, onPressed: _submit),
      ],
    );
  }
}
