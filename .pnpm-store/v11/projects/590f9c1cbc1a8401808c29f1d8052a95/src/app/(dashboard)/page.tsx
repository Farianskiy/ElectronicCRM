"use client";

import Link from "next/link";
import { useAuthSession } from "@/features/auth/model/useAuthSession";
import { useCurrentUserAccess } from "@/features/auth/model/CurrentUserAccessContext";
import type { UserPermissionCode } from "@/features/auth/model/userPermissions";
import { getUserTypeLabel } from "@/shared/api/authToken";
import { PageWorkspace } from "@/shared/ui/PageWorkspace";

interface HomeAction {
  href: string;
  title: string;
  description: string;
  eyebrow: string;
  permission: UserPermissionCode;
}

const homeActions: HomeAction[] = [
  {
    href: "/catalog/assistant",
    title: "Найти товар",
    description:
      "Используйте обычный текстовый запрос для поиска по каталогу, производителю и характеристикам.",
    eyebrow: "Ассистент",
    permission: "AssistantUse",
  },
  {
    href: "/catalog/products",
    title: "Открыть каталог",
    description:
      "Просматривайте товары, цены, остатки, производителей и технические характеристики.",
    eyebrow: "Каталог",
    permission: "ProductsView",
  },
  {
    href: "/catalog/imports",
    title: "Импортировать Excel",
    description:
      "Загрузите файл поставщика и проверьте распознавание наименований перед применением.",
    eyebrow: "Импорт",
    permission: "CatalogImportsCreate",
  },
  {
    href: "/catalog/recognition",
    title: "Проверить качество данных",
    description:
      "Исследуйте распознавание, конфликты, словарные правила и диагностические evidence.",
    eyebrow: "Technical",
    permission: "DictionariesManage",
  },
];

export default function HomePage() {
  const session = useAuthSession();
  const { access, hasPermission } = useCurrentUserAccess();

  const visibleActions = homeActions.filter((action) =>
    hasPermission(action.permission),
  );

  const allowedPermissionsCount =
    access?.permissions.filter((permission) => permission.isAllowed).length ??
    0;

  return (
    <PageWorkspace
      eyebrow="Обзор"
      title="Главная"
      description={`Добро пожаловать, ${
        session?.displayName ?? "пользователь"
      }. Выберите рабочий раздел Electronic CRM.`}
    >
      <section className="grid gap-4 md:grid-cols-2">
        {visibleActions.map((action) => (
          <Link
            key={action.href}
            href={action.href}
            className="group rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-6 shadow-sm shadow-[var(--app-shadow)] transition hover:-translate-y-0.5 hover:border-[var(--app-accent-border)] hover:bg-[var(--app-panel-hover)]"
          >
            <div className="flex items-start justify-between gap-5">
              <div className="min-w-0">
                <p className="text-xs font-semibold uppercase tracking-[0.14em] text-[var(--app-accent)]">
                  {action.eyebrow}
                </p>

                <h2 className="mt-2 text-xl font-semibold text-[var(--app-text)]">
                  {action.title}
                </h2>

                <p className="mt-3 max-w-xl text-sm leading-6 text-[var(--app-muted)]">
                  {action.description}
                </p>
              </div>

              <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] text-lg text-[var(--app-muted)] transition group-hover:border-[var(--app-accent-border)] group-hover:bg-[var(--app-accent-soft)] group-hover:text-[var(--app-accent)]">
                →
              </span>
            </div>
          </Link>
        ))}
      </section>

      <section className="rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5 shadow-sm shadow-[var(--app-shadow)] sm:p-6">
        <div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
          <div>
            <h2 className="text-lg font-semibold text-[var(--app-text)]">
              Уровень доступа
            </h2>

            <p className="mt-1 text-sm text-[var(--app-muted)]">
              Доступно разрешений: {allowedPermissionsCount}. Меню и действия
              автоматически скрываются в соответствии с настройками учётной
              записи.
            </p>
          </div>

          <span className="inline-flex w-fit rounded-full border border-[var(--app-role-border)] bg-[var(--app-role-soft)] px-3 py-1.5 text-xs font-semibold text-[var(--app-role-text)]">
            {getUserTypeLabel(access?.userType ?? session?.userType)}
          </span>
        </div>
      </section>
    </PageWorkspace>
  );
}
