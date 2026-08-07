import 'package:flutter/material.dart';

import '../../bookings/presentation/screens/my_bookings_screen.dart';
import '../../home/presentation/screens/home_screen.dart';
import '../../notifications/presentation/screens/inbox_screen.dart';
import '../../profile/presentation/screens/profile_screen.dart';
import '../../trips/presentation/screens/my_trips_screen.dart';

/// Bottom-nav shell: Home, Trips, Bookings, Inbox, Profile — five tabs per
/// the product brief's "don't create too many tabs" guidance. All five are
/// now real, backend-wired screens.
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
      const MyTripsScreen(),
      const MyBookingsScreen(),
      const InboxScreen(),
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
