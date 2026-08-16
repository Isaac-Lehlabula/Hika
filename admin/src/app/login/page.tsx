import { LoginForm } from "./LoginForm";

export default function LoginPage() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-surface-alt px-4">
      <div className="w-full max-w-sm rounded-2xl border border-border bg-surface p-8 shadow-sm">
        <div className="mb-6 text-center">
          <div className="mx-auto mb-3 flex h-10 w-10 items-center justify-center rounded-xl bg-primary text-lg font-bold text-white">
            H
          </div>
          <h1 className="text-lg font-semibold">Hiking Spot Admin</h1>
          <p className="mt-1 text-sm text-text-secondary">Staff sign-in</p>
        </div>
        <LoginForm />
      </div>
    </div>
  );
}
