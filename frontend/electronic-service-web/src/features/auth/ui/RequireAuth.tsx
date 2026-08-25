"use client";

import { useRouter } from "next/navigation";
import type { ReactNode } from "react";
import { useEffect, useSyncExternalStore } from "react";
import { useAuthSession } from "../model/useAuthSession";

interface RequireAuthProps {
  children: ReactNode;
}

function subscribeClientReadiness(): () => void {
  return () => {};
}

function getClientReadinessSnapshot(): boolean {
  return true;
}

function getServerReadinessSnapshot(): boolean {
  return false;
}

export function RequireAuth({ children }: RequireAuthProps) {
  const router = useRouter();
  const session = useAuthSession();

  const isClientReady = useSyncExternalStore(
    subscribeClientReadiness,
    getClientReadinessSnapshot,
    getServerReadinessSnapshot,
  );

  useEffect(() => {
    if (isClientReady && session === null) {
      router.replace("/login");
    }
  }, [isClientReady, router, session]);

  if (!isClientReady || session === null) {
    return (
      <main className="flex min-h-screen items-center justify-center bg-[#0f1115] text-slate-100">
        <div className="rounded-2xl border border-white/10 bg-white/[0.04] px-6 py-4">
          Проверяем авторизацию...
        </div>
      </main>
    );
  }

  return children;
}
