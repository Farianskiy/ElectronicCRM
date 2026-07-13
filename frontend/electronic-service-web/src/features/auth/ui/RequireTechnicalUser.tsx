"use client";

import Link from "next/link";
import type { ReactNode } from "react";
import { useAuthSession } from "@/features/auth/model/useAuthSession";
import { isTechnicalUser } from "@/shared/api/authToken";

interface RequireTechnicalUserProps {
  children: ReactNode;
}

export function RequireTechnicalUser({ children }: RequireTechnicalUserProps) {
  const session = useAuthSession();

  if (!isTechnicalUser(session)) {
    return (
      <section className="rounded-2xl border border-amber-500/30 bg-amber-500/10 p-6 text-amber-100">
        <h2 className="text-xl font-semibold">Недостаточно прав</h2>

        <p className="mt-2 text-sm text-amber-200">
          Этот раздел доступен только техническому пользователю.
        </p>

        <Link
          href="/"
          className="mt-4 inline-flex rounded-xl bg-amber-500 px-4 py-2 text-sm font-medium text-slate-950"
        >
          На главную
        </Link>
      </section>
    );
  }

  return children;
}