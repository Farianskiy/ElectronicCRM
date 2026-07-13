"use client";

import Link from "next/link";
import { RequireAuth } from "@/features/auth/ui/RequireAuth";
import { AppShell } from "@/widgets/appShell/AppShell";
import { getAuthSession, isTechnicalUser } from "@/shared/api/authToken";

export default function HomePage() {
  const session = getAuthSession();
  const technical = isTechnicalUser(session);

  const cards = [
    {
      href: "/catalog/assistant",
      title: "Catalog Assistant",
      description: "Поиск товаров через естественный текстовый запрос.",
      visible: true,
    },
    {
      href: "/catalog/products",
      title: "Каталог товаров",
      description: "Просмотр товаров, цен, остатков и характеристик.",
      visible: true,
    },
    {
      href: "/catalog/assistant-suggestions",
      title: "Предложения словаря",
      description: "Модерация неизвестных слов и пользовательских исправлений.",
      visible: technical,
    },
  ];

  return (
    <RequireAuth>
      <AppShell
        title="Главная"
        description="Панель управления каталогом, поиском и пользовательскими предложениями."
      >
        <section className="grid gap-5 md:grid-cols-3">
          {cards
            .filter((card) => card.visible)
            .map((card) => (
              <Link
                key={card.href}
                href={card.href}
                className="group rounded-3xl border border-white/10 bg-white/[0.04] p-6 transition hover:-translate-y-1 hover:bg-white/[0.07]"
              >
                <div className="mb-5 flex h-12 w-12 items-center justify-center rounded-2xl bg-teal-500/20 text-xl font-bold text-teal-300 transition group-hover:bg-teal-500 group-hover:text-white">
                  →
                </div>

                <h2 className="text-xl font-semibold text-white">
                  {card.title}
                </h2>

                <p className="mt-3 text-sm leading-6 text-slate-400">
                  {card.description}
                </p>
              </Link>
            ))}
        </section>

        <section className="mt-6 rounded-3xl border border-white/10 bg-white/[0.04] p-6">
          <h2 className="text-xl font-semibold text-white">
            Текущий пользователь
          </h2>

          <div className="mt-4 grid gap-4 md:grid-cols-3">
            <InfoCard label="Имя" value={session?.displayName ?? "—"} />
            <InfoCard label="Роль" value={session?.userType ?? "—"} />
            <InfoCard
              label="Технический доступ"
              value={technical ? "Да" : "Нет"}
            />
          </div>
        </section>
      </AppShell>
    </RequireAuth>
  );
}

function InfoCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
      <p className="text-sm text-slate-400">{label}</p>
      <p className="mt-1 text-lg font-semibold text-white">{value}</p>
    </div>
  );
}