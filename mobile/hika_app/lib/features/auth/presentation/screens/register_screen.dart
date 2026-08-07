import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/networking/api_exception.dart';
import '../../../../core/theme/hika_spacing.dart';
import '../../../../shared/widgets/hika_button.dart';
import '../../../../shared/widgets/hika_text_field.dart';
import '../providers/auth_controller.dart';
import '../widgets/auth_scaffold.dart';

class RegisterScreen extends ConsumerStatefulWidget {
  const RegisterScreen({super.key});

  @override
  ConsumerState<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends ConsumerState<RegisterScreen> {
  final _firstNameController = TextEditingController();
  final _lastNameController = TextEditingController();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();

  bool _isSubmitting = false;
  String? _generalError;
  Map<String, List<String>>? _fieldErrors;

  @override
  void dispose() {
    _firstNameController.dispose();
    _lastNameController.dispose();
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    setState(() {
      _isSubmitting = true;
      _generalError = null;
      _fieldErrors = null;
    });

    try {
      final email = _emailController.text.trim();
      final userId = await ref
          .read(authControllerProvider.notifier)
          .register(
            email: email,
            password: _passwordController.text,
            firstName: _firstNameController.text.trim(),
            lastName: _lastNameController.text.trim(),
          );
      if (mounted) {
        context.push('/verify-email', extra: {'userId': userId, 'email': email});
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

  String? _errorFor(String field) => _fieldErrors?[field]?.firstOrNull;

  @override
  Widget build(BuildContext context) {
    return AuthScaffold(
      title: 'Create your account',
      subtitle: 'Join Hika and find your next hike home.',
      children: [
        Row(
          children: [
            Expanded(
              child: HikaTextField(
                label: 'First name',
                controller: _firstNameController,
                textInputAction: TextInputAction.next,
                autofillHints: const [AutofillHints.givenName],
                errorText: _errorFor('firstName'),
              ),
            ),
            const SizedBox(width: HikaSpacing.sm),
            Expanded(
              child: HikaTextField(
                label: 'Last name',
                controller: _lastNameController,
                textInputAction: TextInputAction.next,
                autofillHints: const [AutofillHints.familyName],
                errorText: _errorFor('lastName'),
              ),
            ),
          ],
        ),
        const SizedBox(height: HikaSpacing.md),
        HikaTextField(
          label: 'Email',
          controller: _emailController,
          keyboardType: TextInputType.emailAddress,
          textInputAction: TextInputAction.next,
          prefixIcon: Icons.mail_outline,
          autofillHints: const [AutofillHints.email],
          errorText: _errorFor('email') ?? _errorFor('Email'),
        ),
        const SizedBox(height: HikaSpacing.md),
        HikaTextField(
          label: 'Password',
          controller: _passwordController,
          obscureText: true,
          textInputAction: TextInputAction.done,
          prefixIcon: Icons.lock_outline,
          autofillHints: const [AutofillHints.newPassword],
          errorText: _errorFor('password') ?? _errorFor('Password'),
          onSubmitted: (_) => _submit(),
        ),
        const SizedBox(height: HikaSpacing.xs),
        Text(
          'At least 10 characters, with an uppercase letter, a lowercase letter, and a digit.',
          style: Theme.of(
            context,
          ).textTheme.bodySmall?.copyWith(color: Theme.of(context).colorScheme.onSurface.withValues(alpha: 0.55)),
        ),
        if (_generalError != null) ...[
          const SizedBox(height: HikaSpacing.md),
          Text(_generalError!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
        ],
        const SizedBox(height: HikaSpacing.xl),
        HikaButton(label: 'Create account', isLoading: _isSubmitting, onPressed: _submit),
        const SizedBox(height: HikaSpacing.md),
        Center(
          child: Wrap(
            alignment: WrapAlignment.center,
            crossAxisAlignment: WrapCrossAlignment.center,
            children: [
              const Text('Already have an account?'),
              HikaButton(variant: HikaButtonVariant.text, label: 'Log in', onPressed: () => context.pop()),
            ],
          ),
        ),
      ],
    );
  }
}
