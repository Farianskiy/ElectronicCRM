"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import type { ReactNode } from "react";
import { clearAuthSession, isTechnicalUser, } from "@/shared/api/authToken";
import { useAuthSession } from "@/features/auth/model/useAuthSession";

interface AppShellProps {
  title: string;
  description?: string;
  children: ReactNode;
}

const regularNavigation = [
  { href: "/", label: "Главная" },
  { href: "/catalog/assistant", label: "Assistant" },
  { href: "/catalog/products", label: "Каталог товаров" },
  { href: "/profile", label: "Профиль" },
];

const technicalNavigation = [
  { href: "/catalog/assistant-suggestions", label: "Предложения словаря" },
];

export function AppShell({ title, description, children }: AppShellProps) {
  const router = useRouter();
  const pathname = usePathname();
  const session = useAuthSession();
    const technical = isTechnicalUser(session);

  const navigation = technical
    ? [...regularNavigation, ...technicalNavigation]
    : regularNavigation;

  function handleLogout() {
    clearAuthSession();
    router.push("/login");
  }

  return (
    <div className="min-h-screen bg-[#0f1115] text-slate-100">
      <aside className="fixed inset-y-0 left-0 hidden w-72 border-r border-white/10 bg-[#111318] p-5 lg:block">
        <div className="rounded-2xl border border-white/10 bg-white/[0.03] p-4">
          <p className="text-sm font-semibold text-teal-300">
            Electronic CRM
          </p>
          <p className="mt-1 text-xs text-slate-400">
            Каталог и assistant
          </p>
        </div>

        <nav className="mt-6 grid gap-1">
          {navigation.map((item) => {
            const isActive = pathname === item.href;

            return (
              <Link
                key={item.href}
                href={item.href}
                className={
                  isActive
                    ? "rounded-xl bg-teal-500/20 px-4 py-3 text-sm font-medium text-teal-200"
                    : "rounded-xl px-4 py-3 text-sm font-medium text-slate-300 transition hover:bg-white/[0.06] hover:text-white"
                }
              >
                {item.label}
              </Link>
            );
          })}
        </nav>

        <div className="absolute bottom-5 left-5 right-5 rounded-2xl border border-white/10 bg-white/[0.03] p-4">
          <p className="text-sm font-semibold text-white">
            {session?.displayName ?? "Пользователь"}
          </p>
          <p className="mt-1 text-xs text-slate-400">
            {technical ? "Technical" : "Regular"}
          </p>
        </div>
      </aside>

      <div className="lg:pl-72">
        <header className="sticky top-0 z-20 border-b border-white/10 bg-[#111318]/90 px-6 py-4 backdrop-blur">
          <div className="flex items-center justify-between gap-4">
            <div>
              <h1 className="text-2xl font-bold text-white">{title}</h1>
              {description && (
                <p className="mt-1 text-sm text-slate-400">{description}</p>
              )}
            </div>

            <div className="flex items-center gap-3">
              <Link
                href="/profile"
                className="flex items-center gap-3 rounded-2xl border border-white/10 bg-white/[0.04] px-3 py-2 transition hover:bg-white/[0.08]"
              >
                <div className="flex h-9 w-9 items-center justify-center rounded-full bg-teal-500 text-sm font-bold text-white">
                  {session?.displayName?.[0]?.toUpperCase() ?? "U"}
                </div>

                <div className="hidden text-left sm:block">
                  <p className="text-sm font-semibold text-white">
                    {session?.displayName ?? "Пользователь"}
                  </p>
                  <p className="text-xs text-slate-400">
                    {session?.userType ?? "Unknown"}
                  </p>
                </div>
              </Link>

              <button
                type="button"
                onClick={handleLogout}
                className="rounded-xl bg-white/[0.06] px-4 py-2 text-sm font-medium text-slate-200 transition hover:bg-white/[0.12]"
              >
                Выйти
              </button>
            </div>
          </div>
        </header>

        <main className="mx-auto max-w-7xl p-6">{children}</main>
      </div>
    </div>
  );
}