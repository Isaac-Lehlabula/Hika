import { cookies } from "next/headers";
import { API_BASE_URL, ACCESS_TOKEN_COOKIE } from "./config";

/** Mirrors backend Hika.Application.Common.Pagination.PagedResult<T>. */
export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

export class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
    public fieldErrors?: Record<string, string[]>,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

/**
 * Server-only fetch against the Hika API, authenticated with the access-token cookie set at
 * login. Token refresh happens in proxy.ts (the only place in the request lifecycle allowed to
 * mutate cookies ahead of a Server Component render) — this function assumes a valid access
 * token is already in place and simply surfaces a 401 as an ApiError if it isn't, rather than
 * attempting a refresh itself.
 */
export async function adminFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const cookieStore = await cookies();
  const accessToken = cookieStore.get(ACCESS_TOKEN_COOKIE)?.value;

  if (!accessToken) {
    throw new ApiError(401, "Not authenticated");
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers: {
      ...init?.headers,
      Authorization: `Bearer ${accessToken}`,
      "Content-Type": "application/json",
    },
    cache: "no-store",
  });

  if (response.status === 204) {
    return undefined as T;
  }

  if (!response.ok) {
    const problem = await response.json().catch(() => null);
    throw new ApiError(
      response.status,
      problem?.title ?? problem?.detail ?? response.statusText,
      problem?.errors,
    );
  }

  return (await response.json()) as T;
}

export function buildQuery(params: Record<string, string | number | undefined | null>): string {
  const query = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== "") {
      query.set(key, String(value));
    }
  }
  const asString = query.toString();
  return asString ? `?${asString}` : "";
}
