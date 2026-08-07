import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/networking/api_exception.dart';
import '../../../../core/theme/hika_spacing.dart';
import '../../../../shared/widgets/hika_button.dart';
import '../../../../shared/widgets/hika_text_field.dart';
import '../providers/auth_controller.dart';
import '../widgets/auth_scaffold.dart';

class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();

  bool _isSubmitting = false;
  String? _errorMessage;

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    setState(() {
      _isSubmitting = true;
      _errorMessage = null;
    });

    try {
      await ref
          .read(authControllerProvider.notifier)
          .login(email: _emailController.text.trim(), password: _passwordController.text);
      if (mounted) {
        context.go('/home');
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
      title: 'Welcome back',
      subtitle: 'Log in to find your next hike home.',
      showBackButton: false,
      children: [
        HikaTextField(
          label: 'Email',
          controller: _emailController,
          keyboardType: TextInputType.emailAddress,
          textInputAction: TextInputAction.next,
          prefixIcon: Icons.mail_outline,
          autofillHints: const [AutofillHints.email],
        ),
        const SizedBox(height: HikaSpacing.md),
        HikaTextField(
          label: 'Password',
          controller: _passwordController,
          obscureText: true,
          textInputAction: TextInputAction.done,
          prefixIcon: Icons.lock_outline,
          autofillHints: const [AutofillHints.password],
          onSubmitted: (_) => _submit(),
        ),
        Align(
          alignment: Alignment.centerRight,
          child: HikaButton(
            variant: HikaButtonVariant.text,
            label: 'Forgot password?',
            onPressed: () => context.push('/forgot-password'),
          ),
        ),
        if (_errorMessage != null) ...[
          Text(_errorMessage!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
          const SizedBox(height: HikaSpacing.md),
        ],
        const SizedBox(height: HikaSpacing.sm),
        HikaButton(label: 'Log in', isLoading: _isSubmitting, onPressed: _submit),
        const SizedBox(height: HikaSpacing.xl),
        Center(
          child: Wrap(
            alignment: WrapAlignment.center,
            crossAxisAlignment: WrapCrossAlignment.center,
            children: [
              const Text("Don't have an account?"),
              HikaButton(
                variant: HikaButtonVariant.text,
                label: 'Register',
                onPressed: () => context.push('/register'),
              ),
            ],
          ),
        ),
      ],
    );
  }
}
