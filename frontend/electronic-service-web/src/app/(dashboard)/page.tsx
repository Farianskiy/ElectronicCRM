"use client";

import { useQuery } from "@tanstack/react-query";
import Link from "next/link";
import type { ReactNode } from "react";
import { useAuthSession } from "@/features/auth/model/useAuthSession";
import { useCurrentUserAccess } from "@/features/auth/model/CurrentUserAccessContext";
import type { UserPermissionCode } from "@/features/auth/model/userPermissions";
import { getCatalogImportReviewQueue } from "@/features/catalogImports/api/getCatalogImportReviewQueue";
import { getMyCatalogImportBatches } from "@/features/catalogImports/api/getMyCatalogImportBatches";
import { getCatalogImportStatusLabel } from "@/features/catalogImports/model/catalogImportStatus";
import { catalogImportQueryKeys } from "@/features/catalogImports/model/queryKeys";
import type { CatalogImportBatchStatus } from "@/features/catalogImports/model/types";
import { CatalogImportStatusBadge } from "@/features/catalogImports/ui/status/CatalogImportStatusBadge";
import { getMyCatalogPriceCalculations } from "@/features/catalogPriceCalculations/api/getMyCatalogPriceCalculations";
import { catalogPriceCalculationQueryKeys } from "@/features/catalogPriceCalculations/model/queryKeys";
import type { CatalogPriceCalculationStatus } from "@/features/catalogPriceCalculations/model/types";
import { getCatalogProducts } from "@/features/catalogProducts/api/getCatalogProducts";
import { formatDate, formatPrice } from "@/shared/lib/formatters";
import styles from "./page.module.css";

type DashboardIconName =
  | "assistant"
  | "calculator"
  | "check"
  | "clock"
  | "document"
  | "error"
  | "import"
  | "products"
  | "shield";

type AccentTone = "teal" | "blue" | "violet" | "amber" | "red";

interface DashboardAction {
  href: string;
  title: string;
  description: string;
  icon: DashboardIconName;
  tone: AccentTone;
  permission: UserPermissionCode;
}

interface DashboardMetric {
  title: string;
  value?: number;
  description: string;
  icon: DashboardIconName;
  tone: AccentTone;
  isLoading: boolean;
  isError: boolean;
}

interface DashboardOperation {
  id: string;
  href: string;
  kind: "import" | "calculation";
  title: string;
  details: string;
  date: string;
  importStatus?: CatalogImportBatchStatus;
  calculationStatus?: CatalogPriceCalculationStatus;
}

const dashboardActions: DashboardAction[] = [
  {
    href: "/catalog/imports/new",
    title: "Импортировать Excel",
    description: "Загрузить и распознать данные",
    icon: "import",
    tone: "teal",
    permission: "CatalogImportsCreate",
  },
  {
    href: "/catalog/assistant",
    title: "Найти товар",
    description: "Поиск по каталогу и характеристикам",
    icon: "assistant",
    tone: "blue",
    permission: "AssistantUse",
  },
  {
    href: "/catalog/price-calculations",
    title: "Расчёт цен",
    description: "Проектные цены и скидки",
    icon: "calculator",
    tone: "violet",
    permission: "PriceCalculationsManage",
  },
  {
    href: "/catalog/products",
    title: "Каталог товаров",
    description: "Цены, характеристики и остатки",
    icon: "products",
    tone: "blue",
    permission: "ProductsView",
  },
  {
    href: "/catalog/imports",
    title: "Мои импорты",
    description: "История, статусы и результаты",
    icon: "document",
    tone: "teal",
    permission: "CatalogImportsCreate",
  },
  {
    href: "/catalog/import-reviews",
    title: "Проверить качество",
    description: "Очередь технического контроля",
    icon: "shield",
    tone: "violet",
    permission: "CatalogImportsReview",
  },
];

const attentionImportStatuses = new Set<CatalogImportBatchStatus>([
  "MappingRequired",
  "NeedsCorrection",
  "Failed",
  "ChangesRequested",
  "Rejected",
]);

const toneClasses: Record<AccentTone, string> = {
  teal: "border-teal-400/25 bg-teal-400/10 text-teal-300 [[data-theme=light]_&]:text-teal-700",
  blue: "border-sky-400/25 bg-sky-400/10 text-sky-300 [[data-theme=light]_&]:text-sky-700",
  violet:
    "border-violet-400/25 bg-violet-400/10 text-violet-300 [[data-theme=light]_&]:text-violet-700",
  amber:
    "border-amber-400/25 bg-amber-400/10 text-amber-300 [[data-theme=light]_&]:text-amber-700",
  red: "border-red-400/25 bg-red-400/10 text-red-300 [[data-theme=light]_&]:text-red-700",
};

const barToneClasses: Record<AccentTone, string> = {
  teal: "text-teal-400",
  blue: "text-sky-400",
  violet: "text-violet-400",
  amber: "text-amber-400",
  red: "text-red-400",
};

function formatCount(value: number): string {
  return new Intl.NumberFormat("ru-RU").format(value);
}

function getCalculationStatusLabel(
  status: CatalogPriceCalculationStatus,
): string {
  switch (status) {
    case "Draft":
      return "Черновик";
    case "Completed":
      return "Завершён";
    case "Archived":
      return "В архиве";
  }
}

function CalculationStatusBadge({
  status,
}: {
  status: CatalogPriceCalculationStatus;
}) {
  const className =
    status === "Draft"
      ? "border-[var(--app-warning-border)] bg-[var(--app-warning-soft)] text-[var(--app-warning)]"
      : status === "Completed"
        ? "border-[var(--app-success-border)] bg-[var(--app-success-soft)] text-[var(--app-success)]"
        : "border-[var(--app-border)] bg-[var(--app-surface)] text-[var(--app-muted)]";

  return (
    <span
      className={`inline-flex rounded-md border px-2 py-1 text-[10px] font-semibold ${className}`}
    >
      {getCalculationStatusLabel(status)}
    </span>
  );
}

export default function HomePage() {
  const session = useAuthSession();
  const { hasPermission } = useCurrentUserAccess();

  const canViewProducts = hasPermission("ProductsView");
  const canManageImports = hasPermission("CatalogImportsCreate");
  const canReviewImports = hasPermission("CatalogImportsReview");
  const canManageCalculations = hasPermission("PriceCalculationsManage");

  const productsQuery = useQuery({
    queryKey: ["catalog-products", "dashboard-total"],
    queryFn: () =>
      getCatalogProducts({
        search: "",
        productTypeCode: null,
        manufacturer: null,
        page: 1,
        pageSize: 1,
      }),
    enabled: canViewProducts,
  });

  const importsQuery = useQuery({
    queryKey: catalogImportQueryKeys.my(null, 1, 10),
    queryFn: () =>
      getMyCatalogImportBatches({ status: null, page: 1, pageSize: 10 }),
    enabled: canManageImports,
  });

  const calculationsQuery = useQuery({
    queryKey: catalogPriceCalculationQueryKeys.list(null, 1, 10),
    queryFn: () =>
      getMyCatalogPriceCalculations({ status: null, page: 1, pageSize: 10 }),
    enabled: canManageCalculations,
  });

  const draftCalculationsQuery = useQuery({
    queryKey: catalogPriceCalculationQueryKeys.list("Draft", 1, 1),
    queryFn: () =>
      getMyCatalogPriceCalculations({ status: "Draft", page: 1, pageSize: 1 }),
    enabled: canManageCalculations,
  });

  const reviewQueueQuery = useQuery({
    queryKey: catalogImportQueryKeys.reviewQueue(null, 1, 5),
    queryFn: () =>
      getCatalogImportReviewQueue({ status: null, page: 1, pageSize: 5 }),
    enabled: canReviewImports,
  });

  const visibleActions = dashboardActions.filter((action) =>
    hasPermission(action.permission),
  );
  const attentionImports = (importsQuery.data?.items ?? []).filter((item) =>
    attentionImportStatuses.has(item.status),
  );

  const metrics: DashboardMetric[] = [];

  if (canViewProducts) {
    metrics.push({
      title: "Всего товаров",
      value: productsQuery.data?.totalCount,
      description: "В каталоге компании",
      icon: "products",
      tone: "teal",
      isLoading: productsQuery.isPending,
      isError: productsQuery.isError,
    });
  }
  if (canManageImports) {
    metrics.push({
      title: "Мои импорты",
      value: importsQuery.data?.totalCount,
      description: "За всё время",
      icon: "document",
      tone: "blue",
      isLoading: importsQuery.isPending,
      isError: importsQuery.isError,
    });
  }
  if (canManageCalculations) {
    metrics.push({
      title: "Черновики расчётов",
      value: draftCalculationsQuery.data?.totalCount,
      description: "Можно продолжить",
      icon: "calculator",
      tone: "violet",
      isLoading: draftCalculationsQuery.isPending,
      isError: draftCalculationsQuery.isError,
    });
  }
  if (canReviewImports) {
    metrics.push({
      title: "Ожидают проверки",
      value: reviewQueueQuery.data?.totalCount,
      description: "Импортов в очереди",
      icon: "clock",
      tone: "amber",
      isLoading: reviewQueueQuery.isPending,
      isError: reviewQueueQuery.isError,
    });
  } else if (canManageImports) {
    metrics.push({
      title: "Нужны действия",
      value: attentionImports.length,
      description: "В последних импортах",
      icon: "error",
      tone: attentionImports.length > 0 ? "red" : "amber",
      isLoading: importsQuery.isPending,
      isError: importsQuery.isError,
    });
  }

  const operations: DashboardOperation[] = [
    ...(importsQuery.data?.items ?? []).map((item) => ({
      id: `import-${item.batchId}`,
      href: `/catalog/imports/${item.batchId}`,
      kind: "import" as const,
      title: item.originalFileName,
      details: `${formatCount(item.rowsCount)} ${getPluralLabel(item.rowsCount, "строка", "строки", "строк")}`,
      date: item.lastActivityAtUtc,
      importStatus: item.status,
    })),
    ...(calculationsQuery.data?.items ?? []).map((item) => ({
      id: `calculation-${item.calculationId}`,
      href: `/catalog/price-calculations/${item.calculationId}`,
      kind: "calculation" as const,
      title: item.title,
      details:
        item.linesCount > 0
          ? `${formatCount(item.linesCount)} ${getPluralLabel(item.linesCount, "позиция", "позиции", "позиций")} · ${formatPrice(item.totalAmount, item.currency)}`
          : "Пустой расчёт",
      date: item.updatedAtUtc ?? item.createdAtUtc,
      calculationStatus: item.status,
    })),
  ]
    .sort(
      (left, right) =>
        new Date(right.date).getTime() - new Date(left.date).getTime(),
    )
    .slice(0, 5);

  const operationsLoading =
    (canManageImports && importsQuery.isPending) ||
    (canManageCalculations && calculationsQuery.isPending);
  const operationsError =
    (canManageImports && importsQuery.isError) ||
    (canManageCalculations && calculationsQuery.isError);

  return (
    <div className={styles.dashboard}>
      <section className={styles.hero}>
        <div className={styles.heroContent}>
          <h1 className={styles.heroTitle}>
            Добрый день, {" "}
            <span className={styles.heroAccent}>
              {session?.displayName ?? "пользователь"}
            </span>
          </h1>
          <p className={styles.heroCopy}>
            Здесь вы можете работать с каталогом, импортировать данные и
            контролировать их качество.
          </p>
        </div>

        <div className={styles.heroAside}>
          <div className={styles.heroSignal} aria-hidden="true">
            <span />
            <span />
            <span />
            <span />
          </div>
          <p>
            Точные данные —<br />
            <span className={styles.heroAccent}>эффективный бизнес</span>
          </p>
        </div>
      </section>

      {metrics.length > 0 && (
        <section className={styles.metrics} aria-label="Ключевые показатели">
          {metrics.map((metric) => (
            <MetricCard key={metric.title} metric={metric} />
          ))}
        </section>
      )}

      {(canManageImports || canManageCalculations || canReviewImports) && (
        <section className={styles.workspace}>
          {(canManageImports || canManageCalculations) && (
            <div className={styles.panel}>
              <PanelHeader
                title="Последние операции"
                href={
                  canManageImports
                    ? "/catalog/imports"
                    : "/catalog/price-calculations"
                }
                linkLabel="Все операции"
              />

              {operationsLoading ? (
                <OperationsSkeleton />
              ) : operationsError && operations.length === 0 ? (
                <EmptyState
                  icon="error"
                  title="Не удалось загрузить операции"
                  description="Обновите страницу или повторите попытку позже."
                />
              ) : operations.length === 0 ? (
                <EmptyState
                  icon="document"
                  title="Операций пока нет"
                  description="Создайте расчёт или загрузите первый Excel-файл."
                />
              ) : (
                <div>
                  <div className={styles.operationHeader} aria-hidden="true">
                    <span>Тип</span>
                    <span>Наименование / файл</span>
                    <span>Статус</span>
                    <span>Обновлено</span>
                    <span />
                  </div>
                  {operations.map((operation) => (
                    <OperationRow key={operation.id} operation={operation} />
                  ))}
                </div>
              )}
            </div>
          )}

          <div className={styles.panel}>
            <PanelHeader
              title="Требует внимания"
              count={
                canReviewImports
                  ? reviewQueueQuery.data?.totalCount
                  : attentionImports.length
              }
              href={
                canReviewImports
                  ? "/catalog/import-reviews"
                  : "/catalog/imports"
              }
              linkLabel="Все задачи"
            />

            {canReviewImports ? (
              reviewQueueQuery.isPending ? (
                <AttentionSkeleton />
              ) : reviewQueueQuery.isError ? (
                <EmptyState
                  icon="error"
                  title="Очередь недоступна"
                  description="Не удалось получить импорты на проверку."
                />
              ) : (reviewQueueQuery.data?.items.length ?? 0) === 0 ? (
                <EmptyState
                  icon="check"
                  title="Очередь разобрана"
                  description="Новых импортов на проверку сейчас нет."
                />
              ) : (
                <div className={styles.attentionList}>
                  {reviewQueueQuery.data?.items.slice(0, 5).map((item) => (
                    <AttentionItem
                      key={item.batchId}
                      href={`/catalog/imports/${item.batchId}?from=review-queue`}
                      title={item.originalFileName}
                      description={`${item.createdByDisplayName} · ${formatCount(item.rowsCount)} строк`}
                      date={item.submittedAtUtc ?? item.createdAtUtc}
                      tone="amber"
                    />
                  ))}
                </div>
              )
            ) : importsQuery.isPending ? (
              <AttentionSkeleton />
            ) : attentionImports.length === 0 ? (
              <EmptyState
                icon="check"
                title="Всё в порядке"
                description="В последних импортах нет задач, требующих действий."
              />
            ) : (
              <div className={styles.attentionList}>
                {attentionImports.slice(0, 5).map((item) => (
                  <AttentionItem
                    key={item.batchId}
                    href={`/catalog/imports/${item.batchId}`}
                    title={item.originalFileName}
                    description={getCatalogImportStatusLabel(item.status)}
                    date={item.lastActivityAtUtc}
                    tone={item.status === "Failed" ? "red" : "amber"}
                  />
                ))}
              </div>
            )}
          </div>
        </section>
      )}

      {visibleActions.length > 0 && (
        <section className={`${styles.panel} ${styles.quickPanel}`}>
          <div className={styles.quickPanelHeader}>
            <h2 className={styles.panelTitle}>Быстрые действия</h2>
          </div>
          <div className={styles.quickGrid}>
            {visibleActions.slice(0, 4).map((action) => (
              <QuickAction key={action.href} action={action} />
            ))}
          </div>
        </section>
      )}
    </div>
  );
}

function MetricCard({ metric }: { metric: DashboardMetric }) {
  return (
    <article className={styles.metricCard}>
      <IconTile icon={metric.icon} tone={metric.tone} size="lg" />
      <div className={styles.metricBody}>
        <p className={styles.metricTitle}>{metric.title}</p>
        {metric.isLoading ? (
          <div
            className={styles.skeleton}
            style={{ width: 78, height: 31, marginTop: 7 }}
          />
        ) : metric.isError || metric.value === undefined ? (
          <p className="mt-2 text-xs font-semibold text-[var(--app-danger)]">
            Нет данных
          </p>
        ) : (
          <p className={styles.metricValue}>{formatCount(metric.value)}</p>
        )}
        <p className={styles.metricDescription}>{metric.description}</p>
      </div>
      <div
        className={`${styles.metricBars} ${barToneClasses[metric.tone]}`}
        aria-hidden="true"
      >
        <span />
        <span />
        <span />
        <span />
        <span />
        <span />
      </div>
    </article>
  );
}

function PanelHeader({
  title,
  count,
  href,
  linkLabel,
}: {
  title: string;
  count?: number;
  href: string;
  linkLabel: string;
}) {
  return (
    <div className={styles.panelHeader}>
      <div className={styles.panelTitleGroup}>
        <h2 className={styles.panelTitle}>{title}</h2>
        {count !== undefined && count > 0 && (
          <span className={styles.countBadge}>{formatCount(count)}</span>
        )}
      </div>
      <Link href={href} className={styles.panelLink}>
        {linkLabel}
        <ArrowIcon />
      </Link>
    </div>
  );
}

function OperationRow({ operation }: { operation: DashboardOperation }) {
  return (
    <Link href={operation.href} className={styles.operationRow}>
      <span className={styles.operationType}>
        <MiniIcon
          icon={operation.kind === "import" ? "import" : "calculator"}
          tone={operation.kind === "import" ? "teal" : "violet"}
        />
        {operation.kind === "import" ? "Импорт" : "Расчёт"}
      </span>

      <span className={styles.operationName}>
        <strong>{operation.title}</strong>
        <span>{operation.details}</span>
      </span>

      <span>
        {operation.importStatus && (
          <CatalogImportStatusBadge status={operation.importStatus} />
        )}
        {operation.calculationStatus && (
          <CalculationStatusBadge status={operation.calculationStatus} />
        )}
      </span>

      <span className={styles.operationDate}>{formatDate(operation.date)}</span>
      <ArrowIcon />
    </Link>
  );
}

function AttentionItem({
  href,
  title,
  description,
  date,
  tone,
}: {
  href: string;
  title: string;
  description: string;
  date: string;
  tone: "amber" | "red";
}) {
  return (
    <Link href={href} className={styles.attentionItem}>
      <IconTile
        icon={tone === "red" ? "error" : "clock"}
        tone={tone}
      />
      <span className={styles.attentionText}>
        <strong>{title}</strong>
        <span>{description}</span>
        <span className={styles.attentionDate}>{formatDate(date)}</span>
      </span>
      <ArrowIcon />
    </Link>
  );
}

function QuickAction({ action }: { action: DashboardAction }) {
  return (
    <Link href={action.href} className={styles.quickAction}>
      <IconTile icon={action.icon} tone={action.tone} size="lg" />
      <span className={styles.quickText}>
        <strong>{action.title}</strong>
        <span>{action.description}</span>
      </span>
      <ArrowIcon />
    </Link>
  );
}

function EmptyState({
  icon,
  title,
  description,
}: {
  icon: DashboardIconName;
  title: string;
  description: string;
}) {
  return (
    <div className={styles.emptyState}>
      <IconTile icon={icon} tone={icon === "error" ? "red" : "teal"} />
      <strong>{title}</strong>
      <p>{description}</p>
    </div>
  );
}

function OperationsSkeleton() {
  return (
    <div>
      <div className={styles.operationHeader}>
        <span>Тип</span>
        <span>Наименование / файл</span>
        <span>Статус</span>
        <span>Обновлено</span>
        <span />
      </div>
      {[0, 1, 2, 3, 4].map((item) => (
        <div key={item} className={styles.operationRow}>
          <div className={styles.skeleton} style={{ width: 68, height: 18 }} />
          <div className={styles.skeleton} style={{ width: "65%", height: 18 }} />
          <div className={styles.skeleton} style={{ width: 90, height: 24 }} />
          <div className={styles.skeleton} style={{ width: 105, height: 14 }} />
        </div>
      ))}
    </div>
  );
}

function AttentionSkeleton() {
  return (
    <div className={styles.attentionList}>
      {[0, 1, 2, 3].map((item) => (
        <div key={item} className={styles.attentionItem}>
          <div className={styles.skeleton} style={{ width: 40, height: 40 }} />
          <div>
            <div className={styles.skeleton} style={{ width: "70%", height: 14 }} />
            <div
              className={styles.skeleton}
              style={{ width: "48%", height: 10, marginTop: 8 }}
            />
          </div>
        </div>
      ))}
    </div>
  );
}

function IconTile({
  icon,
  tone,
  size = "md",
}: {
  icon: DashboardIconName;
  tone: AccentTone;
  size?: "md" | "lg";
}) {
  return (
    <span
      className={`flex shrink-0 items-center justify-center rounded-xl border ${size === "lg" ? "h-12 w-12" : "h-10 w-10"} ${toneClasses[tone]}`}
    >
      <DashboardIcon icon={icon} />
    </span>
  );
}

function MiniIcon({
  icon,
  tone,
}: {
  icon: DashboardIconName;
  tone: AccentTone;
}) {
  return (
    <span
      className={`flex h-7 w-7 shrink-0 items-center justify-center rounded-lg border ${toneClasses[tone]}`}
    >
      <span className="scale-75">
        <DashboardIcon icon={icon} />
      </span>
    </span>
  );
}

function ArrowIcon() {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      className="h-4 w-4 shrink-0 text-[var(--app-subtle)]"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M5 12h14M13 6l6 6-6 6" />
    </svg>
  );
}

function DashboardIcon({ icon }: { icon: DashboardIconName }) {
  const paths: Record<DashboardIconName, ReactNode> = {
    assistant: <path d="M5 5h14a2 2 0 012 2v8a2 2 0 01-2 2h-7l-4.5 3v-3H5a2 2 0 01-2-2V7a2 2 0 012-2zM8 9h8M8 13h5" />,
    calculator: <path d="M5 3h14v18H5V3zM8 7h8M8 11h3M15 11h1M8 15h3M15 15h1M8 18h3M15 18h1" />,
    check: <path d="M20 11a8 8 0 11-4.7-7.3M8 11l3 3 9-9" />,
    clock: <path d="M12 21a9 9 0 100-18 9 9 0 000 18zM12 7v5l3 2" />,
    document: <path d="M6 3h8l4 4v14H6V3zM14 3v5h5M9 13h6M9 17h6" />,
    error: <path d="M12 3L2.5 20h19L12 3zM12 9v5M12 17h.01" />,
    import: <path d="M4 15v4a2 2 0 002 2h12a2 2 0 002-2v-4M12 3v13M7 11l5 5 5-5" />,
    products: <path d="M4 7l8-4 8 4-8 4-8-4zM4 7v10l8 4 8-4V7M12 11v10" />,
    shield: <path d="M12 3l7 3v5c0 4.5-2.7 8.1-7 10-4.3-1.9-7-5.5-7-10V6l7-3zM9 12l2 2 4-5" />,
  };

  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      className="h-5 w-5"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      {paths[icon]}
    </svg>
  );
}

function getPluralLabel(
  count: number,
  one: string,
  few: string,
  many: string,
): string {
  const mod100 = count % 100;
  const mod10 = count % 10;

  if (mod100 >= 11 && mod100 <= 14) return many;
  if (mod10 === 1) return one;
  if (mod10 >= 2 && mod10 <= 4) return few;
  return many;
}
