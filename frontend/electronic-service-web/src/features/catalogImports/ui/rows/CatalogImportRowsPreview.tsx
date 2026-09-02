"use client";

import { useQuery } from "@tanstack/react-query";
import {
  Fragment,
  useCallback,
  useEffect,
  useState,
  type FormEvent,
} from "react";
import { getCatalogProductTypes } from "@/features/catalogMetadata/api/getCatalogProductTypes";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppSelect } from "@/shared/ui/AppSelect";
import { AppButton } from "@/shared/ui/AppButton";
import { AppInput } from "@/shared/ui/AppInput";
import { getCatalogImportRows } from "../../api/getCatalogImportRows";
import { getCatalogImportRowProblemCodes } from "../../api/getCatalogImportRowProblemCodes";
import { getCatalogImportRowFilterStatusLabel } from "../../model/catalogImportRowStatus";
import { catalogImportQueryKeys } from "../../model/queryKeys";
import {
  catalogImportRowFilterStatuses,
  type CatalogImportRow,
  type CatalogImportRowFilterStatus,
  type CatalogImportRowIssue,
  type CatalogImportRowProblemCode,
  type CatalogImportRowProblemKind,
  type CatalogImportManufacturerGroup,
  type CatalogImportManufacturerResolutionSource,
} from "../../model/types";
import { CatalogImportRowStatusBadge } from "../CatalogImportRowStatusBadge";
import { CatalogImportBulkRowsEditor } from "./CatalogImportBulkRowsEditor";
import { CatalogImportRowEditor } from "./CatalogImportRowEditor";
import { useCatalogImportRowExplanations } from "../../model/useCatalogImportRowExplanations";
import { getCatalogImportManufacturerGroups } from "../../api/getCatalogImportManufacturerGroups";
import {
  CatalogImportNameLegend,
  CatalogImportRowName,
} from "./CatalogImportRowName";

interface CatalogImportRowsPreviewProps {
  batchId: string;
  productTypeId?: string | null;
  expectedVersion: number;
  canEditRows: boolean;
}

const pageSize = 25;

const priceFormatter = new Intl.NumberFormat("ru-RU", {
  style: "currency",
  currency: "RUB",
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

function formatCount(
  count: number,
  one: string,
  few: string,
  many: string,
): string {
  const absoluteCount = Math.abs(count);
  const lastTwoDigits = absoluteCount % 100;
  const lastDigit = absoluteCount % 10;

  let word = many;

  if (lastTwoDigits < 11 || lastTwoDigits > 14) {
    if (lastDigit === 1) {
      word = one;
    } else if (lastDigit >= 2 && lastDigit <= 4) {
      word = few;
    }
  }

  return `${count.toLocaleString("ru-RU")} ${word}`;
}

function formatProblemCodeOption(item: CatalogImportRowProblemCode): string {
  const errors = formatCount(item.errorRowsCount, "ошибка", "ошибки", "ошибок");

  const warnings = formatCount(
    item.warningRowsCount,
    "предупреждение",
    "предупреждения",
    "предупреждений",
  );

  if (item.errorRowsCount > 0 && item.warningRowsCount === 0) {
    return `${item.code} · ${errors}`;
  }

  if (item.warningRowsCount > 0 && item.errorRowsCount === 0) {
    return `${item.code} · ${warnings}`;
  }

  const rows = formatCount(item.rowsCount, "строка", "строки", "строк");

  return `${item.code} · ${rows} · ${errors} + ${warnings}`;
}

function getProblemCodeTitle(code: string): string {
  switch (code) {
    case "article.required":
      return "Не указан артикул";

    case "manufacturer.resolved_by_alias":
      return "Производитель определён через alias";

    default:
      return code;
  }
}

interface ManufacturerGroupPresentation {
  label: string;
  cardClassName: string;
  badgeClassName: string;
}

interface ManufacturerGroupBreakdownItem {
  key: string;
  label: string;
  count: number;
  className: string;
}

function getManufacturerGroupPresentation(
  group: CatalogImportManufacturerGroup,
): ManufacturerGroupPresentation {
  const populatedKindsCount = [
    group.exactNameRowsCount,
    group.approvedAliasRowsCount,
    group.ignoredNoiseRowsCount,
    group.unresolvedRowsCount,
    group.manualRowsCount,
  ].filter((count) => count > 0).length;

  if (group.unresolvedRowsCount > 0) {
    return {
      label: "Требует решения",
      cardClassName:
        "border-[var(--app-danger-border)] bg-[var(--app-danger-soft)]",
      badgeClassName:
        "border-[var(--app-danger-border)] bg-[var(--app-panel)] text-[var(--app-danger)]",
    };
  }

  if (group.ignoredNoiseRowsCount > 0 || populatedKindsCount > 1) {
    return {
      label: "Смешанный результат",
      cardClassName:
        "border-[var(--app-warning-border)] bg-[var(--app-warning-soft)]",
      badgeClassName:
        "border-[var(--app-warning-border)] bg-[var(--app-panel)] text-[var(--app-warning)]",
    };
  }

  if (group.approvedAliasRowsCount > 0) {
    return {
      label: "Через alias",
      cardClassName:
        "border-[var(--app-role-border)] bg-[var(--app-role-soft)]",
      badgeClassName:
        "border-[var(--app-role-border)] bg-[var(--app-panel)] text-[var(--app-role-text)]",
    };
  }

  if (group.manualRowsCount > 0) {
    return {
      label: "Выбран вручную",
      cardClassName:
        "border-[var(--app-accent-border)] bg-[var(--app-accent-soft)]",
      badgeClassName:
        "border-[var(--app-accent-border)] bg-[var(--app-panel)] text-[var(--app-accent)]",
    };
  }

  return {
    label: "Точное имя",
    cardClassName:
      "border-[var(--app-success-border)] bg-[var(--app-success-soft)]",
    badgeClassName:
      "border-[var(--app-success-border)] bg-[var(--app-panel)] text-[var(--app-success)]",
  };
}

function getManufacturerGroupBreakdown(
  group: CatalogImportManufacturerGroup,
): ManufacturerGroupBreakdownItem[] {
  return [
    {
      key: "exact",
      label: "Точное имя",
      count: group.exactNameRowsCount,
      className: "text-[var(--app-success)]",
    },
    {
      key: "alias",
      label: "Через alias",
      count: group.approvedAliasRowsCount,
      className: "text-[var(--app-role-text)]",
    },
    {
      key: "noise",
      label: "Шум",
      count: group.ignoredNoiseRowsCount,
      className: "text-[var(--app-warning)]",
    },
    {
      key: "unresolved",
      label: "Не определено",
      count: group.unresolvedRowsCount,
      className: "text-[var(--app-danger)]",
    },
    {
      key: "manual",
      label: "Вручную",
      count: group.manualRowsCount,
      className: "text-[var(--app-accent)]",
    },
  ].filter((item) => item.count > 0);
}

function formatNullableText(value: string | null | undefined): string {
  const normalizedValue = value?.trim();

  return normalizedValue ? normalizedValue : "—";
}

function formatNullablePrice(value: number | null | undefined): string {
  if (value === null || value === undefined) {
    return "—";
  }

  return priceFormatter.format(value);
}

function formatNullableInteger(value: number | null | undefined): string {
  if (value === null || value === undefined) {
    return "—";
  }

  return value.toLocaleString("ru-RU");
}

function getRawDataEntries(row: CatalogImportRow): Array<[string, string]> {
  return Object.entries(row.rawData).sort(
    ([leftColumn], [rightColumn]) => Number(leftColumn) - Number(rightColumn),
  );
}

export function CatalogImportRowsPreview({
  batchId,
  productTypeId,
  expectedVersion,
  canEditRows,
}: CatalogImportRowsPreviewProps) {
  const [status, setStatus] = useState<CatalogImportRowFilterStatus | null>(
    null,
  );

  const [searchDraft, setSearchDraft] = useState("");

  const [search, setSearch] = useState("");

  const [issueCode, setIssueCode] = useState<string | null>(null);

  const [problemKind, setProblemKind] =
    useState<CatalogImportRowProblemKind | null>(null);

  const [selectedManufacturerGroup, setSelectedManufacturerGroup] =
    useState<CatalogImportManufacturerGroup | null>(null);

  const manufacturerGroupKey = selectedManufacturerGroup?.groupKey ?? null;

  const [page, setPage] = useState(1);

  const [expandedRowId, setExpandedRowId] = useState<string | null>(null);

  const [editingRowId, setEditingRowId] = useState<string | null>(null);

  const [selectedRowIds, setSelectedRowIds] = useState<Set<string>>(
    () => new Set(),
  );

  const [isBulkEditorOpen, setIsBulkEditorOpen] = useState(false);

  const resetRowsWorkspace = useCallback((): void => {
    setExpandedRowId(null);
    setEditingRowId(null);
    setSelectedRowIds(new Set());
    setIsBulkEditorOpen(false);
    setPage(1);
  }, []);

  useEffect(() => {
    const normalizedSearch = searchDraft.trim();

    if (normalizedSearch === search) {
      return;
    }

    const timeoutId = window.setTimeout(() => {
      setSearch(normalizedSearch);
      resetRowsWorkspace();
    }, 450);

    return () => {
      window.clearTimeout(timeoutId);
    };
  }, [resetRowsWorkspace, search, searchDraft]);

  const rowsQuery = useQuery({
    queryKey: [
      ...catalogImportQueryKeys.rows(
        batchId,
        status,
        search,
        issueCode,
        problemKind,
        manufacturerGroupKey,
        page,
        pageSize,
      ),
      expectedVersion,
    ],
    queryFn: () =>
      getCatalogImportRows({
        batchId,
        status,
        search,
        issueCode,
        problemKind,
        manufacturerGroupKey,
        page,
        pageSize,
      }),
    enabled: batchId.length > 0,
    placeholderData: (previousData, previousQuery) => {
      const previousKey = previousQuery?.queryKey;

      const filtersDidNotChange =
        previousKey?.[3] === status &&
        previousKey?.[4] === search &&
        previousKey?.[5] === issueCode &&
        previousKey?.[6] === problemKind &&
        previousKey?.[7] === manufacturerGroupKey;

      return filtersDidNotChange ? previousData : undefined;
    },
  });

  const manufacturerGroupsQuery = useQuery({
    queryKey: catalogImportQueryKeys.manufacturerGroups(
      batchId,
      expectedVersion,
    ),
    queryFn: () =>
      getCatalogImportManufacturerGroups({
        batchId,
        expectedVersion,
      }),
    enabled: batchId.length > 0 && expectedVersion > 0,
  });

  const problemCodesQuery = useQuery({
    queryKey: catalogImportQueryKeys.rowProblemCodes(
      batchId,
      status,
      search,
      expectedVersion,
    ),
    queryFn: () =>
      getCatalogImportRowProblemCodes({
        batchId,
        expectedVersion,
        status,
        search,
      }),
    enabled: batchId.length > 0 && expectedVersion > 0,
  });

  const packageProblemCodesQuery = useQuery({
    queryKey: catalogImportQueryKeys.rowProblemCodes(
      batchId,
      null,
      "",
      expectedVersion,
    ),
    queryFn: () =>
      getCatalogImportRowProblemCodes({
        batchId,
        expectedVersion,
      }),
    enabled: batchId.length > 0 && expectedVersion > 0,
  });

  const productTypesQuery = useQuery({
    queryKey: ["catalog-product-types"],
    queryFn: getCatalogProductTypes,
    enabled: batchId.length > 0,
    staleTime: 5 * 60 * 1000,
  });

  const nameExplanations = useCatalogImportRowExplanations(
    batchId,
    expectedVersion,
    rowsQuery,
  );

  const items = rowsQuery.data?.items ?? [];

  const manufacturerGroups = manufacturerGroupsQuery.data?.items ?? [];

  const selectedManufacturerGroupIsVisible =
    selectedManufacturerGroup !== null &&
    manufacturerGroups.some(
      (group) => group.groupKey === selectedManufacturerGroup.groupKey,
    );

  const visibleManufacturerGroups =
    selectedManufacturerGroup !== null && !selectedManufacturerGroupIsVisible
      ? [
          {
            ...selectedManufacturerGroup,
            rowsCount: 0,
          },
          ...manufacturerGroups,
        ].slice(0, 6)
      : manufacturerGroups;

  const totalCount = rowsQuery.data?.totalCount ?? 0;
  const backendTotalPages = rowsQuery.data?.totalPages ?? 0;
  const totalPages = Math.max(1, backendTotalPages);

  const problemCodeItems = problemCodesQuery.data?.items ?? [];

  const packageProblemCodeItems = packageProblemCodesQuery.data?.items ?? [];

  const selectedProductType = productTypesQuery.data?.find(
    (item) => item.id === productTypeId,
  );

  const selectedProblemCode = problemCodeItems.find(
    (item) => item.code === issueCode,
  );

  const allRowsCount =
    issueCode === null
      ? problemCodesQuery.data?.totalRowsCount
      : selectedProblemCode?.rowsCount;

  const errorRowsCount =
    issueCode === null
      ? problemCodesQuery.data?.errorRowsCount
      : selectedProblemCode?.errorRowsCount;

  const warningRowsCount =
    issueCode === null
      ? problemCodesQuery.data?.warningRowsCount
      : selectedProblemCode?.warningRowsCount;

  function formatQuickFilterCount(value: number | undefined): string {
    return value === undefined ? "—" : value.toLocaleString("ru-RU");
  }

  const selectedIssueCodeIsMissing =
    issueCode !== null &&
    !problemCodeItems.some((item) => item.code === issueCode);

  const problemCodeOptions = [
    {
      value: "",
      label: "Все проблемы",
    },
    ...(selectedIssueCodeIsMissing
      ? [
          {
            value: issueCode,
            label: `${issueCode} · 0 строк`,
          },
        ]
      : []),
    ...problemCodeItems.map((item) => ({
      value: item.code,
      label: formatProblemCodeOption(item),
    })),
  ];

  const hasActiveFilters =
    status !== null ||
    searchDraft.trim().length > 0 ||
    issueCode !== null ||
    problemKind !== null ||
    manufacturerGroupKey !== null;

  let emptyFilteredRowsMessage =
    "Строк, соответствующих выбранным условиям, больше нет.";

  if (selectedManufacturerGroup !== null) {
    emptyFilteredRowsMessage = `Строк с исходным производителем «${selectedManufacturerGroup.sourceValue}» больше нет. Возможно, данные строки были изменены.`;
  } else if (issueCode !== null) {
    emptyFilteredRowsMessage = `Строк с проблемой «${issueCode}» больше нет. Возможно, они были исправлены.`;
  } else if (problemKind === "Error") {
    emptyFilteredRowsMessage =
      "Строк с ошибками больше нет. Возможно, последняя ошибка была исправлена.";
  } else if (problemKind === "Warning") {
    emptyFilteredRowsMessage =
      "Строк с предупреждениями больше нет. Возможно, последнее предупреждение было устранено.";
  } else if (search.length > 0) {
    emptyFilteredRowsMessage =
      "По текущему поисковому запросу строки не найдены.";
  } else if (status !== null) {
    emptyFilteredRowsMessage = "Строк с выбранным статусом больше нет.";
  }

  const canUseBulkEditing = canEditRows && Boolean(productTypeId);

  const selectedRows = items.filter((row) => selectedRowIds.has(row.rowId));

  const allPageRowsSelected =
    items.length > 0 && selectedRows.length === items.length;

  function clearSelectedRows(): void {
    setSelectedRowIds(new Set());
    setIsBulkEditorOpen(false);
  }

  function toggleSelectedRow(rowId: string): void {
    setSelectedRowIds((currentRowIds) => {
      const nextRowIds = new Set(currentRowIds);

      if (nextRowIds.has(rowId)) {
        nextRowIds.delete(rowId);
      } else {
        nextRowIds.add(rowId);
      }

      return nextRowIds;
    });

    setIsBulkEditorOpen(false);
    setEditingRowId(null);
  }

  function toggleCurrentPageSelection(): void {
    if (allPageRowsSelected) {
      clearSelectedRows();

      return;
    }

    setSelectedRowIds(new Set(items.map((row) => row.rowId)));
    setIsBulkEditorOpen(false);
    setEditingRowId(null);
  }

  function openBulkEditor(): void {
    if (!canUseBulkEditing || selectedRows.length === 0) {
      return;
    }

    setExpandedRowId(null);
    setEditingRowId(null);
    setIsBulkEditorOpen(true);
  }

  function handleStatusChange(value: string): void {
    setStatus(
      value.length === 0 ? null : (value as CatalogImportRowFilterStatus),
    );

    resetRowsWorkspace();
  }

  function handleIssueCodeChange(value: string): void {
    setIssueCode(value.length === 0 ? null : value);
    resetRowsWorkspace();
  }

  function handleSummaryProblemClick(code: string): void {
    const nextIssueCode = issueCode === code ? null : code;

    setSearchDraft("");
    setSearch("");
    setStatus(null);
    setIssueCode(nextIssueCode);
    setProblemKind(null);
    setSelectedManufacturerGroup(null);
    resetRowsWorkspace();
  }

  function handleProblemKindChange(
    nextProblemKind: CatalogImportRowProblemKind | null,
  ): void {
    if (nextProblemKind === problemKind) {
      return;
    }

    setProblemKind(nextProblemKind);
    resetRowsWorkspace();
  }

  function toggleManufacturerGroup(
    group: CatalogImportManufacturerGroup,
  ): void {
    if (manufacturerGroupKey === group.groupKey) {
      setSelectedManufacturerGroup(null);
    } else {
      setSelectedManufacturerGroup(group);
    }

    resetRowsWorkspace();
  }

  function resetFilters(): void {
    setSearchDraft("");
    setSearch("");
    setStatus(null);
    setIssueCode(null);
    setProblemKind(null);
    setSelectedManufacturerGroup(null);
    resetRowsWorkspace();
  }

  function handlePageChange(requestedPage: number): void {
    const nextPage = Math.min(totalPages, Math.max(1, requestedPage));

    if (nextPage === page) {
      return;
    }

    setExpandedRowId(null);
    setEditingRowId(null);
    clearSelectedRows();
    setPage(nextPage);
  }

  function toggleRow(rowId: string): void {
    setEditingRowId(null);

    setExpandedRowId((currentRowId) => (currentRowId === rowId ? null : rowId));
  }

  function editRow(rowId: string): void {
    setExpandedRowId(null);
    clearSelectedRows();

    setEditingRowId((currentRowId) => (currentRowId === rowId ? null : rowId));
  }

  return (
    <section className="min-w-0 max-w-full rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel)] p-4 text-[var(--app-text)] sm:p-5">
      <div className="min-w-0">
        <h2 className="text-lg font-semibold">Строки Excel</h2>

        <p className="mt-1 text-sm text-[var(--app-muted)]">
          Исходные значения, результат нормализации и найденные проблемы.
        </p>

        <div className="mt-2 flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-[var(--app-muted)]">
          {rowsQuery.data && (
            <span>
              Найдено:{" "}
              <strong className="text-[var(--app-text)]">{totalCount}</strong>
            </span>
          )}

          <span>По {pageSize} строк на странице</span>

          {rowsQuery.isFetching && !rowsQuery.isLoading && (
            <span role="status" className="text-[var(--app-accent)]">
              Обновляем…
            </span>
          )}
        </div>
      </div>

      <section
        aria-labelledby="catalog-import-technical-summary-title"
        className="mt-4 rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4"
      >
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <h3
              id="catalog-import-technical-summary-title"
              className="text-sm font-semibold text-[var(--app-text)]"
            >
              Техническая сводка
            </h3>

            <p className="mt-1 text-xs leading-5 text-[var(--app-muted)]">
              Данные по всему пакету. Одна строка может одновременно содержать
              ошибку и предупреждение.
            </p>
          </div>

          {packageProblemCodesQuery.isFetching &&
            !packageProblemCodesQuery.isLoading && (
              <span role="status" className="text-xs text-[var(--app-accent)]">
                Обновляем…
              </span>
            )}
        </div>

        {packageProblemCodesQuery.isLoading && (
          <p role="status" className="mt-4 text-sm text-[var(--app-muted)]">
            Загружаем техническую сводку…
          </p>
        )}

        {packageProblemCodesQuery.isError && (
          <p role="alert" className="mt-4 text-sm text-[var(--app-danger)]">
            {getApiErrorMessage(
              packageProblemCodesQuery.error,
              "Не удалось загрузить техническую сводку.",
            )}
          </p>
        )}

        {packageProblemCodesQuery.data && (
          <>
            <dl className="mt-4 grid gap-3 sm:grid-cols-3">
              <div className="rounded-xl border border-[var(--app-border)] bg-[var(--app-panel)] p-3">
                <dt className="text-xs text-[var(--app-muted)]">Всего строк</dt>

                <dd className="mt-1 text-xl font-semibold tabular-nums text-[var(--app-text)]">
                  {packageProblemCodesQuery.data.totalRowsCount.toLocaleString(
                    "ru-RU",
                  )}
                </dd>
              </div>

              <div className="rounded-xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-3">
                <dt className="text-xs text-[var(--app-danger)]">
                  Строк с ошибками
                </dt>

                <dd className="mt-1 text-xl font-semibold tabular-nums text-[var(--app-danger)]">
                  {packageProblemCodesQuery.data.errorRowsCount.toLocaleString(
                    "ru-RU",
                  )}
                </dd>
              </div>

              <div className="rounded-xl border border-[var(--app-warning-border)] bg-[var(--app-warning-soft)] p-3">
                <dt className="text-xs text-[var(--app-warning)]">
                  Строк с предупреждениями
                </dt>

                <dd className="mt-1 text-xl font-semibold tabular-nums text-[var(--app-warning)]">
                  {packageProblemCodesQuery.data.warningRowsCount.toLocaleString(
                    "ru-RU",
                  )}
                </dd>
              </div>
            </dl>

            <div className="mt-4">
              <h4 className="text-xs font-semibold uppercase tracking-wide text-[var(--app-muted)]">
                Повторяющиеся проблемы
              </h4>

              {packageProblemCodeItems.length > 0 ? (
                <div className="mt-3 grid gap-3 md:grid-cols-2">
                  {packageProblemCodeItems.slice(0, 6).map((item) => {
                    const isActive = issueCode === item.code;
                    const hasErrors = item.errorRowsCount > 0;

                    return (
                      <button
                        key={item.code}
                        type="button"
                        aria-pressed={isActive}
                        onClick={() => handleSummaryProblemClick(item.code)}
                        className={[
                          "rounded-xl border p-3 text-left transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--app-accent)]",
                          hasErrors
                            ? "border-[var(--app-danger-border)] bg-[var(--app-danger-soft)]"
                            : "border-[var(--app-warning-border)] bg-[var(--app-warning-soft)]",
                          isActive ? "ring-2 ring-[var(--app-accent)]" : "",
                        ].join(" ")}
                      >
                        <span
                          className={[
                            "block text-sm font-semibold",
                            hasErrors
                              ? "text-[var(--app-danger)]"
                              : "text-[var(--app-warning)]",
                          ].join(" ")}
                        >
                          {getProblemCodeTitle(item.code)}
                        </span>

                        <span className="mt-1 block break-all font-mono text-[11px] text-[var(--app-muted)]">
                          {item.code}
                        </span>

                        <span className="mt-3 flex flex-wrap gap-x-3 gap-y-1 text-xs text-[var(--app-muted)]">
                          <strong className="text-[var(--app-text)]">
                            {formatCount(
                              item.rowsCount,
                              "строка",
                              "строки",
                              "строк",
                            )}
                          </strong>

                          {item.errorRowsCount > 0 && (
                            <span>
                              Ошибки:{" "}
                              {item.errorRowsCount.toLocaleString("ru-RU")}
                            </span>
                          )}

                          {item.warningRowsCount > 0 && (
                            <span>
                              Предупреждения:{" "}
                              {item.warningRowsCount.toLocaleString("ru-RU")}
                            </span>
                          )}
                        </span>

                        <span className="mt-3 block text-xs text-[var(--app-muted)]">
                          {isActive
                            ? "Показаны затронутые строки"
                            : "Показать затронутые строки"}
                        </span>
                      </button>
                    );
                  })}
                </div>
              ) : (
                <p className="mt-3 text-sm text-[var(--app-muted)]">
                  Повторяющиеся проблемы не обнаружены.
                </p>
              )}
            </div>
          </>
        )}
      </section>

      {visibleManufacturerGroups.length > 0 && (
        <div className="mt-4">
          <div className="flex flex-wrap items-end justify-between gap-2">
            <div>
              <h3 className="text-sm font-semibold text-[var(--app-text)]">
                Группы исходных производителей
              </h3>

              <p className="mt-1 text-xs text-[var(--app-muted)]">
                Показаны до шести самых крупных групп. Нажатие фильтрует строки.
              </p>
            </div>

            {selectedManufacturerGroup && (
              <AppButton
                size="sm"
                variant="ghost"
                onClick={() => {
                  setSelectedManufacturerGroup(null);
                  resetRowsWorkspace();
                }}
              >
                Снять фильтр группы
              </AppButton>
            )}
          </div>

          <div className="mt-3 grid gap-3 md:grid-cols-2 xl:grid-cols-3">
            {visibleManufacturerGroups.map((group) => {
              const presentation = getManufacturerGroupPresentation(group);
              const breakdown = getManufacturerGroupBreakdown(group);

              const isActive = manufacturerGroupKey === group.groupKey;

              const resolvedManufacturerName =
                group.resolvedManufacturerName?.trim() || null;

              const showResolution =
                resolvedManufacturerName !== null &&
                resolvedManufacturerName.toLocaleUpperCase("ru-RU") !==
                  group.sourceValue.trim().toLocaleUpperCase("ru-RU");

              return (
                <button
                  key={group.groupKey}
                  type="button"
                  aria-pressed={isActive}
                  onClick={() => toggleManufacturerGroup(group)}
                  className={[
                    "min-w-0 rounded-xl border p-3 text-left transition hover:-translate-y-0.5 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--app-accent)] motion-reduce:transform-none motion-reduce:transition-none",
                    presentation.cardClassName,
                    isActive ? "ring-2 ring-[var(--app-accent)]" : "",
                  ].join(" ")}
                >
                  <span className="flex min-w-0 items-start justify-between gap-3">
                    <span className="min-w-0">
                      <span
                        className={[
                          "inline-flex rounded-full border px-2.5 py-1 text-xs font-medium",
                          presentation.badgeClassName,
                        ].join(" ")}
                      >
                        {presentation.label}
                      </span>

                      <strong className="mt-2 block truncate text-sm text-[var(--app-text)]">
                        {group.sourceValue}
                      </strong>

                      {showResolution && (
                        <span className="mt-1 block truncate text-xs text-[var(--app-muted)]">
                          → {resolvedManufacturerName}
                        </span>
                      )}

                      <span className="mt-2 flex flex-wrap gap-x-3 gap-y-1">
                        {breakdown.map((item) => (
                          <span
                            key={item.key}
                            className={[
                              "text-xs font-medium",
                              item.className,
                            ].join(" ")}
                          >
                            {item.label}: {item.count.toLocaleString("ru-RU")}
                          </span>
                        ))}
                      </span>
                    </span>

                    <span className="shrink-0 rounded-lg border border-[var(--app-border)] bg-[var(--app-panel)] px-2.5 py-1.5 text-xs font-semibold text-[var(--app-text)]">
                      {group.rowsCount.toLocaleString("ru-RU")}
                    </span>
                  </span>

                  <span className="mt-3 block text-xs text-[var(--app-muted)]">
                    {isActive
                      ? "Показаны строки этой группы"
                      : "Показать затронутые строки"}
                  </span>
                </button>
              );
            })}
          </div>
        </div>
      )}

      {manufacturerGroupsQuery.isLoading && (
        <p role="status" className="mt-4 text-sm text-[var(--app-muted)]">
          Загружаем группы производителей…
        </p>
      )}

      {manufacturerGroupsQuery.isError && (
        <p role="alert" className="mt-4 text-sm text-[var(--app-danger)]">
          {getApiErrorMessage(
            manufacturerGroupsQuery.error,
            "Не удалось загрузить группы производителей.",
          )}
        </p>
      )}

      <section
        aria-labelledby="catalog-import-product-type-title"
        className="mt-4 rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4"
      >
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <h3
              id="catalog-import-product-type-title"
              className="text-sm font-semibold text-[var(--app-text)]"
            >
              Тип товара
            </h3>

            <p className="mt-1 text-xs leading-5 text-[var(--app-muted)]">
              В смешанном пакете тип определяется отдельно для каждой строки.
              Общий тип используется только для однородных файлов.
            </p>
          </div>

          <span className="rounded-full border border-[var(--app-accent-border)] bg-[var(--app-accent-soft)] px-3 py-1 text-xs font-medium text-[var(--app-accent)]">
            {packageProblemCodesQuery.data
              ? `Весь пакет · ${packageProblemCodesQuery.data.totalRowsCount.toLocaleString(
                  "ru-RU",
                )}`
              : "Весь пакет"}
          </span>
        </div>

        {!productTypeId && (
          <div className="mt-3 rounded-xl border border-[var(--app-accent-border)] bg-[var(--app-accent-soft)] p-3">
            <p className="text-sm font-semibold text-[var(--app-accent)]">
              Включено построчное определение
            </p>

            <p className="mt-1 text-xs leading-5 text-[var(--app-muted)]">
              CRM определяет тип по наименованию каждой строки. Неоднозначные и
              неизвестные позиции останутся для ручного выбора.
            </p>
          </div>
        )}

        {productTypeId && productTypesQuery.isLoading && (
          <p role="status" className="mt-3 text-sm text-[var(--app-muted)]">
            Загружаем тип товара…
          </p>
        )}

        {productTypeId && productTypesQuery.isError && (
          <p role="alert" className="mt-3 text-sm text-[var(--app-danger)]">
            {getApiErrorMessage(
              productTypesQuery.error,
              "Не удалось загрузить сведения о типе товара.",
            )}
          </p>
        )}

        {productTypeId &&
          !productTypesQuery.isLoading &&
          !productTypesQuery.isError && (
            <div
              className={[
                "mt-3 rounded-xl border p-3",
                selectedProductType
                  ? "border-[var(--app-accent-border)] bg-[var(--app-accent-soft)]"
                  : "border-[var(--app-warning-border)] bg-[var(--app-warning-soft)]",
              ].join(" ")}
            >
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div className="min-w-0">
                  <span
                    className={[
                      "inline-flex rounded-full border px-2.5 py-1 text-xs font-medium",
                      selectedProductType
                        ? "border-[var(--app-accent-border)] bg-[var(--app-panel)] text-[var(--app-accent)]"
                        : "border-[var(--app-warning-border)] bg-[var(--app-panel)] text-[var(--app-warning)]",
                    ].join(" ")}
                  >
                    {selectedProductType
                      ? "Выбран для импорта"
                      : "Нет в актуальном справочнике"}
                  </span>

                  <strong className="mt-2 block text-sm text-[var(--app-text)]">
                    {selectedProductType?.name ??
                      "Выбранный тип товара не найден"}
                  </strong>

                  <span className="mt-1 block font-mono text-xs text-[var(--app-muted)]">
                    {selectedProductType?.code ?? productTypeId}
                  </span>
                </div>

                <span className="shrink-0 rounded-lg border border-[var(--app-border)] bg-[var(--app-panel)] px-2.5 py-1.5 text-xs font-semibold text-[var(--app-text)]">
                  {packageProblemCodesQuery.data?.totalRowsCount.toLocaleString(
                    "ru-RU",
                  ) ?? "—"}
                </span>
              </div>

              <p className="mt-3 text-xs text-[var(--app-muted)]">
                Тип применяется ко всем строкам этого Excel-пакета.
              </p>
            </div>
          )}
      </section>

      <div
        className="mt-4 flex flex-wrap items-center gap-2"
        aria-label="Быстрый фильтр по типу проблемы"
      >
        <span className="mr-1 text-xs font-medium text-[var(--app-muted)]">
          Показать:
        </span>

        <AppButton
          size="sm"
          variant={problemKind === null ? "primary" : "secondary"}
          aria-pressed={problemKind === null}
          onClick={() => handleProblemKindChange(null)}
        >
          Все · {formatQuickFilterCount(allRowsCount)}
        </AppButton>

        <AppButton
          size="sm"
          variant={problemKind === "Error" ? "dangerSolid" : "secondary"}
          aria-pressed={problemKind === "Error"}
          onClick={() => handleProblemKindChange("Error")}
        >
          Ошибки · {formatQuickFilterCount(errorRowsCount)}
        </AppButton>

        <AppButton
          size="sm"
          variant={problemKind === "Warning" ? "warning" : "secondary"}
          aria-pressed={problemKind === "Warning"}
          onClick={() => handleProblemKindChange("Warning")}
        >
          Предупреждения · {formatQuickFilterCount(warningRowsCount)}
        </AppButton>
      </div>

      <div className="mt-4 grid gap-3 rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] p-3 md:grid-cols-2 xl:grid-cols-[minmax(280px,1fr)_220px_340px_auto] xl:items-end">
        <div className="min-w-0">
          <label
            htmlFor="catalog-import-rows-search"
            className="mb-1.5 block text-xs font-medium text-[var(--app-muted)]"
          >
            Поиск
          </label>

          <AppInput
            id="catalog-import-rows-search"
            type="search"
            value={searchDraft}
            maxLength={100}
            placeholder="Наименование, артикул или производитель"
            autoComplete="off"
            onChange={(event) => {
              setSearchDraft(event.target.value);
            }}
          />
        </div>

        <div className="min-w-0">
          <div className="mb-1.5 text-xs font-medium text-[var(--app-muted)]">
            Статус строки
          </div>

          <AppSelect
            ariaLabel="Статус строки импорта"
            value={status ?? ""}
            onChange={handleStatusChange}
            options={[
              { value: "", label: "Все статусы" },
              ...catalogImportRowFilterStatuses.map((statusItem) => ({
                value: statusItem,
                label: getCatalogImportRowFilterStatusLabel(statusItem),
              })),
            ]}
          />
        </div>

        <div className="min-w-0">
          <div className="mb-1.5 text-xs font-medium text-[var(--app-muted)]">
            Код проблемы
          </div>

          <AppSelect
            ariaLabel="Код проблемы строки импорта"
            value={issueCode ?? ""}
            options={problemCodeOptions}
            disabled={problemCodesQuery.isLoading}
            onChange={handleIssueCodeChange}
          />

          {problemCodesQuery.isFetching && !problemCodesQuery.isLoading && (
            <p
              role="status"
              className="mt-1.5 text-xs text-[var(--app-accent)]"
            >
              Обновляем список проблем…
            </p>
          )}

          {problemCodesQuery.isError && (
            <p role="alert" className="mt-1.5 text-xs text-[var(--app-danger)]">
              {getApiErrorMessage(
                problemCodesQuery.error,
                "Не удалось загрузить список проблем.",
              )}
            </p>
          )}
        </div>

        <AppButton
          variant="secondary"
          disabled={!hasActiveFilters}
          onClick={resetFilters}
        >
          Сбросить
        </AppButton>
      </div>

      {canUseBulkEditing && selectedRows.length > 0 && (
        <div className="mt-4 flex flex-wrap items-center justify-between gap-3 rounded-xl border border-[var(--app-accent-border)] bg-[var(--app-accent-soft)] p-3">
          <p className="text-sm text-[var(--app-text)]">
            Выбрано: <strong>{selectedRows.length}</strong>
            <span className="ml-2 text-[var(--app-muted)]">
              на текущей странице
            </span>
          </p>

          <div className="flex flex-wrap gap-2">
            <AppButton
              size="sm"
              variant="primary"
              disabled={isBulkEditorOpen}
              onClick={openBulkEditor}
            >
              Редактировать выбранные
            </AppButton>

            <AppButton size="sm" onClick={clearSelectedRows}>
              Снять выбор
            </AppButton>
          </div>
        </div>
      )}

      {isBulkEditorOpen && productTypeId && selectedRows.length > 0 && (
        <div className="mt-4 min-w-0">
          <CatalogImportBulkRowsEditor
            key={selectedRows.map((row) => row.rowId).join("-")}
            batchId={batchId}
            productTypeId={productTypeId}
            expectedVersion={expectedVersion}
            rows={selectedRows}
            onCancel={() => {
              setIsBulkEditorOpen(false);
            }}
            onSaved={() => {
              clearSelectedRows();
            }}
          />
        </div>
      )}

      {rowsQuery.isError && (
        <div
          role="alert"
          className="mt-5 rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-5 text-sm text-[var(--app-danger)]"
        >
          {getApiErrorMessage(
            rowsQuery.error,
            "Не удалось загрузить строки пакета импорта.",
          )}
        </div>
      )}

      {items.length > 0 && (
        <div className="mt-4 space-y-2">
          <CatalogImportNameLegend />

          <div className="flex flex-wrap items-center justify-between gap-2">
            <p role="status" className="text-xs text-[var(--app-muted)]">
              {nameExplanations.message}
            </p>

            <AppButton
              size="sm"
              variant="ghost"
              disabled={nameExplanations.busy}
              onClick={() => {
                void nameExplanations.refresh();
              }}
            >
              Обновить подсветку
            </AppButton>
          </div>
        </div>
      )}

      {backendTotalPages > 1 && (
        <CatalogImportRowsPagination
          page={page}
          totalPages={totalPages}
          disabled={rowsQuery.isFetching}
          placement="top"
          onPageChange={handlePageChange}
        />
      )}

      {rowsQuery.isLoading ? (
        <div className="mt-5 rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-5 text-sm text-[var(--app-text)]">
          Загружаем строки Excel...
        </div>
      ) : rowsQuery.isError && items.length === 0 ? null : items.length ===
        0 ? (
        <div className="mt-5 rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-6">
          <h3 className="font-semibold text-[var(--app-text)]">
            {hasActiveFilters
              ? "По текущим фильтрам строк нет"
              : "В пакете нет строк"}
          </h3>

          <p className="mt-2 max-w-2xl text-sm leading-6 text-[var(--app-muted)]">
            {hasActiveFilters
              ? emptyFilteredRowsMessage
              : "В пакете пока нет строк для отображения."}
          </p>

          {hasActiveFilters && (
            <div className="mt-4">
              <AppButton size="sm" variant="primary" onClick={resetFilters}>
                Показать все строки
              </AppButton>
            </div>
          )}
        </div>
      ) : (
        <div className="mt-4 max-w-full overflow-x-auto rounded-xl border border-[var(--app-border)]">
          <table className="w-full min-w-[1280px] border-collapse text-left text-sm">
            <thead className="bg-[var(--app-surface)] text-[var(--app-muted)]">
              <tr>
                <th className="w-14 px-3 py-3 font-medium">
                  <input
                    type="checkbox"
                    checked={allPageRowsSelected}
                    disabled={
                      !canUseBulkEditing ||
                      items.length === 0 ||
                      isBulkEditorOpen
                    }
                    onChange={toggleCurrentPageSelection}
                    aria-label="Выбрать все строки текущей страницы"
                    className="h-4 w-4 cursor-pointer accent-[var(--app-accent-strong)] disabled:cursor-not-allowed disabled:opacity-40"
                  />
                </th>

                <th className="px-3 py-3 font-medium">Строка</th>

                <th className="px-3 py-3 font-medium">Статус</th>

                <th className="px-3 py-3 font-medium">Наименование</th>

                <th className="px-3 py-3 font-medium">Артикул</th>

                <th className="px-3 py-3 font-medium">Производитель</th>

                <th className="px-3 py-3 font-medium">Тип товара</th>

                <th className="px-3 py-3 font-medium">Цена</th>

                <th className="px-3 py-3 font-medium">Остаток</th>

                <th className="px-3 py-3 font-medium">Проблемы</th>

                <th className="px-3 py-3 font-medium">Действия</th>
              </tr>
            </thead>

            <tbody className="divide-y divide-[var(--app-border)]">
              {items.map((row) => {
                const isExpanded = expandedRowId === row.rowId;

                const isEditing = editingRowId === row.rowId;

                const rowProductType = productTypesQuery.data?.find(
                  (item) => item.id === row.data.productTypeId,
                );

                const rowCanBeEdited = canEditRows && Boolean(productTypeId);

                return (
                  <Fragment key={row.rowId}>
                    <tr
                      className={[
                        "align-top transition-colors",
                        selectedRowIds.has(row.rowId)
                          ? "bg-[var(--app-accent-soft)]"
                          : "bg-[var(--app-panel)] hover:bg-[var(--app-panel-hover)]",
                      ].join(" ")}
                    >
                      <td className="px-3 py-3">
                        <input
                          type="checkbox"
                          checked={selectedRowIds.has(row.rowId)}
                          disabled={!rowCanBeEdited || isBulkEditorOpen}
                          onChange={() => toggleSelectedRow(row.rowId)}
                          aria-label={`Выбрать строку ${row.rowNumber}`}
                          className="h-4 w-4 cursor-pointer accent-[var(--app-accent-strong)] disabled:cursor-not-allowed disabled:opacity-40"
                        />
                      </td>

                      <td className="px-3 py-3 font-medium text-[var(--app-text)]">
                        {row.rowNumber}
                      </td>

                      <td className="px-3 py-3">
                        <CatalogImportRowStatusBadge status={row.status} />
                      </td>

                      <td className="max-w-72 px-3 py-3 text-[var(--app-text)]">
                        <CatalogImportRowName
                          row={row}
                          item={nameExplanations.data?.items.find(
                            (item) => item.rowId === row.rowId,
                          )}
                        />
                      </td>

                      <td className="px-3 py-3 text-[var(--app-text)]">
                        {formatNullableText(row.data.article)}
                      </td>

                      <td className="max-w-56 px-3 py-3 text-[var(--app-text)]">
                        <p className="line-clamp-2">
                          {formatNullableText(row.data.manufacturer)}
                        </p>
                      </td>

                      <td className="max-w-56 px-3 py-3">
                        <p className="font-medium text-[var(--app-text)]">
                          {rowProductType?.name ??
                            (row.data.productTypeId
                              ? "Тип отсутствует в справочнике"
                              : "Не определён")}
                        </p>

                        {row.data.productTypeResolutionSource && (
                          <p className="mt-1 text-xs text-[var(--app-muted)]">
                            {row.data.productTypeResolutionSource ===
                            "Automatic"
                              ? "Определён автоматически"
                              : row.data.productTypeResolutionSource ===
                                  "Manual"
                                ? "Выбран вручную"
                                : "Общий тип пакета"}

                            {row.data.productTypeResolutionConfidence != null
                              ? ` · ${Math.round(
                                  row.data.productTypeResolutionConfidence *
                                    100,
                                )}%`
                              : ""}
                          </p>
                        )}
                      </td>

                      <td className="whitespace-nowrap px-3 py-3 text-[var(--app-text)]">
                        {formatNullablePrice(row.data.price)}
                      </td>

                      <td className="px-3 py-3 text-[var(--app-text)]">
                        {formatNullableInteger(row.data.stockQuantity)}
                      </td>

                      <td className="px-3 py-3">
                        <CatalogImportRowProblems row={row} />
                      </td>

                      <td className="px-3 py-3">
                        <div className="flex flex-wrap gap-2">
                          <AppButton
                            size="sm"
                            variant="secondary"
                            aria-expanded={isExpanded}
                            onClick={() => toggleRow(row.rowId)}
                          >
                            {isExpanded ? "Скрыть" : "Подробнее"}
                          </AppButton>

                          {rowCanBeEdited && (
                            <AppButton
                              size="sm"
                              variant={isEditing ? "secondary" : "primary"}
                              aria-expanded={isEditing}
                              onClick={() => editRow(row.rowId)}
                            >
                              {isEditing ? "Закрыть редактор" : "Редактировать"}
                            </AppButton>
                          )}
                        </div>
                      </td>
                    </tr>

                    {(isExpanded || isEditing) && (
                      <tr className="bg-[var(--app-surface)]">
                        <td colSpan={11} className="px-5 py-5">
                          {isEditing && productTypeId ? (
                            <CatalogImportRowEditor
                              key={`${row.rowId}-${row.status}`}
                              batchId={batchId}
                              productTypeId={productTypeId}
                              row={row}
                              onCancel={() => {
                                setEditingRowId(null);
                              }}
                              onSaved={() => {
                                setEditingRowId(null);
                              }}
                            />
                          ) : (
                            <CatalogImportRowDetails row={row} />
                          )}
                        </td>
                      </tr>
                    )}
                  </Fragment>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {backendTotalPages > 1 && (
        <CatalogImportRowsPagination
          page={page}
          totalPages={totalPages}
          disabled={rowsQuery.isFetching}
          placement="bottom"
          onPageChange={handlePageChange}
        />
      )}
    </section>
  );
}

interface CatalogImportRowsPaginationProps {
  page: number;
  totalPages: number;
  disabled: boolean;
  placement: "top" | "bottom";
  onPageChange: (page: number) => void;
}

function CatalogImportRowsPagination({
  page,
  totalPages,
  disabled,
  placement,
  onPageChange,
}: CatalogImportRowsPaginationProps) {
  const inputId = `catalog-import-page-${placement}`;

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    const value = new FormData(event.currentTarget).get("page");

    if (typeof value !== "string" || value.trim().length === 0) {
      return;
    }

    const requestedPage = Number(value);

    if (!Number.isInteger(requestedPage)) {
      return;
    }

    onPageChange(requestedPage);
  }

  return (
    <nav
      aria-label="Страницы строк импорта"
      className="mt-4 flex flex-wrap items-center justify-center gap-2 rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] px-3 py-3 sm:justify-between"
    >
      <div className="flex flex-wrap gap-2">
        <AppButton
          size="sm"
          variant="secondary"
          disabled={disabled || page <= 1}
          onClick={() => onPageChange(1)}
        >
          В начало
        </AppButton>

        <AppButton
          size="sm"
          variant="secondary"
          disabled={disabled || page <= 1}
          onClick={() => onPageChange(page - 1)}
        >
          Назад
        </AppButton>
      </div>

      <form
        className="flex flex-wrap items-center justify-center gap-2"
        onSubmit={handleSubmit}
      >
        <label htmlFor={inputId} className="text-xs text-[var(--app-muted)]">
          Страница
        </label>

        <div className="w-20">
          <AppInput
            key={page}
            id={inputId}
            name="page"
            type="number"
            inputMode="numeric"
            min={1}
            max={totalPages}
            step={1}
            required
            defaultValue={page}
            disabled={disabled}
            aria-label={"Номер страницы, от 1 до " + totalPages}
            className="min-h-10 px-3 py-2 text-center"
          />
        </div>

        <span className="text-xs text-[var(--app-muted)]">из {totalPages}</span>

        <AppButton
          type="submit"
          size="sm"
          variant="secondary"
          disabled={disabled}
        >
          Перейти
        </AppButton>
      </form>

      <div className="flex flex-wrap gap-2">
        <AppButton
          size="sm"
          variant="secondary"
          disabled={disabled || page >= totalPages}
          onClick={() => onPageChange(page + 1)}
        >
          Вперёд
        </AppButton>

        <AppButton
          size="sm"
          variant="secondary"
          disabled={disabled || page >= totalPages}
          onClick={() => onPageChange(totalPages)}
        >
          В конец
        </AppButton>
      </div>
    </nav>
  );
}

function CatalogImportRowProblems({ row }: { row: CatalogImportRow }) {
  const errorCount = row.issues.length;
  const warningCount = row.warnings.length;
  const firstIssue = row.issues[0] ?? row.warnings[0];

  if (!firstIssue) {
    return <span className="text-[var(--app-muted)]">—</span>;
  }

  const message =
    firstIssue.message.trim() ||
    firstIssue.code.trim() ||
    "Описание отсутствует. Откройте подробности строки.";

  return (
    <div className="w-60 max-w-full space-y-1.5">
      <div className="flex flex-wrap gap-2">
        {errorCount > 0 && (
          <span className="rounded-full border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] px-2.5 py-1 text-xs font-medium text-[var(--app-danger)]">
            Ошибок: {errorCount}
          </span>
        )}

        {warningCount > 0 && (
          <span className="rounded-full border border-[var(--app-warning-border)] bg-[var(--app-warning-soft)] px-2.5 py-1 text-xs font-medium text-[var(--app-warning)]">
            Предупреждений: {warningCount}
          </span>
        )}
      </div>

      <p
        title={message}
        className={[
          "line-clamp-2 break-words text-xs leading-4",
          errorCount > 0
            ? "text-[var(--app-danger)]"
            : "text-[var(--app-warning)]",
        ].join(" ")}
      >
        {message}
      </p>
    </div>
  );
}

function CatalogImportRowDetails({ row }: { row: CatalogImportRow }) {
  const rawDataEntries = getRawDataEntries(row);

  const characteristicEntries = Object.entries(row.data.characteristics).sort(
    ([leftKey], [rightKey]) => leftKey.localeCompare(rightKey),
  );

  return (
    <div className="grid gap-5">
      {(row.issues.length > 0 || row.warnings.length > 0) && (
        <div className="grid gap-4 xl:grid-cols-2">
          <IssueCollection title="Ошибки" items={row.issues} type="error" />

          <IssueCollection
            title="Предупреждения"
            items={row.warnings}
            type="warning"
          />
        </div>
      )}

      <div className="grid gap-4 xl:grid-cols-2">
        <div className="rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel)] p-4">
          <h3 className="font-semibold text-[var(--app-text)]">
            Исходные значения Excel
          </h3>

          {rawDataEntries.length === 0 ? (
            <p className="mt-3 text-sm text-[var(--app-muted)]">
              Исходные значения отсутствуют.
            </p>
          ) : (
            <div className="mt-4 grid gap-2">
              {rawDataEntries.map(([columnNumber, value]) => (
                <div
                  key={columnNumber}
                  className="grid gap-1 rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] p-3 sm:grid-cols-[120px_1fr]"
                >
                  <p className="text-xs font-medium text-[var(--app-muted)]">
                    Колонка {columnNumber}
                  </p>

                  <p className="break-words text-sm text-[var(--app-text)]">
                    {value || "—"}
                  </p>
                </div>
              ))}
            </div>
          )}
        </div>

        <div className="rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel)] p-4">
          <h3 className="font-semibold text-[var(--app-text)]">
            Характеристики
          </h3>

          {characteristicEntries.length === 0 ? (
            <p className="mt-3 text-sm text-[var(--app-muted)]">
              Характеристики пока не определены.
            </p>
          ) : (
            <div className="mt-4 grid gap-2">
              {characteristicEntries.map(([characteristicId, value]) => (
                <div
                  key={characteristicId}
                  className="rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] p-3"
                >
                  <p className="break-all text-xs text-[var(--app-muted)]">
                    {characteristicId}
                  </p>

                  <p className="mt-1 break-words text-sm text-[var(--app-text)]">
                    {value || "—"}
                  </p>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

function IssueCollection({
  title,
  items,
  type,
}: {
  title: string;
  items: CatalogImportRowIssue[];
  type: "error" | "warning";
}) {
  const isError = type === "error";

  return (
    <div
      className={[
        "rounded-2xl border p-4",
        isError
          ? "border-[var(--app-danger-border)] bg-[var(--app-danger-soft)]"
          : "border-[var(--app-warning-border)] bg-[var(--app-warning-soft)]",
      ].join(" ")}
    >
      <h3
        className={
          isError
            ? "font-semibold text-[var(--app-danger)]"
            : "font-semibold text-[var(--app-warning)]"
        }
      >
        {title}
      </h3>

      {items.length === 0 ? (
        <p className="mt-3 text-sm text-[var(--app-muted)]">Нет.</p>
      ) : (
        <div className="mt-4 grid gap-3">
          {items.map((item, index) => (
            <div
              key={[
                item.code,
                item.field ?? "",
                item.sourceColumnNumber ?? "",
                index,
              ].join("-")}
              className="rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] p-3"
            >
              <p
                className={
                  isError
                    ? "text-sm text-[var(--app-danger)]"
                    : "text-sm text-[var(--app-warning)]"
                }
              >
                {item.message}
              </p>

              <div className="mt-2 flex flex-wrap gap-x-4 gap-y-1 text-xs text-[var(--app-muted)]">
                <span>Код: {item.code}</span>

                {item.field && <span>Поле: {item.field}</span>}

                {item.sourceColumnNumber !== null &&
                  item.sourceColumnNumber !== undefined && (
                    <span>Колонка: {item.sourceColumnNumber}</span>
                  )}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
