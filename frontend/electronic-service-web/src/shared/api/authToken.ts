const ACCESS_TOKEN_KEY = "electronic_service_access_token";
const AUTH_SESSION_KEY = "electronic_service_auth_session";
const AUTH_SESSION_CHANGED_EVENT = "electronic_service_auth_session_changed";

export type UserType = "Regular" | "Technical" | string;

export interface AuthSession {
  accessToken: string;
  userId: string;
  userType: UserType;
  displayName: string;
}

function notifyAuthSessionChanged(): void {
  if (typeof window === "undefined") {
    return;
  }

  window.dispatchEvent(new Event(AUTH_SESSION_CHANGED_EVENT));
}

export function subscribeAuthSessionChanged(
  callback: () => void,
): () => void {
  if (typeof window === "undefined") {
    return () => {};
  }

  window.addEventListener("storage", callback);
  window.addEventListener(AUTH_SESSION_CHANGED_EVENT, callback);

  return () => {
    window.removeEventListener("storage", callback);
    window.removeEventListener(AUTH_SESSION_CHANGED_EVENT, callback);
  };
}

export function getAccessToken(): string | null {
  if (typeof window === "undefined") {
    return null;
  }

  return localStorage.getItem(ACCESS_TOKEN_KEY);
}

export function getAuthSessionSnapshot(): string | null {
  if (typeof window === "undefined") {
    return null;
  }

  return localStorage.getItem(AUTH_SESSION_KEY);
}

export function parseAuthSession(rawSession: string | null): AuthSession | null {
  if (!rawSession) {
    return null;
  }

  try {
    return JSON.parse(rawSession) as AuthSession;
  } catch {
    return null;
  }
}

export function getAuthSession(): AuthSession | null {
  return parseAuthSession(getAuthSessionSnapshot());
}

export function setAuthSession(session: AuthSession): void {
  localStorage.setItem(AUTH_SESSION_KEY, JSON.stringify(session));
  localStorage.setItem(ACCESS_TOKEN_KEY, session.accessToken);

  notifyAuthSessionChanged();
}

export function clearAuthSession(): void {
  if (typeof window === "undefined") {
    return;
  }

  localStorage.removeItem(AUTH_SESSION_KEY);
  localStorage.removeItem(ACCESS_TOKEN_KEY);

  notifyAuthSessionChanged();
}

export function isTechnicalUser(session: AuthSession | null): boolean {
  return session?.userType === "Technical";
}

export function isRegularUser(session: AuthSession | null): boolean {
  return session?.userType === "Regular";
}