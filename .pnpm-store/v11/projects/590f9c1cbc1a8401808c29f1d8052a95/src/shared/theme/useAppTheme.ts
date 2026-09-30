"use client";

import { useEffect, useSyncExternalStore } from "react";

export type AppThemeMode = "dark" | "light";

const APP_THEME_STORAGE_KEY = "electronic_crm_theme_mode";
const APP_THEME_CHANGED_EVENT = "electronic_crm_theme_mode_changed";

function normalizeThemeMode(value: string | null): AppThemeMode {
  return value === "light" ? "light" : "dark";
}

function subscribeAppTheme(callback: () => void): () => void {
  if (typeof window === "undefined") {
    return () => {};
  }

  window.addEventListener("storage", callback);
  window.addEventListener(APP_THEME_CHANGED_EVENT, callback);

  return () => {
    window.removeEventListener("storage", callback);
    window.removeEventListener(APP_THEME_CHANGED_EVENT, callback);
  };
}

function getAppThemeSnapshot(): AppThemeMode {
  if (typeof window === "undefined") {
    return "dark";
  }

  return normalizeThemeMode(localStorage.getItem(APP_THEME_STORAGE_KEY));
}

function getServerSnapshot(): AppThemeMode {
  return "dark";
}

export function setAppThemeMode(mode: AppThemeMode): void {
  if (typeof window === "undefined") {
    return;
  }

  localStorage.setItem(APP_THEME_STORAGE_KEY, mode);
  window.dispatchEvent(new Event(APP_THEME_CHANGED_EVENT));
}

export function useAppTheme(): {
  mode: AppThemeMode;
  setMode: (mode: AppThemeMode) => void;
} {
  const mode = useSyncExternalStore(
    subscribeAppTheme,
    getAppThemeSnapshot,
    getServerSnapshot,
  );

  useEffect(() => {
    document.documentElement.dataset.theme = mode;
    document.documentElement.dataset.themeMode = mode;
    document.documentElement.style.colorScheme =
      mode === "light" ? "light" : "dark";
  }, [mode]);

  return {
    mode,
    setMode: setAppThemeMode,
  };
}