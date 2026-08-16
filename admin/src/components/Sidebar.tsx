"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

const NAV_ITEMS = [
  { href: "/", label: "Dashboard" },
  { href: "/users", label: "Users" },
  { href: "/verifications", label: "Verifications" },
  { href: "/trips", label: "Trips" },
  { href: "/bookings", label: "Bookings" },
  { href: "/payments", label: "Payments" },
  { href: "/reports", label: "Reports" },
  { href: "/reviews", label: "Reviews" },
  { href: "/audit-log", label: "Audit log" },
  { href: "/settings/platform-fee", label: "Platform fee" },
];

export function Sidebar() {
  const pathname = usePathname();

  return (
    <nav className="flex h-full w-56 shrink-0 flex-col gap-1 border-r border-border bg-surface px-3 py-4">
      <div className="mb-4 flex items-center gap-2 px-2">
        <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary text-sm font-bold text-white">H</div>
        <span className="text-sm font-semibold">Hiking Spot Admin</span>
      </div>
      {NAV_ITEMS.map((item) => {
        const isActive = item.href === "/" ? pathname === "/" : pathname.startsWith(item.href);
        return (
          <Link
            key={item.href}
            href={item.href}
            className={`rounded-lg px-3 py-2 text-sm font-medium transition-colors ${
              isActive ? "bg-accent-light text-accent" : "text-text-secondary hover:bg-surface-alt hover:text-text-primary"
            }`}
          >
            {item.label}
          </Link>
        );
      })}
    </nav>
  );
}
