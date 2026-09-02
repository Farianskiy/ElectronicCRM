"use client";

import { useRef, useState } from "react";
import { AppButton } from "@/shared/ui/AppButton";
import type {
  AnalyzeCatalogImportBatchResponse,
  CatalogImportManufacturerResolutionGroup,
  CatalogImportManufacturerResolutionSource,
  CatalogImportManufacturerResolutionSummary,
} from "../../model/types";
import { CatalogImportManufacturerAliasCreationForm } from "./CatalogImportManufacturerAliasCreationForm";
import { CatalogImportManufacturerCreationForm } from "./CatalogImportManufacturerCreationForm";
import { CatalogImportManufacturerNoiseForm } from "./CatalogImportManufacturerNoiseForm";

interface CatalogImportManufacturerResolutionPanelProps {
  batchId: string;
  productTypeId?: string | null;
  summary: CatalogImportManufacturerResolutionSummary | null;
  onAnalysisChange: (analysis: AnalyzeCatalogImportBatchResponse) => void;
}

interface ManufacturerSummaryCardProps {
  label: string;
  value: number;
  description: string;
  tone?: "Neutral" | "Success" | "Warning" | "Danger";
}

interface ManufacturerResolutionPresentation {
  label: string;
  explanation: string;
  badgeClassName: string;
  cardClassName: string;
}

function getSummaryCardClassName(
  tone: ManufacturerSummaryCardProps["tone"],
): string {
  switch (tone) {
    case "Success":
      return "border-green-500/25 bg-green-500/[0.06]";

    case "Warning":
      return "border-amber-500/25 bg-amber-500/[0.06]";

    case "Danger":
      return "border-red-500/25 bg-red-500/[0.06]";

    case "Neutral":
    default:
      return "border-[var(--app-border)] bg-[var(--app-surface)]";
  }
}

function getResolutionPresentation(
  source: CatalogImportManufacturerResolutionSource,
): ManufacturerResolutionPresentation {
  switch (source) {
    case "ExactName":
      return {
        label: "Точное имя",
        explanation:
          "Исходное значение совпало с именем Manufacturer после нормализации.",
        badgeClassName:
          "border-green-500/30 bg-green-500/10 text-[var(--app-success)]",
        cardClassName: "border-green-500/20 bg-green-500/[0.03]",
      };

    case "ApprovedAlias":
      return {
        label: "Подтверждённый alias",
        explanation:
          "Исходное значение найдено среди Approved ManufacturerAlias и приведено к каноническому производителю.",
        badgeClassName:
          "border-violet-500/30 bg-violet-500/10 text-[var(--app-role-text)]",
        cardClassName: "border-violet-500/20 bg-violet-500/[0.03]",
      };

    case "IgnoredNoise":
      return {
        label: "Подтверждённый шум",
        explanation:
          "Technical-пользователь подтвердил, что исходное значение не является названием производителя.",
        badgeClassName:
          "border-orange-500/30 bg-orange-500/10 text-[var(--app-warning)]",
        cardClassName: "border-orange-500/20 bg-orange-500/[0.03]",
      };

    case "None":
    default:
      return {
        label: "Не разрешено",
        explanation:
          "Такого точного имени или подтверждённого alias пока нет в справочнике.",
        badgeClassName:
          "border-red-500/30 bg-red-500/10 text-[var(--app-danger)]",
        cardClassName: "border-red-500/20 bg-red-500/[0.03]",
      };
  }
}

function ManufacturerSummaryCard({
  label,
  value,
  description,
  tone = "Neutral",
}: ManufacturerSummaryCardProps) {
  return (
    <div
      className={["rounded-2xl border p-4", getSummaryCardClassName(tone)].join(
        " ",
      )}
    >
      <p className="text-sm text-[var(--app-muted)]">{label}</p>

      <p className="mt-2 text-2xl font-semibold text-[var(--app-text)]">
        {value}
      </p>

      <p className="mt-2 text-xs leading-5 text-[var(--app-muted)]">
        {description}
      </p>
    </div>
  );
}

function ManufacturerResolutionGroupCard({
  batchId,
  productTypeId,
  group,
  onAnalysisChange,
}: {
  batchId: string;
  productTypeId?: string | null;
  group: CatalogImportManufacturerResolutionGroup;
  onAnalysisChange: (analysis: AnalyzeCatalogImportBatchResponse) => void;
}) {
  const presentation = getResolutionPresentation(group.source);
  const isUnresolved = group.status === "Unresolved";
  const isIgnoredNoise = group.status === "IgnoredNoise";

  const resolutionTarget = isIgnoredNoise
    ? "Не является производителем"
    : (group.resolvedManufacturerName ?? "Производитель не определён");

  return (
    <article
      className={["rounded-2xl border p-5", presentation.cardClassName].join(
        " ",
      )}
    >
      <div className="flex flex-col justify-between gap-4 lg:flex-row lg:items-start">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <span
              className={[
                "rounded-full border px-3 py-1 text-xs font-medium",
                presentation.badgeClassName,
              ].join(" ")}
            >
              {presentation.label}
            </span>

            <span className="rounded-full border border-[var(--app-border)] bg-[var(--app-surface)] px-3 py-1 text-xs text-[var(--app-text)]">
              Строк: {group.occurrenceCount}
            </span>
          </div>

          <div className="mt-4 flex flex-wrap items-center gap-3">
            <p className="break-words font-mono text-lg font-semibold text-[var(--app-text)]">
              {group.sourceValue}
            </p>

            <span className="text-[var(--app-muted)]">→</span>

            <p
              className={[
                "break-words font-mono text-lg font-semibold",
                isUnresolved
                  ? "text-[var(--app-danger)]"
                  : "text-[var(--app-accent)]",
              ].join(" ")}
            >
              {resolutionTarget}
            </p>
          </div>

          <p className="mt-3 max-w-4xl text-sm leading-6 text-[var(--app-muted)]">
            {presentation.explanation}
          </p>
        </div>

        <div className="rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] px-4 py-3 lg:text-right">
          <p className="text-xs text-[var(--app-muted)]">Количество строк</p>

          <p className="mt-1 text-2xl font-semibold text-[var(--app-text)]">
            {group.occurrenceCount}
          </p>
        </div>
      </div>

      <dl className="mt-5 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <div className="rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4">
          <dt className="text-xs text-[var(--app-muted)]">
            Исходное значение Excel
          </dt>

          <dd className="mt-2 break-words font-mono text-sm text-[var(--app-text)]">
            {group.sourceValue}
          </dd>
        </div>

        <div className="rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4">
          <dt className="text-xs text-[var(--app-muted)]">
            Нормализованное значение
          </dt>

          <dd className="mt-2 break-words font-mono text-sm text-[var(--app-text)]">
            {group.normalizedSourceValue}
          </dd>
        </div>

        <div className="rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4">
          <dt className="text-xs text-[var(--app-muted)]">
            Итоговый Manufacturer
          </dt>

          <dd
            className={[
              "mt-2 break-words text-sm font-medium",
              isUnresolved
                ? "text-[var(--app-danger)]"
                : "text-[var(--app-success)]",
            ].join(" ")}
          >
            {isIgnoredNoise
              ? "Не требуется: значение признано шумом"
              : (group.resolvedManufacturerName ?? "Не найден")}
          </dd>
        </div>

        <div className="rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4">
          <dt className="text-xs text-[var(--app-muted)]">Источник решения</dt>

          <dd className="mt-2 font-mono text-sm text-[var(--app-text)]">
            {group.source}
          </dd>
        </div>
      </dl>

      <dl className="mt-4 grid gap-4 lg:grid-cols-2">
        <div className="rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4">
          <dt className="text-xs text-[var(--app-muted)]">ManufacturerId</dt>

          <dd className="mt-2 break-all font-mono text-xs text-[var(--app-text)]">
            {group.manufacturerId ?? "Отсутствует"}
          </dd>
        </div>

        <div className="rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4">
          <dt className="text-xs text-[var(--app-muted)]">
            ManufacturerAliasId
          </dt>

          <dd className="mt-2 break-all font-mono text-xs text-[var(--app-text)]">
            {group.manufacturerAliasId ?? "Alias не использовался"}
          </dd>
        </div>
      </dl>

      {isIgnoredNoise && (
        <dl className="mt-4 grid gap-4 lg:grid-cols-2">
          <div className="rounded-xl border border-orange-500/20 bg-orange-500/[0.04] p-4">
            <dt className="text-xs text-[var(--app-warning)]">
              ManufacturerNoisePhraseId
            </dt>

            <dd className="mt-2 break-all font-mono text-xs text-[var(--app-warning)]">
              {group.manufacturerNoisePhraseId ?? "Идентификатор отсутствует"}
            </dd>
          </div>

          <div className="rounded-xl border border-orange-500/20 bg-orange-500/[0.04] p-4">
            <dt className="text-xs text-[var(--app-warning)]">
              Причина решения Technical
            </dt>

            <dd className="mt-2 text-sm leading-6 text-[var(--app-warning)]">
              {group.noiseReason ?? "Причина не указана"}
            </dd>
          </div>
        </dl>
      )}

      <div className="mt-4 grid gap-4 xl:grid-cols-2">
        <div className="rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4">
          <p className="text-xs text-[var(--app-muted)]">Примеры строк Excel</p>

          {group.exampleRowNumbers.length === 0 ? (
            <p className="mt-2 text-sm text-[var(--app-muted)]">
              Номера строк отсутствуют.
            </p>
          ) : (
            <div className="mt-3 flex flex-wrap gap-2">
              {group.exampleRowNumbers.map((rowNumber) => (
                <span
                  key={rowNumber}
                  className="rounded-lg border border-[var(--app-border)] bg-[var(--app-surface)] px-3 py-1 font-mono text-xs text-[var(--app-text)]"
                >
                  Строка {rowNumber}
                </span>
              ))}
            </div>
          )}
        </div>

        <div className="rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4">
          <p className="text-xs text-[var(--app-muted)]">
            Примеры наименований товаров
          </p>

          {group.exampleProductNames.length === 0 ? (
            <p className="mt-2 text-sm text-[var(--app-muted)]">
              Наименования отсутствуют.
            </p>
          ) : (
            <ul className="mt-3 grid gap-2">
              {group.exampleProductNames.map((productName) => (
                <li
                  key={productName}
                  className="rounded-lg border border-[var(--app-border)] bg-[var(--app-surface)] px-3 py-2 font-mono text-xs leading-5 text-[var(--app-text)]"
                >
                  {productName}
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>

      {isUnresolved && (
        <div className="mt-4">
          <div className="rounded-2xl border border-sky-500/25 bg-sky-500/[0.06] p-4">
            <p className="text-sm font-medium text-[var(--app-accent)]">
              Выберите один способ решения
            </p>

            <p className="mt-2 text-sm leading-6 text-[var(--app-accent)]">
              Если бренд уже существует в справочнике, создайте Approved alias.
              Если это действительно новый бренд, создайте нового Manufacturer.
              Если значение вообще не является производителем, пометьте его как
              шум. Выполните только одно из трёх действий.
            </p>
          </div>

          <CatalogImportManufacturerAliasCreationForm
            batchId={batchId}
            productTypeId={productTypeId}
            group={group}
            onAnalysisChange={onAnalysisChange}
          />

          <CatalogImportManufacturerCreationForm
            batchId={batchId}
            productTypeId={productTypeId}
            group={group}
            onAnalysisChange={onAnalysisChange}
          />

          <CatalogImportManufacturerNoiseForm
            batchId={batchId}
            productTypeId={productTypeId}
            group={group}
            onAnalysisChange={onAnalysisChange}
          />
        </div>
      )}
    </article>
  );
}

const manufacturerGroupsPageSize = 6;

function getManufacturerGroupKey(
  group: CatalogImportManufacturerResolutionGroup,
): string {
  return JSON.stringify([
    group.normalizedSourceValue,
    group.status,
    group.source,
    group.manufacturerId,
    group.manufacturerAliasId,
    group.manufacturerNoisePhraseId,
  ]);
}

function ManufacturerGroupsBrowser({
  batchId,
  productTypeId,
  groups,
  onAnalysisChange,
}: {
  batchId: string;
  productTypeId?: string | null;
  groups: CatalogImportManufacturerResolutionGroup[];
  onAnalysisChange: (analysis: AnalyzeCatalogImportBatchResponse) => void;
}) {
  const containerRef = useRef<HTMLDivElement>(null);

  const [navigation, setNavigation] = useState<{
    selected: string | null;
    visited: string[];
    page: number;
  }>({
    selected: null,
    visited: [],
    page: 1,
  });

  const pageCount = Math.max(
    1,
    Math.ceil(groups.length / manufacturerGroupsPageSize),
  );
  const page = Math.min(navigation.page, pageCount);
  const startIndex = (page - 1) * manufacturerGroupsPageSize;

  const visibleGroups = groups.slice(
    startIndex,
    startIndex + manufacturerGroupsPageSize,
  );

  const selectedIndex = groups.findIndex(
    (group) => getManufacturerGroupKey(group) === navigation.selected,
  );
  const selectedGroup = groups[selectedIndex] ?? null;

  function focusBrowser(): void {
    containerRef.current?.focus({ preventScroll: true });
    containerRef.current?.scrollIntoView({
      block: "start",
      behavior: "instant",
    });
  }

  function openGroup(index: number): void {
    const group = groups[index];

    if (!group) {
      return;
    }

    const groupId = getManufacturerGroupKey(group);

    setNavigation((current) => ({
      ...current,
      selected: groupId,
      visited: current.visited.includes(groupId)
        ? current.visited
        : [...current.visited, groupId],
    }));

    focusBrowser();
  }

  function showList(): void {
    setNavigation((current) => ({ ...current, selected: null }));
    focusBrowser();
  }

  function changePage(nextPage: number): void {
    setNavigation((current) => ({
      ...current,
      page: Math.max(1, Math.min(nextPage, pageCount)),
    }));
    focusBrowser();
  }

  function renderNavigation(position: "top" | "bottom") {
    return (
      <nav
        aria-label={`Группы производителей: ${position === "top" ? "верхняя" : "нижняя"} навигация`}
        className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel)] p-3"
      >
        {selectedGroup ? (
          <>
            <AppButton onClick={showList}>← К списку групп</AppButton>

            <p
              role={position === "top" ? "status" : undefined}
              className="text-sm text-[var(--app-muted)]"
            >
              Группа {selectedIndex + 1} из {groups.length}
            </p>

            <div className="flex flex-wrap gap-2">
              <AppButton
                disabled={selectedIndex === 0}
                onClick={() => openGroup(selectedIndex - 1)}
              >
                Предыдущая группа
              </AppButton>

              <AppButton
                disabled={selectedIndex === groups.length - 1}
                onClick={() => openGroup(selectedIndex + 1)}
              >
                Следующая группа
              </AppButton>
            </div>
          </>
        ) : (
          <>
            <p
              role={position === "top" ? "status" : undefined}
              className="text-sm text-[var(--app-muted)]"
            >
              Группы {groups.length === 0 ? 0 : startIndex + 1}–
              {startIndex + visibleGroups.length} из {groups.length}
            </p>

            {pageCount > 1 && (
              <div className="flex flex-wrap items-center gap-2">
                <AppButton
                  disabled={page === 1}
                  onClick={() => changePage(page - 1)}
                  aria-label="Предыдущая страница групп"
                >
                  Назад
                </AppButton>

                <span className="text-sm text-[var(--app-muted)]">
                  {page} / {pageCount}
                </span>

                <AppButton
                  disabled={page === pageCount}
                  onClick={() => changePage(page + 1)}
                  aria-label="Следующая страница групп"
                >
                  Далее
                </AppButton>
              </div>
            )}
          </>
        )}
      </nav>
    );
  }

  return (
    <div
      ref={containerRef}
      role="region"
      aria-label="Группы производителей"
      tabIndex={-1}
      className="mt-4 grid min-w-0 scroll-mt-[calc(var(--app-header-height,4rem)+var(--import-workspace-header-height,6rem)+1rem)] gap-4 rounded-2xl focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--app-accent)]"
    >
      {renderNavigation("top")}

      {navigation.selected !== null && selectedGroup === null && (
        <p role="status" className="text-sm text-[var(--app-muted)]">
          Выбранная группа изменилась или отсутствует в новом результате.
          Показан актуальный список групп.
        </p>
      )}

      <div
        hidden={selectedGroup !== null}
        className={
          selectedGroup ? "hidden" : "grid min-w-0 gap-4 xl:grid-cols-2"
        }
      >
        {visibleGroups.map((group, index) => {
          const presentation = getResolutionPresentation(group.source);
          const target =
            group.status === "IgnoredNoise"
              ? "Не является производителем"
              : (group.resolvedManufacturerName ??
                "Производитель не определён");

          return (
            <article
              key={getManufacturerGroupKey(group)}
              className={[
                "flex min-w-0 flex-col gap-3 rounded-2xl border p-4",
                presentation.cardClassName,
              ].join(" ")}
            >
              <div className="flex flex-wrap items-center justify-between gap-2">
                <span
                  className={[
                    "rounded-full border px-3 py-1 text-xs font-medium",
                    presentation.badgeClassName,
                  ].join(" ")}
                >
                  {presentation.label}
                </span>

                <span className="text-sm text-[var(--app-muted)]">
                  Строк: {group.occurrenceCount}
                </span>
              </div>

              <h4
                title={group.sourceValue}
                className="line-clamp-2 break-all text-base font-semibold text-[var(--app-text)]"
              >
                {group.sourceValue}
              </h4>

              <p
                title={target}
                className={[
                  "line-clamp-2 break-all text-sm font-medium",
                  group.status === "Unresolved"
                    ? "text-[var(--app-danger)]"
                    : group.status === "IgnoredNoise"
                      ? "text-[var(--app-warning)]"
                      : "text-[var(--app-accent)]",
                ].join(" ")}
              >
                → {target}
              </p>

              <p className="line-clamp-2 text-sm leading-6 text-[var(--app-muted)]">
                {presentation.explanation}
              </p>

              <AppButton
                variant="primary"
                className="mt-auto self-start"
                aria-label={`Открыть разбор: ${group.sourceValue}`}
                onClick={() => openGroup(startIndex + index)}
              >
                Открыть разбор →
              </AppButton>
            </article>
          );
        })}
      </div>

      {groups
        .filter((group) =>
          navigation.visited.includes(getManufacturerGroupKey(group)),
        )
        .map((group) => {
          const active = getManufacturerGroupKey(group) === navigation.selected;

          return (
            <div
              key={getManufacturerGroupKey(group)}
              hidden={!active}
              className={active ? "min-w-0" : "hidden"}
            >
              <ManufacturerResolutionGroupCard
                key={getManufacturerGroupKey(group)}
                batchId={batchId}
                productTypeId={productTypeId}
                group={group}
                onAnalysisChange={onAnalysisChange}
              />
            </div>
          );
        })}

      {(selectedGroup !== null || pageCount > 1) && renderNavigation("bottom")}
    </div>
  );
}

export function CatalogImportManufacturerResolutionPanel({
  batchId,
  productTypeId,
  summary,
  onAnalysisChange,
}: CatalogImportManufacturerResolutionPanelProps) {
  if (summary === null) {
    return (
      <section className="rounded-3xl border border-violet-500/20 bg-violet-500/[0.04] p-6">
        <h2 className="text-xl font-semibold text-[var(--app-text)]">
          Разбор производителей
        </h2>

        <p className="mt-2 max-w-4xl text-sm leading-6 text-[var(--app-muted)]">
          После запуска анализа здесь появится отдельная сводка по значениям из
          колонки производителя. Она не относится к характеристикам товара и не
          является частью Recognition Shadow Mode.
        </p>

        <div className="mt-5 rounded-2xl border border-sky-500/25 bg-sky-500/[0.06] p-4">
          <p className="text-sm font-medium text-[var(--app-accent)]">
            Справочник производителей пока не изменяется
          </p>

          <p className="mt-2 text-sm leading-6 text-[var(--app-accent)]">
            Нажмите «Повторить автораспознавание» или сохраните сопоставление
            колонок. Backend повторно прочитает Excel и вернёт результат
            ManufacturerResolver.
          </p>
        </div>
      </section>
    );
  }

  const unresolvedGroups = summary.groups.filter(
    (group) => group.status === "Unresolved",
  );
  const ignoredNoiseGroups = summary.groups.filter(
    (group) => group.status === "IgnoredNoise",
  );
  const aliasGroups = summary.groups.filter(
    (group) => group.source === "ApprovedAlias",
  );
  const exactNameGroups = summary.groups.filter(
    (group) => group.source === "ExactName",
  );
  const orderedGroups = [
    ...unresolvedGroups,
    ...ignoredNoiseGroups,
    ...aliasGroups,
    ...exactNameGroups,
  ];

  return (
    <section className="rounded-3xl border border-violet-500/25 bg-violet-500/[0.04] p-6">
      <div>
        <h2 className="text-xl font-semibold text-[var(--app-text)]">
          Разбор производителей
        </h2>

        <p className="mt-2 max-w-4xl text-sm leading-6 text-[var(--app-muted)]">
          Этот раздел показывает, как значения из колонки производителя были
          сопоставлены со справочником Manufacturer и таблицей
          ManufacturerAlias. Здесь производитель рассматривается как отдельная
          сущность каталога, а не как характеристика товара.
        </p>
      </div>

      <div className="mt-5 rounded-2xl border border-green-500/25 bg-green-500/[0.06] p-4">
        <p className="text-sm font-medium text-[var(--app-success)]">
          Режим безопасного чтения
        </p>

        <p className="mt-2 text-sm leading-6 text-[var(--app-success)]">
          Анализ ничего не создаёт и не исправляет автоматически. Даже
          неизвестное значение, встретившееся много раз, не станет Manufacturer
          или ManufacturerAlias без явного решения Technical-пользователя.
        </p>
      </div>

      <div className="mt-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
        <ManufacturerSummaryCard
          label="Строк с производителем"
          value={summary.rowsWithManufacturerValueCount}
          description="Количество строк, в которых Excel содержал непустое значение производителя."
        />

        <ManufacturerSummaryCard
          label="Найдено по точному имени"
          value={summary.resolvedByExactNameRowsCount}
          description="Значение Excel совпало с каноническим именем Manufacturer."
          tone="Success"
        />

        <ManufacturerSummaryCard
          label="Найдено через alias"
          value={summary.resolvedByApprovedAliasRowsCount}
          description="Использован подтверждённый ManufacturerAlias."
          tone="Success"
        />

        <ManufacturerSummaryCard
          label="Подтверждённый шум"
          value={summary.ignoredNoiseRowsCount}
          description="Technical подтвердил, что значение не является названием производителя."
          tone={summary.ignoredNoiseRowsCount > 0 ? "Warning" : "Success"}
        />

        <ManufacturerSummaryCard
          label="Не разрешено"
          value={summary.unresolvedRowsCount}
          description="Нет ни точного Manufacturer, ни Approved ManufacturerAlias."
          tone={summary.unresolvedRowsCount > 0 ? "Danger" : "Success"}
        />
      </div>

      <div className="mt-8">
        <div className="flex flex-col justify-between gap-3 lg:flex-row lg:items-end">
          <div>
            <h3 className="text-lg font-semibold text-[var(--app-text)]">
              Группы исходных значений
            </h3>

            <p className="mt-2 max-w-4xl text-sm leading-6 text-[var(--app-muted)]">
              Одинаковые нормализованные значения объединены в одну карточку.
              Неизвестные группы показаны первыми, затем alias и точные
              совпадения.
            </p>
          </div>

          <div className="flex flex-wrap gap-2">
            <span className="rounded-full border border-red-500/30 bg-red-500/10 px-3 py-1 text-xs text-[var(--app-danger)]">
              Неизвестных групп: {unresolvedGroups.length}
            </span>

            <span className="rounded-full border border-violet-500/30 bg-violet-500/10 px-3 py-1 text-xs text-[var(--app-role-text)]">
              Alias-групп: {aliasGroups.length}
            </span>

            <span className="rounded-full border border-green-500/30 bg-green-500/10 px-3 py-1 text-xs text-[var(--app-success)]">
              Точных групп: {exactNameGroups.length}
            </span>

            <span className="rounded-full border border-orange-500/30 bg-orange-500/10 px-3 py-1 text-xs text-[var(--app-warning)]">
              Шумовых групп: {ignoredNoiseGroups.length}
            </span>
          </div>
        </div>

        {orderedGroups.length === 0 ? (
          <div className="mt-4 rounded-2xl border border-slate-500/20 bg-[var(--app-surface)] p-5">
            <p className="font-medium text-[var(--app-text)]">
              Значения производителей отсутствуют
            </p>

            <p className="mt-2 text-sm leading-6 text-[var(--app-muted)]">
              В проанализированных строках не найдено непустых значений в
              колонке производителя.
            </p>
          </div>
        ) : (
          <ManufacturerGroupsBrowser
            key={JSON.stringify([batchId, productTypeId ?? null])}
            batchId={batchId}
            productTypeId={productTypeId}
            groups={orderedGroups}
            onAnalysisChange={onAnalysisChange}
          />
        )}
      </div>
    </section>
  );
}
