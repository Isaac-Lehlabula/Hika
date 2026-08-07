import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/theme/hika_colors.dart';
import '../../../../core/theme/hika_spacing.dart';
import '../../../../shared/widgets/hika_button.dart';
import '../../../../shared/widgets/hika_card.dart';
import '../../../../shared/widgets/hika_empty_state.dart';
import '../../data/emergency_contact.dart';
import '../providers/emergency_contacts_controller.dart';
import '../widgets/emergency_contact_sheet.dart';

class EmergencyContactsScreen extends ConsumerWidget {
  const EmergencyContactsScreen({super.key});

  Future<void> _delete(BuildContext context, WidgetRef ref, EmergencyContact contact) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Remove this contact?'),
        content: Text('${contact.name} will no longer be listed as an emergency contact.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Keep')),
          TextButton(onPressed: () => Navigator.pop(context, true), child: const Text('Remove')),
        ],
      ),
    );
    if (confirmed == true) {
      await ref.read(emergencyContactsControllerProvider.notifier).delete(contact.id);
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final contactsAsync = ref.watch(emergencyContactsControllerProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Emergency contacts')),
      floatingActionButton: FloatingActionButton(
        onPressed: () => EmergencyContactSheet.show(context),
        child: const Icon(Icons.add),
      ),
      body: contactsAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => Center(
          child: HikaButton(
            label: 'Try again',
            variant: HikaButtonVariant.secondary,
            onPressed: () => ref.read(emergencyContactsControllerProvider.notifier).refresh(),
          ),
        ),
        data: (contacts) {
          if (contacts.isEmpty) {
            return HikaEmptyState(
              icon: Icons.contact_phone_outlined,
              title: 'No emergency contacts',
              message: "Add someone we can share your trip status with if you ever need help.",
              action: HikaButton(label: 'Add contact', onPressed: () => EmergencyContactSheet.show(context)),
            );
          }

          return RefreshIndicator(
            onRefresh: () => ref.read(emergencyContactsControllerProvider.notifier).refresh(),
            child: ListView.separated(
              padding: const EdgeInsets.all(HikaSpacing.lg),
              itemCount: contacts.length,
              separatorBuilder: (_, _) => const SizedBox(height: HikaSpacing.md),
              itemBuilder: (context, index) {
                final contact = contacts[index];
                return HikaCard(
                  onTap: () => EmergencyContactSheet.show(context, existing: contact),
                  child: Row(
                    children: [
                      const CircleAvatar(
                        radius: 20,
                        backgroundColor: HikaColors.accentLight,
                        child: Icon(Icons.person_outline, color: HikaColors.accent),
                      ),
                      const SizedBox(width: HikaSpacing.md),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(contact.name, style: Theme.of(context).textTheme.titleMedium),
                            Text(
                              [contact.phoneNumber, if (contact.relationship != null) contact.relationship!].join(' · '),
                              style: Theme.of(context).textTheme.bodySmall,
                            ),
                          ],
                        ),
                      ),
                      IconButton(
                        icon: const Icon(Icons.delete_outline, color: HikaColors.danger),
                        onPressed: () => _delete(context, ref, contact),
                      ),
                    ],
                  ),
                );
              },
            ),
          );
        },
      ),
    );
  }
}
