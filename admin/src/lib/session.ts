"use server";

import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { API_BASE_URL, ACCESS_TOKEN_COOKIE, REFRESH_TOKEN_COOKIE } from "./config";
import { adminFetch, ApiError } from "./api";

type AuthTokenResponse = {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
};

type OwnProfileResponse = {
  userId: string;
  firstName: string;
  lastName: string;
  email: string;
};

function secondsUntil(isoDate: string): number {
  const seconds = Math.floor((new Date(isoDate).getTime() - Date.now()) / 1000);
  return Math.max(seconds, 0);
}

async function storeTokens(tokens: AuthTokenResponse) {
  const cookieStore = await cookies();
  const cookieBase = { httpOnly: true, secure: process.env.NODE_ENV === "production", sameSite: "lax" as const, path: "/" };

  cookieStore.set(ACCESS_TOKEN_COOKIE, tokens.accessToken, {
    ...cookieBase,
    maxAge: secondsUntil(tokens.accessTokenExpiresAtUtc),
  });
  cookieStore.set(REFRESH_TOKEN_COOKIE, tokens.refreshToken, {
    ...cookieBase,
    maxAge: secondsUntil(tokens.refreshTokenExpiresAtUtc),
  });
}

export type LoginState = { error?: string };

/** Server Action backing the login form. Does not itself check IsAdmin — a non-admin can
 * authenticate here, but every subsequent page load's requireAdmin() call rejects them with a
 * clear "not authorized" message rather than a mysterious login failure, since "wrong password"
 * and "not staff" are different problems the user should be told apart. */
export async function loginAction(_prevState: LoginState, formData: FormData): Promise<LoginState> {
  const email = String(formData.get("email") ?? "");
  const password = String(formData.get("password") ?? "");

  if (!email || !password) {
    return { error: "Enter your email and password." };
  }

  const response = await fetch(`${API_BASE_URL}/api/v1/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
    cache: "no-store",
  });

  if (!response.ok) {
    return { error: response.status === 401 ? "Incorrect email or password." : "Something went wrong. Try again." };
  }

  const tokens = (await response.json()) as AuthTokenResponse;
  await storeTokens(tokens);
  redirect("/");
}

export async function logoutAction() {
  const cookieStore = await cookies();
  const refreshToken = cookieStore.get(REFRESH_TOKEN_COOKIE)?.value;

  if (refreshToken) {
    await fetch(`${API_BASE_URL}/api/v1/auth/logout`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refreshToken }),
      cache: "no-store",
    }).catch(() => undefined);
  }

  cookieStore.delete(ACCESS_TOKEN_COOKIE);
  cookieStore.delete(REFRESH_TOKEN_COOKIE);
  redirect("/login");
}

/** Confirms the caller is authenticated *and* an admin by calling a real admin endpoint —
 * the only source of truth for IsAdmin, since it's a DB-checked policy, not a JWT claim (see
 * backend AdminAuthorizationHandler). Redirects to /login on 401 (no/expired session); returns
 * `{ authorized: false }` on 403 so the caller can render a clear "not staff" message instead of
 * bouncing an authenticated-but-unauthorized user back to a login screen they'd just pass again. */
export async function requireAdminSession(): Promise<
  { authorized: true; name: string } | { authorized: false }
> {
  try {
    const [, profile] = await Promise.all([
      adminFetch<unknown>("/api/v1/admin/audit-logs?pageSize=1"),
      adminFetch<OwnProfileResponse>("/api/v1/users/me"),
    ]);
    return { authorized: true, name: `${profile.firstName} ${profile.lastName}` };
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) {
      redirect("/login");
    }
    if (error instanceof ApiError && error.status === 403) {
      return { authorized: false };
    }
    throw error;
  }
}
