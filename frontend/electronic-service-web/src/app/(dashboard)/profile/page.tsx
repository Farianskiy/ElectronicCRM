"use client";

import { useState } from "react";
import { useAuthSession } from "@/features/auth/model/useAuthSession";
import { isTechnicalUser } from "@/shared/api/authToken";
import { PageWorkspace } from "@/shared/ui/PageWorkspace";
import { AppButton } from "@/shared/ui/AppButton";

type CopyState = "idle" | "copied" | "failed";

export default function ProfilePage() {
  const session = useAuthSession();
  const technical = isTechnicalUser(session);
  const [copyState, setCopyState] = useState<CopyState>("idle");

  const catalogAllowedCount = 3;
  const catalogPermissionCount = 3;
  const qualityAllowedCount = technical ? 3 : 0;
  const qualityPermissionCount = 3;

  async function handleCopyUserId(): Promise<void> {
    const userId = session?.userId;

    if (!userId) {
      return;
    }

    try {
      await navigator.clipboard.writeText(userId);
      setCopyState("copied");
    } catch {
      setCopyState("failed");
    }
  }

  return (
    <PageWorkspace
      eyebrow="Учётная запись"
      title="Профиль"
      description="Данные текущего пользователя и доступные рабочие возможности Electronic CRM."
    >
      <div className="mx-auto grid w-full max-w-5xl gap-6">
        <article className="rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5 shadow-sm shadow-[var(--app-shadow)] sm:p-6">
          <div className="flex flex-col gap-6 md:flex-row md:items-center md:justify-between">
            <div className="flex min-w-0 items-center gap-4 sm:gap-5">
              <div className="flex h-20 w-20 shrink-0 items-center justify-center rounded-full bg-[var(--app-accent-strong)] text-3xl font-bold text-white shadow-lg shadow-[var(--app-shadow)] sm:h-24 sm:w-24 sm:text-4xl">
                {session?.displayName?.[0]?.toUpperCase() ?? "U"}
              </div>

              <div className="min-w-0">
                <div className="flex flex-wrap items-center gap-3">
                  <h2 className="truncate text-2xl font-bold text-[var(--app-text)]">
                    {session?.displayName ?? "Пользователь"}
                  </h2>

                  <span className="inline-flex rounded-full border border-[var(--app-role-border)] bg-[var(--app-role-soft)] px-3 py-1 text-xs font-semibold text-[var(--app-role-text)]">
                    {session?.userType ?? "Unknown"}
                  </span>
                </div>

                <p className="mt-1 text-sm text-[var(--app-muted)]">
                  {technical
                    ? "Технический специалист"
                    : "Пользователь каталога"}
                </p>

                <p className="mt-3 text-xs leading-5 text-[var(--app-subtle)]">
                  Роль определяет доступные разделы и действия в системе.
                </p>
              </div>
            </div>

            <div className="min-w-0 rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4 md:w-[390px]">
              <p className="text-xs font-medium text-[var(--app-subtle)]">
                Идентификатор пользователя
              </p>

              <div className="mt-2 flex flex-col gap-3 sm:flex-row sm:items-center">
                <code className="min-w-0 flex-1 break-all font-mono text-sm text-[var(--app-text)]">
                  {session?.userId ?? "—"}
                </code>

                <AppButton
                  variant="primary"
                  size="sm"
                  disabled={!session?.userId}
                  onClick={handleCopyUserId}
                  aria-live="polite"
                >
                  <CopyIcon />

                  {copyState === "copied"
                    ? "Скопировано"
                    : copyState === "failed"
                      ? "Не удалось"
                      : "Копировать"}
                </AppButton>
              </div>
            </div>
          </div>
        </article>

        <section>
          <div>
            <h2 className="text-xl font-semibold text-[var(--app-text)]">
              Уровень доступа
            </h2>

            <p className="mt-1 text-sm leading-6 text-[var(--app-muted)]">
              Возможности сгруппированы по двум рабочим режимам Electronic CRM.
            </p>
          </div>

          <div className="mt-4 grid gap-4 md:grid-cols-2">
            <AccessSummaryCard
              title="Работа с каталогом"
              description="Поиск, просмотр каталога и передача предложений."
              allowedCount={catalogAllowedCount}
              totalCount={catalogPermissionCount}
              available
            />

            <AccessSummaryCard
              title="Настройка и качество"
              description="Технический разбор, модерация и изменение справочников."
              allowedCount={qualityAllowedCount}
              totalCount={qualityPermissionCount}
              available={technical}
            />
          </div>
        </section>

        <details className="group overflow-hidden rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] shadow-sm shadow-[var(--app-shadow)]">
          <summary className="flex min-h-16 cursor-pointer list-none items-center justify-between gap-4 px-5 py-4 transition hover:bg-[var(--app-panel-hover)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-[var(--app-accent)] [&::-webkit-details-marker]:hidden">
            <div>
              <h2 className="text-base font-semibold text-[var(--app-text)]">
                Подробные права доступа
              </h2>

              <p className="mt-1 text-xs leading-5 text-[var(--app-muted)]">
                Откройте список, чтобы посмотреть каждую доступную возможность.
              </p>
            </div>

            <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] text-[var(--app-muted)] transition-transform group-open:rotate-180">
              <ChevronDownIcon />
            </span>
          </summary>

          <div className="border-t border-[var(--app-border)] p-5 sm:p-6">
            <div className="grid gap-6 lg:grid-cols-2">
              <PermissionGroup
                title="Работа с каталогом"
                description="Основные ежедневные операции с товарами."
              >
                <AccessRow
                  label="Поиск через Ассистента"
                  description="Поиск товаров с помощью обычного текстового запроса."
                  allowed
                />

                <AccessRow
                  label="Просмотр каталога"
                  description="Товары, производители, цены, остатки и характеристики."
                  allowed
                />

                <AccessRow
                  label="Предложения по неизвестным словам"
                  description="Передача неизвестных фрагментов на техническую проверку."
                  allowed
                />
              </PermissionGroup>

              <PermissionGroup
                title="Настройка и качество"
                description="Технические инструменты управления знаниями CRM."
              >
                <AccessRow
                  label="Технический разбор запроса"
                  description="Распознанные значения, источники и внутреннее представление."
                  allowed={technical}
                />

                <AccessRow
                  label="Модерация словаря"
                  description="Одобрение, изменение и отклонение предложений."
                  allowed={technical}
                />

                <AccessRow
                  label="Изменение каталога"
                  description="Редактирование справочников и технических данных."
                  allowed={technical}
                />
              </PermissionGroup>
            </div>
          </div>
        </details>
      </div>
    </PageWorkspace>
  );
}

function AccessSummaryCard({
  title,
  description,
  allowedCount,
  totalCount,
  available,
}: {
  title: string;
  description: string;
  allowedCount: number;
  totalCount: number;
  available: boolean;
}) {
  return (
    <article className="rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5 shadow-sm shadow-[var(--app-shadow)]">
      <div className="flex items-start justify-between gap-4">
        <div className="min-w-0">
          <h3 className="text-base font-semibold text-[var(--app-text)]">
            {title}
          </h3>

          <p className="mt-2 text-xs leading-5 text-[var(--app-muted)]">
            {description}
          </p>
        </div>

        <span
          className={
            available
              ? "inline-flex shrink-0 items-center gap-2 rounded-full border border-[var(--app-success-border)] bg-[var(--app-success-soft)] px-3 py-1.5 text-xs font-semibold text-[var(--app-success)]"
              : "inline-flex shrink-0 items-center gap-2 rounded-full border border-[var(--app-border)] bg-[var(--app-surface)] px-3 py-1.5 text-xs font-semibold text-[var(--app-muted)]"
          }
        >
          {available ? (
            <span
              aria-hidden="true"
              className="h-1.5 w-1.5 rounded-full bg-[var(--app-success)]"
            />
          ) : (
            <LockIcon />
          )}

          {available ? "Доступен" : "Недоступен"}
        </span>
      </div>

      <div className="mt-5 flex items-end justify-between gap-4 border-t border-[var(--app-border)] pt-4">
        <div>
          <p className="text-2xl font-bold text-[var(--app-text)]">
            {allowedCount}
            <span className="text-base font-medium text-[var(--app-subtle)]">
              {" "}
              из {totalCount}
            </span>
          </p>

          <p className="mt-1 text-xs text-[var(--app-muted)]">
            доступных возможностей
          </p>
        </div>

        <div
          className="h-2 w-28 overflow-hidden rounded-full bg-[var(--app-surface)]"
          aria-label={`Доступно ${allowedCount} из ${totalCount}`}
        >
          <div
            className={
              available
                ? "h-full rounded-full bg-[var(--app-success)]"
                : "h-full rounded-full bg-[var(--app-subtle)]"
            }
            style={{
              width: `${Math.round((allowedCount / totalCount) * 100)}%`,
            }}
          />
        </div>
      </div>
    </article>
  );
}

function PermissionGroup({
  title,
  description,
  children,
}: {
  title: string;
  description: string;
  children: React.ReactNode;
}) {
  return (
    <section>
      <h3 className="text-base font-semibold text-[var(--app-text)]">
        {title}
      </h3>

      <p className="mt-1 text-xs leading-5 text-[var(--app-muted)]">
        {description}
      </p>

      <div className="mt-4 grid gap-3">{children}</div>
    </section>
  );
}

function AccessRow({
  label,
  description,
  allowed,
}: {
  label: string;
  description: string;
  allowed: boolean;
}) {
  return (
    <div className="rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4">
      <div className="flex items-start gap-3">
        <span
          className={
            allowed
              ? "mt-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded-xl border border-[var(--app-success-border)] bg-[var(--app-success-soft)] text-[var(--app-success)]"
              : "mt-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded-xl border border-[var(--app-border)] bg-[var(--app-panel)] text-[var(--app-subtle)]"
          }
        >
          {allowed ? <CheckIcon /> : <LockIcon />}
        </span>

        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <p className="text-sm font-semibold text-[var(--app-text)]">
              {label}
            </p>

            <span
              className={
                allowed
                  ? "text-xs font-semibold text-[var(--app-success)]"
                  : "text-xs font-semibold text-[var(--app-subtle)]"
              }
            >
              {allowed ? "Разрешено" : "Недоступно для роли"}
            </span>
          </div>

          <p className="mt-1 text-xs leading-5 text-[var(--app-muted)]">
            {description}
          </p>
        </div>
      </div>
    </div>
  );
}

function CopyIcon() {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      className="h-4 w-4"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <rect x="8" y="8" width="11" height="11" rx="2" />
      <path d="M16 8V6a2 2 0 00-2-2H6a2 2 0 00-2 2v8a2 2 0 002 2h2" />
    </svg>
  );
}

function CheckIcon() {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      className="h-4 w-4"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M5 12.5l4 4L19 7" />
    </svg>
  );
}

function LockIcon() {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      className="h-4 w-4"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <rect x="5" y="10" width="14" height="10" rx="2" />
      <path d="M8 10V7a4 4 0 018 0v3" />
    </svg>
  );
}

function ChevronDownIcon() {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      className="h-4 w-4"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M6 9l6 6 6-6" />
    </svg>
  );
}
