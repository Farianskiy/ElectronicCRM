"use client";

import { RequireAuth } from "@/features/auth/ui/RequireAuth";
import { AppShell } from "@/widgets/appShell/AppShell";
import { getAuthSession, isTechnicalUser } from "@/shared/api/authToken";

export default function ProfilePage() {
  const session = getAuthSession();
  const technical = isTechnicalUser(session);

  return (
    <RequireAuth>
      <AppShell
        title="Профиль"
        description="Информация о текущем пользователе и его правах доступа."
      >
        <section className="grid gap-6 lg:grid-cols-[360px_1fr]">
          <div className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
            <div className="flex h-24 w-24 items-center justify-center rounded-3xl bg-teal-500 text-4xl font-bold text-white">
              {session?.displayName?.[0]?.toUpperCase() ?? "U"}
            </div>

            <h2 className="mt-5 text-2xl font-bold text-white">
              {session?.displayName ?? "Пользователь"}
            </h2>

            <p className="mt-2 text-sm text-slate-400">
              ID: {session?.userId ?? "—"}
            </p>

            <div className="mt-5 inline-flex rounded-full bg-teal-500/20 px-4 py-2 text-sm font-medium text-teal-200">
              {session?.userType ?? "Unknown"}
            </div>
          </div>

          <div className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
            <h2 className="text-xl font-semibold text-white">
              Права доступа
            </h2>

            <div className="mt-5 grid gap-3">
              <AccessRow
                label="Поиск товаров через Assistant"
                allowed={Boolean(session)}
              />
              <AccessRow
                label="Просмотр каталога товаров"
                allowed={Boolean(session)}
              />
              <AccessRow
                label="Отправка предложений по неизвестным словам"
                allowed={Boolean(session)}
              />
              <AccessRow
                label="Просмотр Parsed request"
                allowed={technical}
              />
              <AccessRow
                label="Модерация предложений словаря"
                allowed={technical}
              />
              <AccessRow
                label="Изменение цен, остатков и характеристик"
                allowed={technical}
              />
            </div>
          </div>
        </section>
      </AppShell>
    </RequireAuth>
  );
}

function AccessRow({
  label,
  allowed,
}: {
  label: string;
  allowed: boolean;
}) {
  return (
    <div className="flex items-center justify-between rounded-2xl border border-white/10 bg-black/20 px-4 py-3">
      <span className="text-sm text-slate-200">{label}</span>

      <span
        className={
          allowed
            ? "rounded-full bg-green-500/20 px-3 py-1 text-xs font-medium text-green-300"
            : "rounded-full bg-red-500/20 px-3 py-1 text-xs font-medium text-red-300"
        }
      >
        {allowed ? "Разрешено" : "Нет доступа"}
      </span>
    </div>
  );
}