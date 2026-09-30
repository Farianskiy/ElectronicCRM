"use client";

import { useSyncExternalStore } from "react";

export type AppWorkspaceMode = "catalog" | "quality";

const APP_WORKSPACE_STORAGE_KEY = "electronic_crm_workspace_mode";
const APP_WORKSPACE_CHANGED_EVENT =
  "electronic_crm_workspace_mode_changed";

function normalizeWorkspaceMode(
  value: string | null,
): AppWorkspaceMode {
  return value === "quality" ? "quality" : "catalog";
}

function subscribeAppWorkspace(
  callback: () => void,
): () => void {
  if (typeof window === "undefined") {
    return () => {};
  }

  window.addEventListener("storage", callback);
  window.addEventListener(APP_WORKSPACE_CHANGED_EVENT, callback);

  return () => {
    window.removeEventListener("storage", callback);
    window.removeEventListener(
      APP_WORKSPACE_CHANGED_EVENT,
      callback,
    );
  };
}

function getAppWorkspaceSnapshot(): AppWorkspaceMode {
  if (typeof window === "undefined") {
    return "catalog";
  }

  return normalizeWorkspaceMode(
    localStorage.getItem(APP_WORKSPACE_STORAGE_KEY),
  );
}

function getServerSnapshot(): AppWorkspaceMode {
  return "catalog";
}

export function setAppWorkspaceMode(
  mode: AppWorkspaceMode,
): void {
  if (typeof window === "undefined") {
    return;
  }

  localStorage.setItem(APP_WORKSPACE_STORAGE_KEY, mode);
  window.dispatchEvent(new Event(APP_WORKSPACE_CHANGED_EVENT));
}

export function useAppWorkspaceMode(): {
  mode: AppWorkspaceMode;
  setMode: (mode: AppWorkspaceMode) => void;
} {
  const mode = useSyncExternalStore(
    subscribeAppWorkspace,
    getAppWorkspaceSnapshot,
    getServerSnapshot,
  );

  return {
    mode,
    setMode: setAppWorkspaceMode,
  };
}