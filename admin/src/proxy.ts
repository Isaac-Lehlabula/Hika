import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";
import { API_BASE_URL, ACCESS_TOKEN_COOKIE, REFRESH_TOKEN_COOKIE } from "@/lib/config";

type AuthTokenResponse = {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
};

function secondsUntil(isoDate: string): number {
  return Math.max(Math.floor((new Date(isoDate).getTime() - Date.now()) / 1000), 0);
}

/**
 * Silently refreshes an expired/missing access token before a protected page renders. This is
 * the only point in the request lifecycle where that can happen: Server Components can't set
 * cookies (see lib/api.ts's adminFetch), so a refresh attempted there would have nowhere to put
 * the new token. Proxy runs ahead of rendering and CAN set cookies on its outgoing response,
 * which is why token refresh lives here rather than in adminFetch itself.
 */
export async function proxy(request: NextRequest) {
  const accessToken = request.cookies.get(ACCESS_TOKEN_COOKIE)?.value;
  if (accessToken) {
    return NextResponse.next();
  }

  const refreshToken = request.cookies.get(REFRESH_TOKEN_COOKIE)?.value;
  if (!refreshToken) {
    return NextResponse.redirect(new URL("/login", request.url));
  }

  const refreshResponse = await fetch(`${API_BASE_URL}/api/v1/auth/refresh`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ refreshToken }),
    cache: "no-store",
  });

  if (!refreshResponse.ok) {
    const redirectResponse = NextResponse.redirect(new URL("/login", request.url));
    redirectResponse.cookies.delete(ACCESS_TOKEN_COOKIE);
    redirectResponse.cookies.delete(REFRESH_TOKEN_COOKIE);
    return redirectResponse;
  }

  const tokens = (await refreshResponse.json()) as AuthTokenResponse;
  const response = NextResponse.next();
  const cookieBase = { httpOnly: true, secure: process.env.NODE_ENV === "production", sameSite: "lax" as const, path: "/" };

  response.cookies.set(ACCESS_TOKEN_COOKIE, tokens.accessToken, {
    ...cookieBase,
    maxAge: secondsUntil(tokens.accessTokenExpiresAtUtc),
  });
  response.cookies.set(REFRESH_TOKEN_COOKIE, tokens.refreshToken, {
    ...cookieBase,
    maxAge: secondsUntil(tokens.refreshTokenExpiresAtUtc),
  });

  return response;
}

export const config = {
  matcher: [
    /*
     * Run on everything except the login page, Next internals, and static assets — those
     * don't need a session and shouldn't be blocked on a token refresh round-trip.
     */
    "/((?!login|_next/static|_next/image|favicon.ico).*)",
  ],
};
