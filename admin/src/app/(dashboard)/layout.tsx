import { requireAdminSession, logoutAction } from "@/lib/session";
import { Sidebar } from "@/components/Sidebar";

export default async function DashboardLayout({ children }: { children: React.ReactNode }) {
  const session = await requireAdminSession();

  if (!session.authorized) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-surface-alt px-4">
        <div className="max-w-sm rounded-2xl border border-border bg-surface p-8 text-center shadow-sm">
          <h1 className="text-lg font-semibold">Not authorized</h1>
          <p className="mt-2 text-sm text-text-secondary">
            Your account isn&apos;t staff-authorized for the Hiking Spot admin portal. Ask an existing admin to grant access.
          </p>
          <form action={logoutAction} className="mt-6">
            <button type="submit" className="rounded-lg border border-border px-4 py-2 text-sm hover:bg-surface-alt">
              Sign out
            </button>
          </form>
        </div>
      </div>
    );
  }

  return (
    <div className="flex min-h-screen">
      <Sidebar />
      <div className="flex flex-1 flex-col">
        <header className="flex items-center justify-between border-b border-border bg-surface px-6 py-3">
          <span className="text-sm text-text-secondary">
            Signed in as <span className="font-medium text-text-primary">{session.name}</span>
          </span>
          <form action={logoutAction}>
            <button type="submit" className="text-sm font-medium text-text-secondary hover:text-text-primary">
              Sign out
            </button>
          </form>
        </header>
        <main className="flex-1 overflow-x-auto px-6 py-6">{children}</main>
      </div>
    </div>
  );
}
