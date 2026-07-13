"use client";

import { useMemo, useSyncExternalStore } from "react";
import {
  getAuthSessionSnapshot,
  parseAuthSession,
  subscribeAuthSessionChanged,
  type AuthSession,
} from "@/shared/api/authToken";

function getServerSnapshot(): string | null {
  return null;
}

export function useAuthSession(): AuthSession | null {
  const rawSession = useSyncExternalStore(
    subscribeAuthSessionChanged,
    getAuthSessionSnapshot,
    getServerSnapshot,
  );

  return useMemo(() => parseAuthSession(rawSession), [rawSession]);
}