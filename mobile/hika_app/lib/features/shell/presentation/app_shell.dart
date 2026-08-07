import 'package:flutter/material.dart';

import '../../../shared/widgets/hika_empty_state.dart';
import '../../home/presentation/screens/home_screen.dart';
import '../../profile/presentation/screens/profile_screen.dart';

/// Bottom-nav shell: Home, Trips, Bookings, Inbox, Profile — five tabs per
/// the product brief's "don't create too many tabs" guidance. Trips/
/// Bookings/Inbox are honest "coming soon" states (see docs/roadmap.md)
/// rather than placeholder screens pretending to be finished features.
class AppShell extends StatefulWidget {
  const AppShell({super.key});

  @override
  State<AppShell> createState() => _AppShellState();
}

class _AppShellState extends State<AppShell> {
  int _index = 0;

  static const _tabs = [
    _Tab(icon: Icons.home_outlined, selectedIcon: Icons.home_rounded, label: 'Home'),
    _Tab(icon: Icons.route_outlined, selectedIcon: Icons.route_rounded, label: 'Trips'),
    _Tab(icon: Icons.event_seat_outlined, selectedIcon: Icons.event_seat_rounded, label: 'Bookings'),
    _Tab(icon: Icons.mail_outline_rounded, selectedIcon: Icons.mail_rounded, label: 'Inbox'),
    _Tab(icon: Icons.person_outline_rounded, selectedIcon: Icons.person_rounded, label: 'Profile'),
  ];

  @override
  Widget build(BuildContext context) {
    final pages = [
      const HomeScreen(),
      const _ComingSoonTab(
        icon: Icons.route_outlined,
        title: 'Your trips',
        message: 'Trips you post or book will show up here once trip posting and search are live.',
      ),
      const _ComingSoonTab(
        icon: Icons.event_seat_outlined,
        title: 'Bookings',
        message: 'Seat reservations and requests will show up here once bookings are live.',
      ),
      const _ComingSoonTab(
        icon: Icons.mail_outline_rounded,
        title: 'Inbox',
        message: 'Booking updates and messages will show up here once notifications are live.',
      ),
      const ProfileScreen(),
    ];

    return Scaffold(
      body: IndexedStack(index: _index, children: pages),
      bottomNavigationBar: BottomNavigationBar(
        currentIndex: _index,
        onTap: (value) => setState(() => _index = value),
        items: [
          for (final tab in _tabs)
            BottomNavigationBarItem(
              icon: Icon(tab.icon),
              activeIcon: Icon(tab.selectedIcon),
              label: tab.label,
            ),
        ],
      ),
    );
  }
}

class _Tab {
  const _Tab({required this.icon, required this.selectedIcon, required this.label});

  final IconData icon;
  final IconData selectedIcon;
  final String label;
}

class _ComingSoonTab extends StatelessWidget {
  const _ComingSoonTab({required this.icon, required this.title, required this.message});

  final IconData icon;
  final String title;
  final String message;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(title)),
      body: HikaEmptyState(icon: icon, title: 'Coming soon', message: message),
    );
  }
}
