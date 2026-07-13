"use client";

import { useRouter } from "next/navigation";
import type { ReactNode } from "react";
import { useEffect } from "react";
import { useAuthSession } from "@/features/auth/model/useAuthSession";

interface RequireAuthProps {
  children: ReactNode;
}

export function RequireAuth({ children }: RequireAuthProps) {
  const router = useRouter();
  const session = useAuthSession();

  useEffect(() => {
    if (session === null) {
      router.replace("/login");
    }
  }, [router, session]);

  if (session === null) {
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