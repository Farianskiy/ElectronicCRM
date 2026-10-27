"use client";

import Link from "next/link";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useAuthSession } from "@/features/auth/model/useAuthSession";
import { RequirePermission } from "@/features/auth/ui/RequirePermission";
import { httpClient } from "@/shared/api/httpClient";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppButton } from "@/shared/ui/AppButton";
import { AppSelect } from "@/shared/ui/AppSelect";
import { getCatalogManufacturers } from "@/features/catalogMetadata/api/getCatalogManufacturers";
import { getCatalogProductTypes } from "@/features/catalogMetadata/api/getCatalogProductTypes";
import { getCatalogProductTypeCharacteristics } from "@/features/catalogMetadata/api/getCatalogProductTypeCharacteristics";
import { CatalogRecognitionLiteralProposalPreview } from "@/features/catalogImports/ui/rows/CatalogRecognitionLiteralProposalPreview";
import { CatalogRecognitionMultiIntegerPreview } from "@/features/catalogImports/ui/rows/CatalogRecognitionMultiIntegerPreview";
import { CatalogRecognitionRuleSetVersionCreate } from "@/features/catalogImports/ui/rows/CatalogRecognitionRuleSetVersionCreate";
import { CatalogRecognitionRuleSetActivation } from "@/features/catalogImports/ui/rows/CatalogRecognitionRuleSetActivation";
import { CatalogRecognitionRuleSetNamePreview } from "@/features/catalogImports/ui/rows/CatalogRecognitionRuleSetNamePreview";
import { CatalogRecognitionLiteralDraftRecheckPanel } from "@/features/catalogImports/ui/rows/CatalogRecognitionLiteralDraftRecheckPanel";
import { CatalogRecognitionMultiIntegerDraftRecheckPanel } from "@/features/catalogImports/ui/rows/CatalogRecognitionMultiIntegerDraftRecheckPanel";
import { CatalogRecognitionIntegerDraftRecheckPanel } from "@/features/catalogImports/ui/rows/CatalogRecognitionIntegerDraftRecheckPanel";
import {
  switchRecognitionRuleSet,
  checkRecognitionRuleSetTraining,
} from "@/features/catalogImports/api/recognitionRuleSetActivation";
import { getRecognitionRuleSetVersion } from "@/features/catalogImports/api/getRecognitionRuleSetVersions";
import { SuggestionCard } from "@/features/dictionarySuggestions/ui/DictionarySuggestionsContent";
import { approveDictionarySuggestion } from "@/features/dictionarySuggestions/api/approveDictionarySuggestion";
import { rejectDictionarySuggestion } from "@/features/dictionarySuggestions/api/rejectDictionarySuggestion";
import type { AssistantDictionarySuggestion } from "@/features/dictionarySuggestions/model/types";
import { TrainingExamplesPanel } from "./TrainingExamplesPanel";
import { EvaluationReportPanel } from "./EvaluationReportPanel";
import { previewCatalogProductNameRecognition } from "../api/previewCatalogProductNameRecognition";

type Scope = { manufacturerId: string; productTypeId: string };
type Item = {
  id: string;
  kind: string;
  label: string;
  number?: number;
  parentId?: string;
  batchId?: string;
  batchAvailable: boolean;
  createdAtUtc: string;
};
type Page<T> = {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
};
type Overview = {
  asOfUtc: string;
  manufacturer: string;
  productType: string;
  productTypeCode: string;
  activeVersionId: string | null;
  sequenceNumber: number;
  trainingExamples: number;
  controlExamples: number;
  revokedExamples: number;
  excludedSources: number;
  literalDrafts: number;
  integerDrafts: number;
  multiIntegerDrafts: number;
  versions: number;
  pendingSuggestions: number;
  approvedSuggestions: number;
};
type AnalogyMetric = {
  characteristicDefinitionId: string;
  characteristicCode: string;
  shown: number;
  stale: number;
  awaitingDecision: number;
  decisions: number;
  accepted: number;
  corrected: number;
  rejected: number;
  averageSupport: number | null;
};
type AnalogyMetrics = {
  asOfUtc: string;
  shown: number;
  stale: number;
  awaitingDecision: number;
  decisions: number;
  accepted: number;
  corrected: number;
  rejected: number;
  averageSupport: number | null;
  characteristics: AnalogyMetric[];
  modelVersions: Array<{
    modelVersion: string;
    shown: number;
    decisions: number;
    lastDecisionAtUtc: string;
  }>;
};
type AnalogyMode = "Off" | "Shadow" | "Suggest" | "AutoFill";

const variantAnalogyCharacteristicCodes = new Set([
  "RATEDCURRENT",
  "RATED_CURRENT",
  "NOMINALCURRENT",
  "NOMINAL_CURRENT",
  "DIAPASONSRABATVANIA",
  "DIAPASON_SRABATVANIA",
  "TRIPRANGE",
  "POLES",
  "POLECOUNT",
  "POLE_COUNT",
  "LEAKAGECURRENT",
  "LEAKAGE_CURRENT",
  "RESIDUALCURRENT",
  "RESIDUAL_CURRENT",
  "BREAKINGCAPACITY",
  "BREAKING_CAPACITY",
]);

function isVariantAnalogyCharacteristic(code: string): boolean {
  const normalized = Array.from(code.trim().toUpperCase())
    .filter((character) => /[\p{L}\p{N}_]/u.test(character))
    .join("");
  return variantAnalogyCharacteristicCodes.has(normalized);
}
type AnalogySettings = {
  minimumSupportingProducts: number;
  minimumProductsWithCharacteristic: number;
  minimumSupportRatio: number;
  maximumAlternativeValues: number;
  maximumCandidatePoolSize: number;
  characteristics: Record<string, {
    mode: AnalogyMode;
    minimumSupportingProducts: number | null;
    minimumSupportRatio: number | null;
  }>;
};
type AnalogyConfiguration = {
  id: string | null;
  versionNumber: number | null;
  name: string;
  status: string;
  settingsHash: string;
  isSystem: boolean;
  settings: AnalogySettings;
  createdAtUtc: string | null;
  updatedAtUtc: string | null;
  version: number | null;
};
type AnalogyConfigurationWorkspace = {
  active: AnalogyConfiguration;
  versions: AnalogyConfiguration[];
};
type AnalogyEvaluationReport = {
  id: string;
  configurationId: string;
  createdAtUtc: string;
  controlProducts: number;
  controlValues: number;
  improvements: number;
  regressions: number;
  candidateCorrect: number;
  candidateIncorrect: number;
  isReady: boolean;
  isCurrent: boolean;
  blockingReasons: string[];
};
type AnalogyEvaluationReportDetail = {
  report: AnalogyEvaluationReport;
  rows: Array<{
    productId: string;
    productName: string;
    characteristicCode: string;
    expectedValue: string;
    baselineValue: string | null;
    candidateValue: string | null;
    baselineCorrect: boolean;
    candidateCorrect: boolean;
  }>;
};
type Navigate = (values: Record<string, string | undefined>) => void;

function useWorkspace<T>(
  scope: Scope,
  section: string,
  params: Record<string, string | number | undefined> = {},
  enabled = true,
) {
  const user = useAuthSession();
  return useQuery({
    queryKey: ["learning-workspace", user?.userId, scope, section, params],
    queryFn: async ({ signal }) =>
      (
        await httpClient.get<T>(
          `/api/catalog/recognition/learning-workspace/${section}`,
          { params: { ...scope, ...params }, signal },
        )
      ).data,
    enabled: enabled && Boolean(user?.userId),
    retry: false,
    staleTime: 0,
  });
}
function ErrorNotice({ error }: { error: unknown }) {
  return error ? (
    <p role="alert" className="text-red-400">
      {getApiErrorMessage(
        error,
        "Не удалось загрузить данные. Обновите раздел.",
      )}
    </p>
  ) : null;
}
function Pager({
  page,
  total,
  busy,
  change,
}: {
  page: number;
  total: number;
  busy: boolean;
  change: (page: number) => void;
}) {
  return (
    <div className="flex gap-3 items-center">
      <AppButton
        variant="secondary"
        disabled={page <= 1 || busy}
        onClick={() => change(page - 1)}
      >
        Назад
      </AppButton>
      <span>
        Страница {page} · всего {total}
      </span>
      <AppButton
        variant="secondary"
        disabled={page * 20 >= total || busy}
        onClick={() => change(page + 1)}
      >
        Далее
      </AppButton>
    </div>
  );
}

function ruleKindName(kind: number): string {
  switch (kind) {
    case 1:
      return "Точное правило";
    case 2:
      return "Одиночный числовой шаблон";
    case 3:
      return "Составной числовой шаблон";
    default:
      return "Неизвестный вид правила";
  }
}

export function LearningWorkspace() {
  return (
    <RequirePermission permission="DictionariesManage">
      <Workspace />
    </RequirePermission>
  );
}
function Workspace() {
  const params = useSearchParams();
  const router = useRouter();
  const pathname = usePathname();
  const client = useQueryClient();
  const scope = {
    manufacturerId: params.get("manufacturerId") ?? "",
    productTypeId: params.get("productTypeId") ?? "",
  };
  const manufacturers = useQuery({
    queryKey: ["catalog-manufacturers"],
    queryFn: getCatalogManufacturers,
  });
  const types = useQuery({
    queryKey: ["catalog-product-types"],
    queryFn: getCatalogProductTypes,
  });
  const navigate: Navigate = (values) => {
    const next = new URLSearchParams(params.toString());
    Object.entries(values).forEach(([key, value]) =>
      value ? next.set(key, value) : next.delete(key),
    );
    router.push(`${pathname}?${next}`, { scroll: false });
  };
  useEffect(
    () =>
      client.getMutationCache().subscribe((event) => {
        if (
          event.type === "updated" &&
          (event.action.type === "success" || event.action.type === "error")
        ) {
          // Shared forms own the commands. Refresh projections even after a rejected concurrent operation.
          for (const root of [
            "learning-workspace",
            "recognition-evaluation",
            "dictionary-evaluation",
            "suggestion-evidence",
            "recognition-rule-set-state",
          ])
            void client.invalidateQueries({ queryKey: [root] });
        }
      }),
    [client],
  );
  const changeScope = (key: keyof Scope, value: string) =>
    router.push(
      `${pathname}?${new URLSearchParams({ ...scope, [key]: value, branch: "overview" })}`,
    );
  return (
    <main className="grid gap-6">
      <header className="grid gap-3">
        <h1 className="text-2xl font-semibold">Обучение распознавания</h1>

        <p className="max-w-4xl text-sm text-[var(--app-muted)]">
          Здесь подтверждённые примеры превращаются в проверенные изменения
          правил и словаря. Ни одно изменение не включается автоматически.
        </p>

        <Link
          href="/catalog/recognition"
          className="w-fit text-sm text-[var(--app-accent)] hover:underline"
        >
          Вернуться к проверке распознавания
        </Link>
      </header>

      <section
        aria-labelledby="learning-stages-title"
        className="grid gap-4 rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5"
      >
        <div>
          <h2 id="learning-stages-title" className="text-lg font-semibold">
            Как изменение становится действующим
          </h2>

          <p className="mt-1 text-sm text-[var(--app-muted)]">
            Сохранение и подтверждение примера ещё не меняет распознавание.
            Изменение начинает работать только после подготовки, оценки и явного
            включения.
          </p>
        </div>

        <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
          <article className="rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4">
            <p className="text-xs font-semibold text-[var(--app-accent)]">
              Этап 1
            </p>
            <h3 className="mt-1 font-semibold">Подтверждение примера</h3>
            <p className="mt-2 text-sm text-[var(--app-muted)]">
              В импорте сохраняются правильное значение и соответствующий
              фрагмент названия.
            </p>
          </article>

          <article className="rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4">
            <p className="text-xs font-semibold text-[var(--app-accent)]">
              Этап 2
            </p>
            <h3 className="mt-1 font-semibold">Подготовка изменения</h3>
            <p className="mt-2 text-sm text-[var(--app-muted)]">
              Из накопленных примеров создаются черновики правил или предложения
              словаря.
            </p>
          </article>

          <article className="rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4">
            <p className="text-xs font-semibold text-[var(--app-accent)]">
              Этап 3
            </p>
            <h3 className="mt-1 font-semibold">Оценка</h3>
            <p className="mt-2 text-sm text-[var(--app-muted)]">
              Предлагаемое изменение сравнивается с текущим распознаванием.
              Регрессии и устаревшие основания блокируют выпуск.
            </p>
          </article>

          <article className="rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4">
            <p className="text-xs font-semibold text-[var(--app-accent)]">
              Этап 4
            </p>
            <h3 className="mt-1 font-semibold">Включение</h3>
            <p className="mt-2 text-sm text-[var(--app-muted)]">
              После явного подтверждения правило или словарное решение начинает
              использоваться при следующем распознавании.
            </p>
          </article>
        </div>
      </section>

      <section
        aria-labelledby="learning-products-title"
        className="grid gap-4 rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5"
      >
        <div>
          <h2 id="learning-products-title" className="text-lg font-semibold">
            Для каких товаров готовим изменения
          </h2>

          <p className="mt-1 text-sm text-[var(--app-muted)]">
            Выберите производителя и тип товара. Это ограничивает примеры,
            черновики, версии и предложения теми товарами, для которых
            подготавливается изменение распознавания.
          </p>
        </div>

        <div className="grid gap-4 md:grid-cols-2">
          <label className="grid gap-2">
            <span className="text-sm font-medium">Производитель</span>
            <AppSelect
              ariaLabel="Производитель для обучения распознавания"
              value={scope.manufacturerId}
              options={[
                { value: "", label: "Выберите производителя" },
                ...(manufacturers.data ?? []).map((x) => ({
                  value: x.id,
                  label: x.name,
                })),
              ]}
              onChange={(value) => changeScope("manufacturerId", value)}
            />
          </label>

          <label className="grid gap-2">
            <span className="text-sm font-medium">Тип товара</span>
            <AppSelect
              ariaLabel="Тип товара для обучения распознавания"
              value={scope.productTypeId}
              options={[
                { value: "", label: "Выберите тип товара" },
                ...(types.data ?? []).map((x) => ({
                  value: x.id,
                  label: x.name,
                })),
              ]}
              onChange={(value) => changeScope("productTypeId", value)}
            />
          </label>
        </div>

        <ErrorNotice error={manufacturers.error ?? types.error} />

        {!scope.manufacturerId || !scope.productTypeId ? (
          <p className="rounded-xl border border-[var(--app-accent-border)] bg-[var(--app-accent-soft)] p-4 text-sm text-[var(--app-muted)]">
            Выберите производителя и тип товара. После этого появятся примеры,
            правила, предложения словаря, отчёты и действующая конфигурация
            выбранных товаров.
          </p>
        ) : (
          <ScopedWorkspace
            key={`${scope.manufacturerId}:${scope.productTypeId}`}
            scope={scope}
            navigate={navigate}
          />
        )}
      </section>
    </main>
  );
}

function ScopedWorkspace({
  scope,
  navigate,
}: {
  scope: Scope;
  navigate: Navigate;
}) {
  const client = useQueryClient();
  const params = useSearchParams();
  const branch = params.get("branch") ?? "overview";
  const overview = useWorkspace<Overview>(scope, "overview");
  const selected = params.get("id") ?? "";
  const section = params.get("section") ?? "";
  const detailSection =
    branch === "examples"
      ? "examples"
      : branch === "dictionary"
        ? "suggestions"
        : branch === "reports"
          ? section === "dictionaryReports"
            ? section
            : "ruleReports"
          : branch === "rules"
            ? ["literal", "integer", "multi"].includes(section)
              ? section
              : "versions"
            : "versions";
  const detail = useWorkspace<Page<Item>>(
    scope,
    detailSection,
    { id: selected },
    Boolean(selected),
  );
  const tabs = [
    ["overview", "Обзор"],
    ["examples", "Мои подтверждения"],
    ["rules", "Правила"],
    ["dictionary", "Словарь"],
    ["analogies", "Аналогии"],
    ["reports", "Мои отчёты"],
    ["current", "Действует сейчас"],
  ];
  return (
    <>
      <div className="flex flex-wrap gap-2">
        {tabs.map(([value, label]) => (
          <AppButton
            key={value}
            variant={branch === value ? "primary" : "secondary"}
            onClick={() =>
              navigate({
                branch: value,
                section: undefined,
                id: undefined,
                page: undefined,
              })
            }
          >
            {label}
          </AppButton>
        ))}
        <AppButton
          variant="secondary"
          onClick={() =>
            void client.invalidateQueries({ queryKey: ["learning-workspace"] })
          }
        >
          Обновить обзор
        </AppButton>
      </div>
      {overview.isFetching && <p role="status">Обновляем обзор…</p>}
      <ErrorNotice error={overview.error} />
      {overview.data && !overview.isError && (
        <>
          <h2 className="text-xl">
            {overview.data.manufacturer} · {overview.data.productType}
          </h2>
          <p className="text-sm text-[var(--app-muted)]">
            Обзор на {new Date(overview.data.asOfUtc).toLocaleString("ru-RU")}.
            Детали обновляются независимо. Перед выпуском сервер повторно
            проверяет допуск.
          </p>
          {selected && detail.isPending ? (
            <p>Проверяем доступ к выбранному объекту…</p>
          ) : selected && detail.isError ? (
            <ErrorNotice error={detail.error} />
          ) : (
            <>
              {branch === "overview" && (
                <>
                  <p>
                    Мои доступные подтверждения для правил:{" "}
                    <strong>{overview.data.trainingExamples}</strong>; только
                    для контроля правил:{" "}
                    <strong>{overview.data.controlExamples}</strong>;
                    отозванные: <strong>{overview.data.revokedExamples}</strong>
                    ; исключённые источники моих примеров:{" "}
                    <strong>{overview.data.excludedSources}</strong>.
                  </p>
                  <p>
                    Отзыв и исключение — разные действия, счётчики могут
                    пересекаться. Контроль правил не запрещает обучение словаря
                    на обратной связи.
                  </p>
                  <div className="grid gap-4 md:grid-cols-2">
                    <section className="grid gap-3 rounded-xl border border-[var(--app-border)] p-4">
                      <div>
                        <h3 className="font-semibold">
                          Правила для выбранного производителя и типа
                        </h3>
                        <p className="mt-1 text-sm text-[var(--app-muted)]">
                          Правила извлекают значения характеристик из названия
                          товара. Подготовленные правила сначала собираются в
                          версию, затем оцениваются и включаются.
                        </p>
                      </div>

                      <p>
                        Черновики: точные {overview.data.literalDrafts},
                        числовые {overview.data.integerDrafts}, составные{" "}
                        {overview.data.multiIntegerDrafts}. Версии:{" "}
                        {overview.data.versions}.
                      </p>

                      <AppButton
                        onClick={() =>
                          navigate({ branch: "rules", section: "prepare" })
                        }
                      >
                        Подготовить правила
                      </AppButton>

                      <AppButton
                        variant="secondary"
                        onClick={() =>
                          navigate({ branch: "rules", section: "compose" })
                        }
                      >
                        Составить версию правил
                      </AppButton>
                    </section>

                    <section className="grid gap-3 rounded-xl border border-[var(--app-border)] p-4">
                      <div>
                        <h3 className="font-semibold">
                          Словарь для выбранного производителя и типа
                        </h3>
                        <p className="mt-1 text-sm text-[var(--app-muted)]">
                          Словарные предложения связывают найденные обозначения
                          с нормализованными значениями. Они оцениваются и
                          одобряются отдельно от версий правил.
                        </p>
                      </div>

                      <p>
                        Ожидают решения: {overview.data.pendingSuggestions}.
                        Одобрены: {overview.data.approvedSuggestions}.
                      </p>

                      <AppButton
                        onClick={() =>
                          navigate({ branch: "dictionary", status: "Pending" })
                        }
                      >
                        Проверить предложения словаря
                      </AppButton>
                    </section>
                  </div>
                  <AppButton
                    variant="secondary"
                    onClick={() => navigate({ branch: "examples" })}
                  >
                    Проверить мои подтверждения и контроль
                  </AppButton>
                  <BatchPicker scope={scope} navigate={navigate} />
                  <p>
                    Наличие примеров не гарантирует появления правила или
                    независимого контроля. Сведения о прогрессе и последнем
                    проходе фонового обучения не сохраняются.
                  </p>
                </>
              )}
              {branch === "examples" && (
                <TrainingExamplesPanel
                  key={selected}
                  scope={scope}
                  exampleId={selected || undefined}
                  onSelectExample={(id) => navigate({ id })}
                />
              )}
              {branch === "rules" && (
                <Rules
                  scope={scope}
                  overview={overview.data}
                  navigate={navigate}
                />
              )}
              {branch === "dictionary" && (
                <Dictionary scope={scope} navigate={navigate} />
              )}
              {branch === "analogies" && (
                <AnalogyMetricsPanel
                  scope={scope}
                  productTypeCode={overview.data.productTypeCode}
                />
              )}
              {branch === "reports" && (
                <Reports scope={scope} navigate={navigate} />
              )}
              {branch === "current" && (
                <CurrentConfiguration
                  key={overview.data.sequenceNumber}
                  scope={scope}
                  overview={overview.data}
                  navigate={navigate}
                />
              )}
              {!tabs.some(([value]) => value === branch) && (
                <p role="alert">Неизвестный раздел. Выберите вкладку.</p>
              )}
            </>
          )}
        </>
      )}
    </>
  );
}

function AnalogyMetricsPanel({
  scope,
  productTypeCode,
}: {
  scope: Scope;
  productTypeCode: string;
}) {
  const metrics = useWorkspace<AnalogyMetrics>(scope, "analogyMetrics");

  if (metrics.isPending) {
    return <p role="status">Собираем метрики решений по аналогам…</p>;
  }

  if (metrics.isError || !metrics.data) {
    return <ErrorNotice error={metrics.error} />;
  }

  const data = metrics.data;
  const exactAcceptance = data.decisions
    ? Math.round((data.accepted / data.decisions) * 100)
    : 0;

  return (
    <div className="grid gap-5">
    <AnalogyConfigurationPanel scope={scope} productTypeCode={productTypeCode} />
    <section className="grid gap-5 rounded-xl border border-[var(--app-border)] p-4">
      <div>
        <h3 className="font-semibold">Качество предложений по аналогам</h3>
        <p className="mt-1 text-sm text-[var(--app-muted)]">
          Здесь учитываются показы и сохранённые решения пользователей.
          Повторный показ того же предложения не создаёт дубликат.
        </p>
      </div>

      <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        <MetricCard label="Показано" value={data.shown.toString()} />
        <MetricCard label="Всего решений" value={data.decisions.toString()} />
        <MetricCard label="Принято" value={data.accepted.toString()} />
        <MetricCard label="Исправлено" value={data.corrected.toString()} />
        <MetricCard label="Отклонено" value={data.rejected.toString()} />
        <MetricCard
          label="Ожидают решения"
          value={data.awaitingDecision.toString()}
        />
        <MetricCard label="Устарело" value={data.stale.toString()} />
        <MetricCard label="Принято без изменений" value={`${exactAcceptance}%`} />
      </div>

      <p className="text-sm text-[var(--app-muted)]">
        Средняя поддержка похожими товарами: {data.averageSupport == null
          ? "нет данных"
          : `${Math.round(data.averageSupport * 100)}%`}. Отчёт на {new Date(
            data.asOfUtc,
          ).toLocaleString("ru-RU")}.
      </p>

      <div className="grid gap-3">
        <h4 className="font-semibold">По характеристикам</h4>
        {data.characteristics.length === 0 ? (
          <p className="text-sm text-[var(--app-muted)]">
            По выбранной области ещё нет сохранённых решений.
          </p>
        ) : (
          data.characteristics.map((item) => (
            <article
              key={item.characteristicDefinitionId}
              className="rounded-xl border border-[var(--app-border)] p-4"
            >
              <p className="font-medium">{item.characteristicCode}</p>
              <p className="mt-1 text-sm text-[var(--app-muted)]">
                Показано: {item.shown}; решений: {item.decisions}; ожидают решения:{" "}
                {item.awaitingDecision}; устарело: {item.stale}; принято:{" "}
                {item.accepted}; исправлено: {item.corrected}; отклонено:{" "}
                {item.rejected}; средняя поддержка:{" "}
                {item.averageSupport == null
                  ? "нет данных"
                  : `${Math.round(item.averageSupport * 100)}%`}.
              </p>
            </article>
          ))
        )}
      </div>

      {data.modelVersions.length > 0 && (
        <details>
          <summary className="cursor-pointer font-medium">
            Версии алгоритма
          </summary>
          <div className="mt-3 grid gap-2 text-sm text-[var(--app-muted)]">
            {data.modelVersions.map((item) => (
              <p key={item.modelVersion}>
                {item.modelVersion}: показано {item.shown}, решений:{" "}
                {item.decisions}, последнее {new Date(
                  item.lastDecisionAtUtc,
                ).toLocaleString("ru-RU")}
              </p>
            ))}
          </div>
        </details>
      )}
    </section>
    </div>
  );
}

function AnalogyConfigurationPanel({
  scope,
  productTypeCode,
}: {
  scope: Scope;
  productTypeCode: string;
}) {
  const client = useQueryClient();
  const query = useQuery({
    queryKey: ["catalog-analogy-configurations", scope],
    queryFn: async ({ signal }) =>
      (await httpClient.get<AnalogyConfigurationWorkspace>(
        "/api/catalog/products/analogies/configurations",
        { params: scope, signal },
      )).data,
    retry: false,
  });
  const characteristics = useQuery({
    queryKey: ["catalog-product-type-characteristics", productTypeCode],
    queryFn: () => getCatalogProductTypeCharacteristics(productTypeCode),
    enabled: Boolean(productTypeCode),
  });
  const [name, setName] = useState("Новая настройка аналогов");
  const [settings, setSettings] = useState<AnalogySettings | null>(null);
  const [editing, setEditing] = useState<AnalogyConfiguration | null>(null);
  const currentSettings = settings ?? query.data?.active.settings ?? null;

  const save = useMutation({
    mutationFn: async () => {
      if (!currentSettings) return;
      const body = {
        name,
        settings: currentSettings,
        expectedVersion: editing?.version ?? undefined,
      };
      if (editing?.id) {
        await httpClient.put(
          `/api/catalog/products/analogies/configurations/${editing.id}`,
          body,
        );
      } else {
        await httpClient.post(
          "/api/catalog/products/analogies/configurations",
          body,
          { params: scope },
        );
      }
    },
    onSuccess: async () => {
      setEditing(null);
      await client.invalidateQueries({
        queryKey: ["catalog-analogy-configurations", scope],
      });
    },
  });

  if (query.isPending) return <p>Загружаем настройки аналогов…</p>;
  if (query.isError || !query.data || !currentSettings) {
    return <ErrorNotice error={query.error} />;
  }

  const active = query.data.active;
  const numberField = (
    label: string,
    key: keyof Pick<AnalogySettings,
      "minimumSupportingProducts" | "minimumProductsWithCharacteristic" |
      "minimumSupportRatio" | "maximumAlternativeValues" | "maximumCandidatePoolSize">,
    step = "1",
  ) => (
    <label className="grid gap-1 text-sm">
      <span>{label}</span>
      <input
        className="rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] px-3 py-2"
        type="number"
        step={step}
        value={currentSettings[key]}
        onChange={(event) => setSettings({ ...currentSettings, [key]: Number(event.target.value) })}
      />
    </label>
  );

  return (
    <section className="grid gap-4 rounded-xl border border-[var(--app-border)] p-4">
      <div>
        <h3 className="font-semibold">Настройки предложений по похожим товарам</h3>
        <p className="mt-1 text-sm text-[var(--app-muted)]">
          Сейчас используется: {active.name}{active.versionNumber ? ` · версия ${active.versionNumber}` : ""}.
          Черновик не меняет импорт и распознавание, пока не пройдёт оценку и выпуск.
        </p>
      </div>

      <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-5">
        {numberField("Минимум подтверждений", "minimumSupportingProducts")}
        {numberField("Товаров с характеристикой", "minimumProductsWithCharacteristic")}
        {numberField("Минимальное согласие", "minimumSupportRatio", "0.01")}
        {numberField("Вариантов в отчёте", "maximumAlternativeValues")}
        {numberField("Размер выборки", "maximumCandidatePoolSize")}
      </div>

      <label className="grid gap-1 text-sm">
        <span>Название версии</span>
        <input
          className="rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] px-3 py-2"
          value={name}
          onChange={(event) => setName(event.target.value)}
        />
      </label>

      <div className="grid gap-2">
        <h4 className="font-medium">Режимы характеристик</h4>
        <p className="text-sm text-[var(--app-muted)]">
          Off отключает предложение, Shadow только считает результат, Suggest показывает его пользователю. AutoFill начинает заполнять только пустые поля после оценки и выпуска версии. Номиналы конкретной модели защищены от AutoFill на сервере.
        </p>
        {characteristics.data?.map((item) => {
          const current = currentSettings.characteristics[item.code]?.mode ?? "Suggest";
          const isVariantCharacteristic = isVariantAnalogyCharacteristic(item.code);
          return (
            <div key={item.id} className="grid items-end gap-2 rounded-xl border border-[var(--app-border)] p-3 lg:grid-cols-[1fr_190px_180px_180px]">
              <span>{item.name} · {item.code}</span>
              <AppSelect
                ariaLabel={`Режим ${item.name}`}
                value={current}
                options={[
                  { value: "Off", label: "Выключено" },
                  { value: "Shadow", label: "Только наблюдение" },
                  { value: "Suggest", label: "Предлагать" },
                  {
                    value: "AutoFill",
                    label: isVariantCharacteristic
                      ? "Автозаполнение запрещено для номинала"
                      : "Автозаполнение после выпуска",
                    disabled: isVariantCharacteristic,
                  },
                ]}
                onChange={(mode) => setSettings({
                  ...currentSettings,
                  characteristics: {
                    ...currentSettings.characteristics,
                    [item.code]: {
                      ...(currentSettings.characteristics[item.code] ?? {
                        minimumSupportingProducts: null,
                        minimumSupportRatio: null,
                      }),
                      mode: mode as AnalogyMode,
                    },
                  },
                })}
              />
              {isVariantCharacteristic && (
                <p className="text-xs text-amber-200 lg:col-span-4">
                  Это параметр конкретной модели. Аналоги могут показать предложение, но не заполняют его автоматически.
                </p>
              )}
              <label className="grid gap-1 text-xs">
                <span>Минимум подтверждений</span>
                <input
                  className="rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] px-3 py-2"
                  type="number"
                  placeholder={`${currentSettings.minimumSupportingProducts}`}
                  value={currentSettings.characteristics[item.code]?.minimumSupportingProducts ?? ""}
                  onChange={(event) => {
                    const existing = currentSettings.characteristics[item.code];
                    setSettings({
                      ...currentSettings,
                      characteristics: {
                        ...currentSettings.characteristics,
                        [item.code]: {
                          mode: existing?.mode ?? "Suggest",
                          minimumSupportRatio: existing?.minimumSupportRatio ?? null,
                          minimumSupportingProducts: event.target.value
                            ? Number(event.target.value)
                            : null,
                        },
                      },
                    });
                  }}
                />
              </label>
              <label className="grid gap-1 text-xs">
                <span>Минимальное согласие</span>
                <input
                  className="rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] px-3 py-2"
                  type="number"
                  step="0.01"
                  placeholder={`${currentSettings.minimumSupportRatio}`}
                  value={currentSettings.characteristics[item.code]?.minimumSupportRatio ?? ""}
                  onChange={(event) => {
                    const existing = currentSettings.characteristics[item.code];
                    setSettings({
                      ...currentSettings,
                      characteristics: {
                        ...currentSettings.characteristics,
                        [item.code]: {
                          mode: existing?.mode ?? "Suggest",
                          minimumSupportingProducts: existing?.minimumSupportingProducts ?? null,
                          minimumSupportRatio: event.target.value
                            ? Number(event.target.value)
                            : null,
                        },
                      },
                    });
                  }}
                />
              </label>
            </div>
          );
        })}
      </div>

      <ErrorNotice error={save.error} />
      <AppButton loading={save.isPending} onClick={() => save.mutate()}>
        {editing ? "Сохранить черновик" : "Создать новую версию-черновик"}
      </AppButton>

      {query.data.versions.length > 0 && (
        <div className="grid gap-2">
          <h4 className="font-medium">Сохранённые версии</h4>
          {query.data.versions.map((version) => (
            <AnalogyVersionReleaseCard
              key={version.id}
              version={version}
              scope={scope}
              onEdit={() => {
                setEditing(version);
                setName(version.name);
                setSettings(structuredClone(version.settings));
              }}
            />
          ))}
        </div>
      )}
    </section>
  );
}

function AnalogyVersionReleaseCard({
  version,
  scope,
  onEdit,
}: {
  version: AnalogyConfiguration;
  scope: Scope;
  onEdit: () => void;
}) {
  const client = useQueryClient();
  const [confirmed, setConfirmed] = useState(false);
  const reports = useQuery({
    queryKey: ["catalog-analogy-evaluation-reports", version.id],
    queryFn: async ({ signal }) =>
      (await httpClient.get<AnalogyEvaluationReport[]>(
        `/api/catalog/products/analogies/configurations/${version.id}/evaluation-reports`,
        { signal },
      )).data,
    enabled: Boolean(version.id),
    retry: false,
  });
  const refresh = async () => {
    await Promise.all([
      client.invalidateQueries({ queryKey: ["catalog-analogy-configurations", scope] }),
      client.invalidateQueries({ queryKey: ["catalog-analogy-evaluation-reports", version.id] }),
    ]);
  };
  const evaluate = useMutation({
    mutationFn: () => httpClient.post(
      `/api/catalog/products/analogies/configurations/${version.id}/evaluation`,
    ),
    onSettled: refresh,
  });
  const clone = useMutation({
    mutationFn: () => httpClient.post(
      `/api/catalog/products/analogies/configurations/${version.id}/clone`,
    ),
    onSettled: refresh,
  });
  const latest = reports.data?.[0];
  const activate = useMutation({
    mutationFn: () => httpClient.post(
      `/api/catalog/products/analogies/configurations/${version.id}/activate`,
      {
        reportId: latest?.id,
        expectedVersion: version.version,
        confirmed,
      },
    ),
    onSuccess: async () => {
      setConfirmed(false);
      await refresh();
    },
  });

  return (
    <article className="grid gap-3 rounded-xl border border-[var(--app-border)] p-3">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <span>Версия {version.versionNumber}: {version.name} · {version.status}</span>
        <div className="flex flex-wrap gap-2">
          {version.status === "Draft" && (
            <AppButton variant="secondary" onClick={onEdit}>Редактировать</AppButton>
          )}
          {(version.status === "Draft" || version.status === "Evaluated") && (
            <AppButton loading={evaluate.isPending} onClick={() => evaluate.mutate()}>
              {version.status === "Evaluated" ? "Оценить повторно" : "Оценить версию"}
            </AppButton>
          )}
          {(version.status === "Active" || version.status === "Superseded") && (
            <AppButton variant="secondary" loading={clone.isPending} onClick={() => clone.mutate()}>
              Создать новый черновик на основе
            </AppButton>
          )}
        </div>
      </div>
      <ErrorNotice error={evaluate.error ?? clone.error ?? reports.error ?? activate.error} />
      {latest && (
        <div className="grid gap-2 rounded-xl bg-[var(--app-surface)] p-3 text-sm">
          <p>
            Отчёт от {new Date(latest.createdAtUtc).toLocaleString("ru-RU")} ·
            товаров: {latest.controlProducts}; значений: {latest.controlValues};
            улучшений: {latest.improvements}; регрессий: {latest.regressions}.
          </p>
          <p>
            Правильных предложений: {latest.candidateCorrect}; неправильных: {latest.candidateIncorrect}.
            {latest.isCurrent ? " Отчёт актуален." : " Отчёт устарел."}
          </p>
          {latest.blockingReasons.map((reason) => (
            <p key={reason} className="text-amber-300">{reason}</p>
          ))}
          <AnalogyEvaluationDetails reportId={latest.id} />
          {version.status === "Evaluated" && latest.isReady && latest.isCurrent && (
            <>
              <label className="flex items-center gap-2">
                <input
                  type="checkbox"
                  checked={confirmed}
                  onChange={(event) => setConfirmed(event.target.checked)}
                />
                Результаты проверены. Подтверждаю замену действующей версии.
              </label>
              <AppButton
                loading={activate.isPending}
                disabled={!confirmed || version.version == null}
                onClick={() => activate.mutate()}
              >
                Включить эту версию
              </AppButton>
            </>
          )}
        </div>
      )}
    </article>
  );
}

function AnalogyEvaluationDetails({ reportId }: { reportId: string }) {
  const [open, setOpen] = useState(false);
  const query = useQuery({
    queryKey: ["catalog-analogy-evaluation-report", reportId],
    queryFn: async ({ signal }) =>
      (await httpClient.get<AnalogyEvaluationReportDetail>(
        `/api/catalog/products/analogies/configurations/evaluation-reports/${reportId}`,
        { signal },
      )).data,
    enabled: open,
    retry: false,
  });
  return (
    <details onToggle={(event) => setOpen(event.currentTarget.open)}>
      <summary className="cursor-pointer font-medium">Показать контрольные результаты</summary>
      <div className="mt-2 grid gap-2">
        {query.isPending && <p>Загружаем сохранённый снимок…</p>}
        <ErrorNotice error={query.error} />
        {query.data?.rows.slice(0, 50).map((row) => (
          <div key={`${row.productId}-${row.characteristicCode}`} className="rounded-lg border border-[var(--app-border)] p-2">
            <p>{row.productName} · {row.characteristicCode}</p>
            <p className="text-[var(--app-muted)]">
              Ожидалось: {row.expectedValue}; было: {row.baselineValue ?? "нет предложения"}; стало: {row.candidateValue ?? "нет предложения"}.
            </p>
          </div>
        ))}
        {query.data && query.data.rows.length > 50 && (
          <p>Показаны первые 50 из {query.data.rows.length} результатов.</p>
        )}
      </div>
    </details>
  );
}

function MetricCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4">
      <p className="text-xs text-[var(--app-muted)]">{label}</p>
      <p className="mt-1 text-2xl font-semibold">{value}</p>
    </div>
  );
}

function ObjectList({
  scope,
  section,
  navigate,
  parentId,
}: {
  scope: Scope;
  section: string;
  navigate: Navigate;
  parentId?: string;
}) {
  const params = useSearchParams();
  const page = Number(params.get("page") ?? 1);
  const query = useWorkspace<Page<Item>>(scope, section, { page, parentId });
  return (
    <section className="grid gap-3">
      <ErrorNotice error={query.error} />
      {query.isFetching && <p>Загрузка списка…</p>}
      {query.data && !query.isError && (
        <>
          {query.data.items.length === 0 && (
            <p>Объектов по выбранным условиям нет.</p>
          )}
          {query.data.items.map((x) => (
            <article key={x.id} className="border rounded-lg p-3 grid gap-2">
              <p>
                {x.label}
                {x.number ? ` · версия ${x.number}` : ""} ·{" "}
                {new Date(x.createdAtUtc).toLocaleString("ru-RU")}
              </p>
              {x.kind === "rules" && !x.batchAvailable && (
                <p>
                  Исходный импорт недоступен. История сохранена; для выпуска
                  выберите другой пакет и создайте новую оценку.
                </p>
              )}
              <AppButton
                variant="secondary"
                onClick={() => navigate({ id: x.id, section })}
              >
                Открыть
              </AppButton>
            </article>
          ))}
          <Pager
            page={page}
            total={query.data.totalCount}
            busy={query.isFetching}
            change={(p) => navigate({ page: String(p), id: undefined })}
          />
        </>
      )}
    </section>
  );
}
function BatchPicker({
  scope,
  navigate,
}: {
  scope: Scope;
  navigate: Navigate;
}) {
  const params = useSearchParams();
  const page = Number(params.get("batchPage") ?? 1);
  const batchId = params.get("batchId") ?? "";
  const query = useWorkspace<Page<Item>>(scope, "batches", { page });
  const selected = useWorkspace<Page<Item>>(
    scope,
    "batches",
    { id: batchId },
    Boolean(batchId),
  );
  const returnTo = `/catalog/recognition/learning?${params}`;
  return (
    <section className="border rounded-xl p-4 grid gap-3">
      <h3>Мой пакет импорта для разметки и оценки правил</h3>
      <p>
        Пакеты с 1–2000 строками, включая смешанные области. Окончательную
        применимость проверяет оценка.
      </p>
      <ErrorNotice error={query.error ?? selected.error} />
      {query.isFetching && <p>Загрузка пакетов…</p>}
      {selected.data && !selected.isError && (
        <p>
          Выбран: {selected.data.items[0]?.label}.{" "}
          <Link
            href={`/catalog/imports/${batchId}?learningReturn=${encodeURIComponent(returnTo)}`}
          >
            Открыть этот импорт
          </Link>
        </p>
      )}
      {query.data && !query.isError && (
        <>
          {query.data.items.length === 0 && (
            <p>Доступных пакетов нет. Загрузите и подготовьте импорт.</p>
          )}
          {query.data.items.map((x) => (
            <AppButton
              key={x.id}
              variant="secondary"
              onClick={() => navigate({ batchId: x.id })}
            >
              {x.id === batchId ? "Выбран: " : "Выбрать: "}
              {x.label}
            </AppButton>
          ))}
          <Pager
            page={page}
            total={query.data.totalCount}
            busy={query.isFetching}
            change={(p) => navigate({ batchPage: String(p) })}
          />
        </>
      )}
      <Link
        href={`/catalog/imports?learningReturn=${encodeURIComponent(returnTo)}`}
      >
        Открыть мои импорты
      </Link>
    </section>
  );
}

function Rules({
  scope,
  overview,
  navigate,
}: {
  scope: Scope;
  overview: Overview;
  navigate: Navigate;
}) {
  const params = useSearchParams();
  const section = params.get("section") ?? "versions";
  const id = params.get("id") ?? "";
  const batchId = params.get("batchId") ?? "";
  const user = useAuthSession();
  const characteristics = useQuery({
    queryKey: ["workspace-characteristics", overview.productTypeCode],
    queryFn: () =>
      getCatalogProductTypeCharacteristics(overview.productTypeCode),
    enabled: ["prepare", "compose", "multi"].includes(section),
  });
  const batch = useWorkspace<Page<Item>>(
    scope,
    "batches",
    { id: batchId },
    Boolean(batchId),
  );
  const version = useQuery({
    queryKey: ["workspace-version-detail", id],
    queryFn: ({ signal }) => getRecognitionRuleSetVersion(id, signal),
    enabled: Boolean(id) && section === "versions",
  });
  const [characteristic, setCharacteristic] = useState("");
  const choices = [
    ["prepare", "1. Подготовить черновики"],
    ["compose", "2. Составить версию"],
    ["versions", "3. Версии и выпуск"],
    ["literal", "Точные черновики"],
    ["integer", "Числовые черновики"],
    ["multi", "Составные черновики"],
  ];
  return (
    <section className="grid gap-4">
      <p>
        Здесь показаны черновики и версии правил выбранного производителя и типа
        товара. Основания проверяются для конкретного выбранного объекта;
        отсутствие загруженной проверки не означает, что он актуален.
      </p>

      <div className="flex flex-wrap gap-2">
        {choices.map(([value, label]) => (
          <AppButton
            key={value}
            variant={section === value ? "primary" : "secondary"}
            onClick={() =>
              navigate({ section: value, id: undefined, page: undefined })
            }
          >
            {label}
          </AppButton>
        ))}
      </div>
      {["versions", "literal", "integer", "multi"].includes(section) && (
        <ObjectList scope={scope} section={section} navigate={navigate} />
      )}
      {id && section === "literal" && (
        <CatalogRecognitionLiteralDraftRecheckPanel key={id} draftId={id} />
      )}
      {id && section === "integer" && (
        <CatalogRecognitionIntegerDraftRecheckPanel key={id} draftId={id} />
      )}
      {id && section === "multi" && characteristics.data && (
        <CatalogRecognitionMultiIntegerDraftRecheckPanel
          key={id}
          draftId={id}
          characteristics={characteristics.data}
          disabled={false}
        />
      )}
      <ErrorNotice error={characteristics.error ?? version.error} />
      {section === "prepare" && (
        <>
          <BatchPicker scope={scope} navigate={navigate} />
          <p>
            Генерация и сохранение черновиков доступны без исходного импорта.
            Для проверки на строках и выпуска правил выберите доступный пакет.
          </p>
          {characteristics.isPending && <p>Загрузка характеристик…</p>}
          {characteristics.data && (
            <>
              <AppSelect
                ariaLabel="Характеристика для подготовки"
                value={characteristic}
                options={[
                  { value: "", label: "Выберите характеристику" },
                  ...characteristics.data.map((x) => ({
                    value: x.id,
                    label: x.name,
                  })),
                ]}
                onChange={setCharacteristic}
              />
              {characteristic && (
                <CatalogRecognitionLiteralProposalPreview
                  key={`${batchId}:${characteristic}`}
                  {...scope}
                  batchId={batch.data && !batch.isError ? batchId : ""}
                  characteristicDefinitionId={characteristic}
                  characteristicName={
                    characteristics.data.find((x) => x.id === characteristic)
                      ?.name ?? ""
                  }
                  disabled={false}
                />
              )}
              <details>
                <summary>Составные числовые шаблоны</summary>
                <CatalogRecognitionMultiIntegerPreview
                  key={batchId}
                  {...scope}
                  batchId={batch.data && !batch.isError ? batchId : ""}
                  characteristics={characteristics.data}
                  disabled={false}
                  hideVersionManagement
                />
              </details>
            </>
          )}
        </>
      )}
      {section === "compose" && characteristics.data && (
        <CatalogRecognitionRuleSetVersionCreate
          {...scope}
          characteristics={characteristics.data}
          disabled={false}
          onOpenVersion={(id) =>
            navigate({ section: "versions", id, page: undefined })
          }
        />
      )}
      {section === "versions" && id && (
        <section className="grid gap-5 rounded-xl border border-[var(--app-border)] bg-[var(--app-panel)] p-4">
          {version.isPending && (
            <p role="status">Загружаем выбранную версию…</p>
          )}

          {version.data && (
            <>
              <header>
                <p className="text-sm text-[var(--app-muted)]">
                  Выбрана версия правил №{version.data.versionNumber}
                </p>

                <h3 className="mt-1 text-lg font-semibold">
                  {version.data.name}
                </h3>

                <p className="mt-2 text-sm text-[var(--app-muted)]">
                  Создана{" "}
                  {new Date(version.data.createdAtUtc).toLocaleString("ru-RU")}.
                  Открытие версии не включает её автоматически.
                </p>
              </header>

              <section className="grid gap-3">
                <div>
                  <h4 className="font-semibold">Состав версии</h4>
                  <p className="mt-1 text-sm text-[var(--app-muted)]">
                    Версия содержит сохранённые черновики правил. Порядок
                    элементов не задаёт приоритет распознавания.
                  </p>
                </div>

                {version.data.entries.length === 0 ? (
                  <p className="rounded-xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-3 text-sm text-[var(--app-danger)]">
                    В версии нет правил. Такую версию нельзя считать готовой к
                    оценке и включению.
                  </p>
                ) : (
                  <div className="grid gap-2">
                    {version.data.entries.map((entry, index) => (
                      <article
                        key={entry.position}
                        className="rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] p-3"
                      >
                        <p className="font-medium">
                          {index + 1}. {ruleKindName(entry.kind)}
                        </p>

                        <details className="mt-2 text-xs text-[var(--app-muted)]">
                          <summary className="cursor-pointer">
                            Технические сведения
                          </summary>

                          <p className="mt-2 break-all">
                            Идентификатор черновика: {entry.draftId}
                          </p>
                        </details>
                      </article>
                    ))}
                  </div>
                )}
              </section>

              <section className="grid gap-2">
                <h4 className="font-semibold">Проверить отдельное название</h4>

                <p className="text-sm text-[var(--app-muted)]">
                  Эта проверка показывает, как выбранная версия распознает
                  конкретное название. Она ничего не сохраняет и не включает
                  правила.
                </p>

                <CatalogRecognitionRuleSetNamePreview
                  key={id}
                  {...scope}
                  versionId={id}
                  disabled={false}
                />
              </section>

              <section className="grid gap-2">
                <h4 className="font-semibold">
                  Сохранённые оценки этой версии
                </h4>

                <p className="text-sm text-[var(--app-muted)]">
                  Сохранённый отчёт показывает исторический результат. Перед
                  включением сервер отдельно проверяет, что отчёт по-прежнему
                  актуален.
                </p>

                <VersionReports
                  scope={scope}
                  versionId={id}
                  navigate={navigate}
                />
              </section>

              <section className="grid gap-3">
                <div>
                  <h4 className="font-semibold">Оценка и включение</h4>

                  <p className="mt-1 text-sm text-[var(--app-muted)]">
                    Выберите пакет импорта для сравнительной оценки. Изменение
                    включается только после успешной оценки и отдельного
                    подтверждения.
                  </p>
                </div>

                <BatchPicker scope={scope} navigate={navigate} />

                {batchId && batch.data && !batch.isError && user?.userId ? (
                  <CatalogRecognitionRuleSetActivation
                    key={`${id}:${batchId}`}
                    {...scope}
                    userId={user.userId}
                    batchId={batchId}
                    versionId={id}
                    disabled={false}
                  />
                ) : (
                  <p className="rounded-xl border border-[var(--app-accent-border)] bg-[var(--app-accent-soft)] p-3 text-sm text-[var(--app-muted)]">
                    Для оценки и включения этой версии выберите доступный пакет
                    импорта. Выбор другого пакета потребует новой оценки.
                  </p>
                )}
              </section>
            </>
          )}
        </section>
      )}
    </section>
  );
}
function VersionReports({
  scope,
  versionId,
  navigate,
}: {
  scope: Scope;
  versionId: string;
  navigate: Navigate;
}) {
  return (
    <ObjectList
      scope={scope}
      section="ruleReports"
      parentId={versionId}
      navigate={(values) => navigate({ ...values, branch: "reports" })}
    />
  );
}
function Dictionary({ scope, navigate }: { scope: Scope; navigate: Navigate }) {
  const params = useSearchParams();
  const id = params.get("id") ?? "";
  const status = params.get("status") ?? "Pending";
  const page = Number(params.get("page") ?? 1);

  const query = useWorkspace<Page<AssistantDictionarySuggestion>>(
    scope,
    "suggestions",
    { page, status },
  );

  const selected = useWorkspace<Page<AssistantDictionarySuggestion>>(
    scope,
    "suggestions",
    { id },
    Boolean(id),
  );

  const selectedSuggestion = selected.data?.items[0];

  return (
    <section className="grid gap-5">
      <header>
        <h3 className="text-lg font-semibold">Учебные предложения словаря</h3>

        <p className="mt-1 text-sm text-[var(--app-muted)]">
          Здесь накопленные наблюдения превращаются в словарные решения для
          выбранного производителя и типа товара. Предложение не начинает
          действовать автоматически.
        </p>

        <p className="mt-2 text-sm text-[var(--app-muted)]">
          У словаря нет общих версий. Каждое учебное предложение отдельно
          проходит проверку влияния и после одобрения создаёт самостоятельный
          действующий термин словаря.
        </p>
      </header>

      <div className="grid gap-3 md:grid-cols-3">
        <article className="rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4">
          <p className="text-xs font-semibold text-[var(--app-accent)]">
            Шаг 1
          </p>
          <h4 className="mt-1 font-semibold">Выбрать предложение</h4>
          <p className="mt-2 text-sm text-[var(--app-muted)]">
            Система объединяет повторяющиеся наблюдения и предлагает обозначение
            и правильное значение.
          </p>
        </article>

        <article className="rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4">
          <p className="text-xs font-semibold text-[var(--app-accent)]">
            Шаг 2
          </p>
          <h4 className="mt-1 font-semibold">Проверить влияние</h4>
          <p className="mt-2 text-sm text-[var(--app-muted)]">
            Перед одобрением проверяются основания и сравнивается качество
            распознавания до и после изменения.
          </p>
        </article>

        <article className="rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4">
          <p className="text-xs font-semibold text-[var(--app-accent)]">
            Шаг 3
          </p>
          <h4 className="mt-1 font-semibold">Принять решение</h4>
          <p className="mt-2 text-sm text-[var(--app-muted)]">
            Одобрение создаёт действующий термин словаря. Отклонение сохраняет
            решение без создания термина.
          </p>
        </article>
      </div>

      <section className="grid gap-3 rounded-xl border border-[var(--app-border)] p-4">
        <div>
          <h4 className="font-semibold">Выбор предложения</h4>

          <p className="mt-1 text-sm text-[var(--app-muted)]">
            Изменение оснований может потребовать пересмотра уже выпущенного
            термина, но не отключает его автоматически.
          </p>
        </div>

        <AppSelect
          ariaLabel="Состояние учебных предложений"
          value={status}
          options={[
            { value: "Pending", label: "Ожидают решения" },
            { value: "Approved", label: "Одобрены" },
            { value: "Rejected", label: "Отклонены" },
          ]}
          onChange={(value) =>
            navigate({
              status: value,
              page: undefined,
              id: undefined,
            })
          }
        />

        <ErrorNotice error={query.error ?? selected.error} />

        {query.isFetching && <p role="status">Загрузка предложений…</p>}

        {query.data && !query.isError && (
          <>
            <p className="text-sm text-[var(--app-muted)]">
              Найдено предложений: {query.data.totalCount}.
            </p>

            {query.data.items.length === 0 ? (
              <div className="rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4 text-sm text-[var(--app-muted)]">
                {status === "Pending" ? (
                  <>
                    <p className="font-medium text-[var(--app-text)]">
                      Учебных предложений пока нет
                    </p>

                    <p className="mt-2">
                      Предложение создаётся фоновым обучением после накопления
                      достаточного количества повторяющихся исправлений для
                      одной закономерности. Создать учебное предложение вручную
                      в этой ветке нельзя.
                    </p>

                    <p className="mt-2">
                      Продолжайте исправлять и подтверждать примеры в импорте.
                      Когда оснований станет достаточно, предложение появится
                      здесь автоматически.
                    </p>
                  </>
                ) : (
                  <p>Предложений в выбранном состоянии нет.</p>
                )}
              </div>
            ) : (
              <div className="grid gap-2">
                {query.data.items.map((suggestion) => (
                  <article
                    key={suggestion.id}
                    className="grid gap-2 rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] p-3"
                  >
                    <p className="font-medium">
                      «{suggestion.unknownPhrase}» →{" "}
                      {suggestion.suggestedTargetValue}
                    </p>

                    <p className="text-sm text-[var(--app-muted)]">
                      Наблюдений: {suggestion.occurrenceCount}. Характеристика:{" "}
                      {suggestion.characteristicName ??
                        suggestion.characteristicCode ??
                        "не указана"}
                      .
                    </p>

                    <AppButton
                      variant={id === suggestion.id ? "primary" : "secondary"}
                      onClick={() => navigate({ id: suggestion.id })}
                    >
                      {id === suggestion.id
                        ? "Предложение открыто"
                        : "Открыть предложение"}
                    </AppButton>
                  </article>
                ))}
              </div>
            )}

            <Pager
              page={page}
              total={query.data.totalCount}
              busy={query.isFetching}
              change={(nextPage) =>
                navigate({
                  page: String(nextPage),
                  id: undefined,
                })
              }
            />
          </>
        )}
      </section>

      {id && selected.isFetching && (
        <p role="status">Загружаем выбранное предложение…</p>
      )}

      {selectedSuggestion && !selected.isError && (
        <section className="grid gap-4 rounded-xl border border-[var(--app-accent-border)] bg-[var(--app-panel)] p-4">
          <div className="flex flex-col justify-between gap-3 sm:flex-row sm:items-start">
            <div>
              <h4 className="font-semibold">Выбранное предложение</h4>
              <p className="mt-1 text-sm text-[var(--app-muted)]">
                Проверьте окончательное решение, основания и результат оценки.
                Затем явно одобрите или отклоните предложение.
              </p>
            </div>

            <AppButton
              variant="secondary"
              onClick={() => navigate({ id: undefined })}
            >
              Закрыть предложение
            </AppButton>
          </div>

          <DictionaryDecision key={id} suggestion={selectedSuggestion} />

          <section className="grid gap-2">
            <h4 className="font-semibold">Сохранённые оценки предложения</h4>

            <p className="text-sm text-[var(--app-muted)]">
              Эти отчёты сохраняют историю проверок. Для одобрения подходит
              только актуальная оценка текущего решения и текущих оснований.
            </p>

            <ObjectList
              scope={scope}
              section="dictionaryReports"
              parentId={id}
              navigate={(values) => navigate({ ...values, branch: "reports" })}
            />
          </section>
        </section>
      )}

      <section className="rounded-xl border border-[var(--app-border)] p-4">
        <h4 className="font-semibold">
          Другие источники словарных предложений
        </h4>

        <p className="mt-1 text-sm text-[var(--app-muted)]">
          Ручные предложения и предложения других механизмов не относятся к
          этому учебному циклу и управляются отдельно.
        </p>

        <Link
          href="/catalog/assistant-suggestions"
          className="mt-3 inline-flex text-sm text-[var(--app-accent)] hover:underline"
        >
          Открыть все предложения и ручное администрирование
        </Link>
      </section>
    </section>
  );
}
function DictionaryDecision({
  suggestion,
}: {
  suggestion: AssistantDictionarySuggestion;
}) {
  const [comment, setComment] = useState("");
  const approve = useMutation({ mutationFn: approveDictionarySuggestion });
  const reject = useMutation({ mutationFn: rejectDictionarySuggestion });
  return (
    <>
      <ErrorNotice error={approve.error ?? reject.error} />
      <SuggestionCard
        suggestion={suggestion}
        reviewComment={comment}
        onReviewCommentChange={setComment}
        onApprove={(request) =>
          approve.mutate({ suggestionId: suggestion.id, request })
        }
        onReject={() =>
          reject.mutate({
            suggestionId: suggestion.id,
            request: { reviewComment: comment },
          })
        }
        isReviewPending={approve.isPending || reject.isPending}
      />
    </>
  );
}
function Reports({ scope, navigate }: { scope: Scope; navigate: Navigate }) {
  const params = useSearchParams();
  const section =
    params.get("section") === "dictionaryReports"
      ? "dictionaryReports"
      : "ruleReports";
  const id = params.get("id") ?? "";
  const detail = useWorkspace<Page<Item>>(scope, section, { id }, Boolean(id));
  const item = detail.data?.items[0];
  return (
    <section className="grid gap-4">
      <AppSelect
        ariaLabel="Ветка отчётов"
        value={section}
        options={[
          { value: "ruleReports", label: "Правила" },
          { value: "dictionaryReports", label: "Словарь" },
        ]}
        onChange={(value) =>
          navigate({ section: value, id: undefined, page: undefined })
        }
      />
      <p>
        Список показывает сохранённую историю. Актуальность не проверена;
        открытие выбранного отчёта запрашивает текущую серверную готовность.
      </p>
      <ObjectList scope={scope} section={section} navigate={navigate} />
      {item && (
        <>
          <EvaluationReportPanel
            key={id}
            reportId={id}
            kind={section === "ruleReports" ? "rules" : "dictionary"}
          />
          <AppButton
            disabled={!item.parentId}
            onClick={() =>
              navigate({
                branch: section === "ruleReports" ? "rules" : "dictionary",
                section: section === "ruleReports" ? "versions" : undefined,
                id: item.parentId,
                batchId: item.batchAvailable ? item.batchId : undefined,
                page: undefined,
              })
            }
          >
            {section === "ruleReports"
              ? "Вернуться к оцениваемой версии правил"
              : "Вернуться к оцениваемому предложению словаря"}
          </AppButton>
        </>
      )}
    </section>
  );
}
function CurrentConfiguration({
  scope,
  overview,
  navigate,
}: {
  scope: Scope;
  overview: Overview;
  navigate: Navigate;
}) {
  const [reason, setReason] = useState("");
  const [confirmed, setConfirmed] = useState(false);
  const [name, setName] = useState("");
  const client = useQueryClient();
  const disable = useMutation({
    mutationFn: () =>
      switchRecognitionRuleSet({
        ...scope,
        expectedSequenceNumber: overview.sequenceNumber,
        newVersionId: null,
        reportId: null,
        reason,
        confirmed,
      }),
    onSettled: () =>
      client.invalidateQueries({ queryKey: ["learning-workspace"] }),
  });
  const check = useMutation({
    mutationFn: () =>
      checkRecognitionRuleSetTraining(overview.activeVersionId!),
  });
  const preview = useMutation({
    mutationFn: () =>
      previewCatalogProductNameRecognition({
        productName: name,
        manufacturerId: scope.manufacturerId,
        productTypeCode: overview.productTypeCode,
      }),
  });
  return (
    <section className="grid gap-4">
      <h3>Действующая конфигурация</h3>
      <p>
        Версия правил:{" "}
        {overview.activeVersionId ?? "отсутствует, используется baseline"}.
        Sequence: {overview.sequenceNumber}.
      </p>
      <p>
        Основания не проверены автоматически. Изменение оснований означает
        необходимость пересмотра, а не доказанную ошибку распознавания.
      </p>
      {overview.activeVersionId && (
        <>
          <AppButton
            onClick={() =>
              navigate({
                branch: "rules",
                section: "versions",
                id: overview.activeVersionId!,
              })
            }
          >
            Открыть действующую версию
          </AppButton>
          <AppButton
            variant="secondary"
            disabled={check.isPending}
            onClick={() => check.mutate()}
          >
            Проверить основания действующих правил
          </AppButton>
          {check.data && (
            <>
              <p>
                {check.data.passedTrainingChecks
                  ? "Проверка учебных оснований пройдена"
                  : "Требуется пересмотр оснований"}
              </p>
              {check.data.items.map((x) => (
                <p key={`${x.ruleKind}:${x.draftId}`}>{x.message}</p>
              ))}
            </>
          )}
          <details>
            <summary>Явно отключить правила до baseline</summary>
            <label>
              Причина
              <textarea
                className="block w-full bg-transparent border"
                value={reason}
                maxLength={1000}
                onChange={(e) => setReason(e.target.value)}
              />
            </label>
            <label>
              <input
                type="checkbox"
                checked={confirmed}
                onChange={(e) => setConfirmed(e.target.checked)}
              />{" "}
              Подтверждаю отключение версии с sequence {overview.sequenceNumber}
            </label>
            <AppButton
              disabled={!reason.trim() || !confirmed || disable.isPending}
              onClick={() => disable.mutate()}
            >
              Отключить правила
            </AppButton>
          </details>
        </>
      )}
      <ErrorNotice error={disable.error ?? check.error ?? preview.error} />
      <AppButton
        variant="secondary"
        onClick={() =>
          navigate({ branch: "dictionary", status: "Approved", id: undefined })
        }
      >
        Проверить одобренные словарные предложения и их происхождение
      </AppButton>
      <p>
        У старых и ручных одобрений может не быть отчёта оценки. Возврат к
        прежней версии правил требует новой оценки.
      </p>
      <label>
        Проверить новое распознавание
        <input
          className="block w-full bg-transparent border p-2"
          value={name}
          onChange={(e) => setName(e.target.value)}
        />
      </label>
      <AppButton
        disabled={!name.trim() || preview.isPending}
        onClick={() => preview.mutate()}
      >
        Распознать текущей конфигурацией
      </AppButton>
      {preview.data && (
        <div className="grid gap-2">
          <p>
            Использованная версия:{" "}
            {preview.data.activeRuleSetVersionId ?? "baseline"}; sequence:{" "}
            {preview.data.activeRuleSetSequenceNumber ?? 0}.
          </p>
          {preview.data.characteristics.map((x) => (
            <p key={x.characteristicCode}>
              {x.characteristicCode}: {x.rawValue} → {x.normalizedValue}
            </p>
          ))}
          {preview.data.characteristics.length === 0 && (
            <p>Значения не распознаны.</p>
          )}
          {preview.data.conflicts.map((x) => (
            <p key={x.characteristicCode}>
              Конфликт {x.characteristicCode}:{" "}
              {x.candidates.map((c) => c.normalizedValue).join(", ")}
            </p>
          ))}
        </div>
      )}
    </section>
  );
}
