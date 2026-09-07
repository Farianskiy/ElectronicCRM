"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useState, type FormEvent } from "react";
import {
  activateCatalogPriceList,
  bulkUpdateCatalogPriceListRows,
  getCatalogPriceList,
  getCatalogPriceListRows,
  processCatalogPriceList,
  updateCatalogPriceListRow,
} from "@/features/catalogPriceLists/api/catalogPriceListDetailsApi";
import { catalogPriceListQueryKeys } from "@/features/catalogPriceLists/model/queryKeys";
import {
  CatalogPriceListBulkRowEditor,
  CatalogPriceListSingleRowEditor,
} from "@/features/catalogPriceLists/ui/CatalogPriceListRowEditors";
import {
  catalogPriceListRowMatchStatuses,
  catalogPriceListRowStatuses,
  type BulkUpdateCatalogPriceListRowsRequest,
  type CatalogPriceListDetails,
  type CatalogPriceListRow,
  type CatalogPriceListRowIssue,
  type CatalogPriceListRowMatchStatus,
  type CatalogPriceListRowStatus,
  type CatalogPriceListStatus,
  type UpdateCatalogPriceListRowRequest,
} from "@/features/catalogPriceLists/model/types";
import { CatalogPriceListIssueGroupsPanel } from "@/features/catalogPriceLists/ui/CatalogPriceListIssueGroupsPanel";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import {
  formatDate,
  formatFileSize,
  formatPrice,
} from "@/shared/lib/formatters";
import { AppButton } from "@/shared/ui/AppButton";
import { AppInput } from "@/shared/ui/AppInput";
import { AppSelect } from "@/shared/ui/AppSelect";
import { PageWorkspace } from "@/shared/ui/PageWorkspace";

const rowsPageSize = 50;

interface CatalogPriceListRowSelectionGroup {
  key: string;
  issueCode: string;
  field: string;
  sourceValue: string;
}

function normalizeGroupValue(value: string): string {
  return value.trim().replace(/\s+/g, " ").toUpperCase();
}

function getIssueSourceValue(row: CatalogPriceListRow, field: string): string {
  switch (field.trim().toLowerCase()) {
    case "article":
      return row.article;
    case "name":
      return row.name;
    case "basepriceamount":
      return row.basePriceAmount?.toString() ?? "";
    case "mrcpriceamount":
      return row.mrcPriceAmount?.toString() ?? "";
    case "producturl":
      return row.productUrl ?? "";
    case "unit":
      return row.unit ?? "";
    case "productid":
      return row.article.trim() || row.name.trim();
    default:
      return "";
  }
}

function createRowSelectionGroup(
  row: CatalogPriceListRow,
  issue: CatalogPriceListRowIssue,
): CatalogPriceListRowSelectionGroup {
  const sourceValue = getIssueSourceValue(row, issue.field);
  const normalizedIssueCode = issue.code.trim().toLowerCase();
  const normalizedField = issue.field.trim().toLowerCase();
  const normalizedSourceValue = normalizeGroupValue(sourceValue);

  return {
    key: JSON.stringify([
      normalizedIssueCode,
      normalizedField,
      normalizedSourceValue,
    ]),
    issueCode: issue.code,
    field: issue.field,
    sourceValue,
  };
}

function rowBelongsToSelectionGroup(
  row: CatalogPriceListRow,
  group: CatalogPriceListRowSelectionGroup,
): boolean {
  return row.issues.some(
    (issue) => createRowSelectionGroup(row, issue).key === group.key,
  );
}

function getPriceListStatusLabel(status: CatalogPriceListStatus): string {
  switch (status) {
    case "Uploaded":
      return "Загружен";
    case "Processing":
      return "Обрабатывается";
    case "NeedsCorrection":
      return "Требует исправления";
    case "Ready":
      return "Готов к активации";
    case "Active":
      return "Активный";
    case "Archived":
      return "Архивный";
    case "Failed":
      return "Ошибка обработки";
  }
}

function getRowStatusLabel(status: CatalogPriceListRowStatus): string {
  switch (status) {
    case "Pending":
      return "Ожидает обработки";
    case "Valid":
      return "Корректная";
    case "Error":
      return "С ошибками";
  }
}

function getMatchStatusLabel(status: CatalogPriceListRowMatchStatus): string {
  switch (status) {
    case "Pending":
      return "Не проверено";
    case "MatchedByArticle":
      return "По артикулу";
    case "MatchedByName":
      return "По наименованию";
    case "MatchedManually":
      return "Вручную";
    case "Ambiguous":
      return "Несколько товаров";
    case "ProductNotFound":
      return "Товар не найден";
  }
}

function PriceListStatusBadge({ status }: { status: CatalogPriceListStatus }) {
  const className =
    status === "Active"
      ? "border-[var(--app-success-border)] bg-[var(--app-success-soft)] text-[var(--app-success)]"
      : status === "Ready"
        ? "border-[var(--app-accent-border)] bg-[var(--app-accent-soft)] text-[var(--app-accent)]"
        : status === "NeedsCorrection" || status === "Failed"
          ? "border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] text-[var(--app-danger)]"
          : "border-[var(--app-warning-border)] bg-[var(--app-warning-soft)] text-[var(--app-warning)]";

  return (
    <span
      className={`inline-flex rounded-full border px-3 py-1 text-xs font-semibold ${className}`}
    >
      {getPriceListStatusLabel(status)}
    </span>
  );
}

function PriceListRow({
  row,
  currency,
  selected,
  selectable,
  editable,
  editing,
  saving,
  saveErrorMessage,
  priceListId,
  onSelectedChange,
  onSelectIssueGroup,
  onEdit,
  onCancelEdit,
  onSave,
  onIssueCodeClick,
}: {
  row: CatalogPriceListRow;
  currency: string;
  selected: boolean;
  selectable: boolean;
  editable: boolean;
  editing: boolean;
  saving: boolean;
  saveErrorMessage?: string | null;
  priceListId: string;
  onSelectedChange: (rowId: string, selected: boolean) => void;
  onSelectIssueGroup: (
    row: CatalogPriceListRow,
    issue: CatalogPriceListRowIssue,
  ) => void;
  onEdit: (rowId: string) => void;
  onCancelEdit: () => void;
  onSave: (request: UpdateCatalogPriceListRowRequest) => void;
  onIssueCodeClick: (issueCode: string) => void;
}) {
  return (
    <>
      <tr className="bg-[var(--app-panel)] align-top">
        <td className="px-4 py-4">
          <input
            type="checkbox"
            checked={selected}
            disabled={!editable || !selectable}
            aria-label={`Выбрать строку ${row.rowNumber}`}
            title={
              selectable
                ? "Добавить строку в выбранную группу"
                : "Сначала выберите группу через одну из ошибок строки"
            }
            onChange={(event) =>
              onSelectedChange(row.rowId, event.target.checked)
            }
            className="h-4 w-4 rounded border-[var(--app-border)] accent-[var(--app-accent)]"
          />
        </td>

        <td className="px-4 py-4 tabular-nums text-[var(--app-muted)]">
          {row.rowNumber}
        </td>

        <td className="px-4 py-4">
          <p className="font-medium text-[var(--app-text)]">{row.name}</p>

          <p className="mt-1 text-xs text-[var(--app-muted)]">{row.article}</p>

          {row.productId && (
            <p className="mt-1 break-all text-xs text-[var(--app-accent)]">
              ProductId: {row.productId}
            </p>
          )}

          <p className="mt-1 text-xs text-[var(--app-muted)]">
            Единица: {row.unit || "—"}
          </p>
        </td>

        <td className="whitespace-nowrap px-4 py-4 tabular-nums text-[var(--app-text)]">
          {row.basePriceAmount === null || row.basePriceAmount === undefined
            ? "—"
            : formatPrice(row.basePriceAmount, currency)}
        </td>

        <td className="whitespace-nowrap px-4 py-4 tabular-nums text-[var(--app-muted)]">
          {row.mrcPriceAmount === null || row.mrcPriceAmount === undefined
            ? "—"
            : formatPrice(row.mrcPriceAmount, currency)}
        </td>

        <td className="px-4 py-4">
          <p
            className={
              row.status === "Error"
                ? "font-semibold text-[var(--app-danger)]"
                : "text-[var(--app-success)]"
            }
          >
            {getRowStatusLabel(row.status)}
          </p>

          <p className="mt-1 text-xs text-[var(--app-muted)]">
            {getMatchStatusLabel(row.matchStatus)}
          </p>

          {row.matchConfidencePercent !== null &&
            row.matchConfidencePercent !== undefined && (
              <p className="mt-1 text-xs text-[var(--app-muted)]">
                Уверенность: {row.matchConfidencePercent.toFixed(2)}%
              </p>
            )}
        </td>

        <td className="px-4 py-4">
          {row.issues.length > 0 ? (
            <div className="grid gap-2">
              {row.issues.map((issue, index) => {
                const group = createRowSelectionGroup(row, issue);

                return (
                  <div
                    key={`${issue.code}-${issue.field}-${index}`}
                    className="rounded-xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-3"
                  >
                    <span className="block text-xs font-semibold text-[var(--app-danger)]">
                      {issue.code}
                    </span>

                    <span className="mt-1 block text-sm text-[var(--app-danger)]">
                      {issue.message}
                    </span>

                    <span className="mt-1 block text-xs text-[var(--app-muted)]">
                      Поле: {issue.field}
                    </span>

                    <span className="mt-1 block break-all text-xs text-[var(--app-muted)]">
                      Значение группы: {group.sourceValue || "пустое значение"}
                    </span>

                    <div className="mt-3 flex flex-wrap gap-2">
                      <AppButton
                        type="button"
                        variant="ghost"
                        size="sm"
                        onClick={() => onIssueCodeClick(issue.code)}
                      >
                        Фильтр по коду
                      </AppButton>

                      <AppButton
                        type="button"
                        variant="danger"
                        size="sm"
                        disabled={!editable}
                        onClick={() => onSelectIssueGroup(row, issue)}
                      >
                        Выбрать одинаковые
                      </AppButton>
                    </div>
                  </div>
                );
              })}
            </div>
          ) : (
            <span className="text-[var(--app-muted)]">Нет</span>
          )}
        </td>

        <td className="px-4 py-4">
          <AppButton
            type="button"
            variant="secondary"
            size="sm"
            disabled={!editable || saving}
            onClick={() => onEdit(row.rowId)}
          >
            Редактировать
          </AppButton>
        </td>
      </tr>

      {editing && (
        <tr className="bg-[var(--app-panel)]">
          <td colSpan={8} className="px-4 pb-5">
            <CatalogPriceListSingleRowEditor
              key={`${row.rowId}-${row.article}-${row.name}-${row.productId ?? ""}-${row.unit ?? ""}`}
              priceListId={priceListId}
              row={row}
              saving={saving}
              errorMessage={saveErrorMessage}
              onSave={onSave}
              onCancel={onCancelEdit}
            />
          </td>
        </tr>
      )}
    </>
  );
}

export default function CatalogPriceListDetailsPage() {
  const params = useParams<{ priceListId: string }>();
  const queryClient = useQueryClient();

  const priceListId = params.priceListId ?? "";

  const [rowStatus, setRowStatus] = useState<CatalogPriceListRowStatus | null>(
    null,
  );
  const [matchStatus, setMatchStatus] =
    useState<CatalogPriceListRowMatchStatus | null>(null);
  const [issueCode, setIssueCode] = useState("");
  const [issueCodeInput, setIssueCodeInput] = useState("");
  const [page, setPage] = useState(1);
  const [selectedRowIds, setSelectedRowIds] = useState<Set<string>>(
    () => new Set<string>(),
  );
  const [editingRowId, setEditingRowId] = useState<string | null>(null);
  const [bulkEditorOpen, setBulkEditorOpen] = useState(false);
  const [activeSelectionGroup, setActiveSelectionGroup] =
    useState<CatalogPriceListRowSelectionGroup | null>(null);

  const priceListQuery = useQuery({
    queryKey: catalogPriceListQueryKeys.details(priceListId),
    queryFn: () => getCatalogPriceList(priceListId),
    enabled: priceListId.length > 0,
    refetchInterval: (query) =>
      query.state.data?.status === "Processing" ? 1_000 : false,
  });

  const rowsQuery = useQuery({
    queryKey: catalogPriceListQueryKeys.rows(
      priceListId,
      rowStatus,
      matchStatus,
      issueCode,
      page,
      rowsPageSize,
    ),
    queryFn: () =>
      getCatalogPriceListRows({
        priceListId,
        status: rowStatus,
        matchStatus,
        issueCode,
        page,
        pageSize: rowsPageSize,
      }),
    enabled:
      priceListId.length > 0 &&
      priceListQuery.isSuccess &&
      priceListQuery.data.rowsCount > 0,
    placeholderData: (previousData) => previousData,
  });

  async function refreshPriceList(): Promise<void> {
    await Promise.all([
      queryClient.invalidateQueries({
        queryKey: catalogPriceListQueryKeys.details(priceListId),
      }),
      queryClient.invalidateQueries({
        queryKey: catalogPriceListQueryKeys.rowsRoot(priceListId),
      }),
      queryClient.invalidateQueries({
        queryKey: catalogPriceListQueryKeys.versionsRoot,
      }),
    ]);
  }

  const processMutation = useMutation({
    mutationFn: processCatalogPriceList,
    onMutate: async () => {
      const detailsQueryKey = catalogPriceListQueryKeys.details(priceListId);

      await queryClient.cancelQueries({
        queryKey: detailsQueryKey,
      });

      queryClient.setQueryData<CatalogPriceListDetails>(
        detailsQueryKey,
        (current) =>
          current
            ? {
                ...current,
                status: "Processing",
                rowsCount: 0,
                validRowsCount: 0,
                errorRowsCount: 0,
                estimatedRowsCount: 0,
                readRowsCount: 0,
                savedRowsCount: 0,
                failureReason: null,
              }
            : current,
      );
    },
    onError: refreshPriceList,
    onSuccess: refreshPriceList,
  });

  const activateMutation = useMutation({
    mutationFn: activateCatalogPriceList,
    onSuccess: refreshPriceList,
  });

  const updateRowMutation = useMutation({
    mutationFn: ({
      rowId,
      request,
    }: {
      rowId: string;
      request: UpdateCatalogPriceListRowRequest;
    }) => updateCatalogPriceListRow(priceListId, rowId, request),
    onSuccess: async () => {
      setEditingRowId(null);
      await refreshPriceList();
    },
  });

  const bulkUpdateRowsMutation = useMutation({
    mutationFn: (request: BulkUpdateCatalogPriceListRowsRequest) =>
      bulkUpdateCatalogPriceListRows(priceListId, request),
    onSuccess: async () => {
      setSelectedRowIds(new Set<string>());
      setBulkEditorOpen(false);
      setActiveSelectionGroup(null);
      await refreshPriceList();
    },
  });

  const priceList = priceListQuery.data;
  const estimatedRowsCount = priceList?.estimatedRowsCount ?? 0;

  const readRowsCount = priceList?.readRowsCount ?? 0;

  const savedRowsCount = priceList?.savedRowsCount ?? 0;

  const processingPercent =
    estimatedRowsCount > 0
      ? Math.min(100, Math.round((readRowsCount / estimatedRowsCount) * 100))
      : 0;
  const rows = rowsQuery.data?.items ?? [];
  const totalRows = rowsQuery.data?.totalCount ?? 0;
  const backendTotalPages = rowsQuery.data?.totalPages ?? 0;
  const totalPages = Math.max(1, backendTotalPages);

  const selectedRows = rows.filter((row) => selectedRowIds.has(row.rowId));

  const selectableRows = activeSelectionGroup
    ? rows.filter((row) =>
        rowBelongsToSelectionGroup(row, activeSelectionGroup),
      )
    : [];

  const allVisibleRowsSelected =
    selectableRows.length > 0 &&
    selectableRows.every((row) => selectedRowIds.has(row.rowId));

  const canProcess =
    priceList?.status === "Uploaded" ||
    priceList?.status === "NeedsCorrection" ||
    priceList?.status === "Failed";

  const canActivate = priceList?.status === "Ready";

  const canEdit =
    priceList?.status === "NeedsCorrection" || priceList?.status === "Ready";

  function clearRowEditingContext(): void {
    setSelectedRowIds(new Set<string>());
    setEditingRowId(null);
    setBulkEditorOpen(false);
    setActiveSelectionGroup(null);
  }

  function handleSelectedChange(rowId: string, selected: boolean): void {
    if (!activeSelectionGroup) {
      return;
    }

    const row = rows.find((candidate) => candidate.rowId === rowId);

    if (!row || !rowBelongsToSelectionGroup(row, activeSelectionGroup)) {
      return;
    }

    setSelectedRowIds((current) => {
      const next = new Set(current);

      if (selected) {
        if (next.size >= 200) {
          return current;
        }

        next.add(rowId);
      } else {
        next.delete(rowId);
      }

      return next;
    });
  }

  function handleSelectIssueGroup(
    row: CatalogPriceListRow,
    issue: CatalogPriceListRowIssue,
  ): void {
    const group = createRowSelectionGroup(row, issue);
    const matchingRows = rows
      .filter((candidate) => rowBelongsToSelectionGroup(candidate, group))
      .slice(0, 200);

    setActiveSelectionGroup(group);
    setSelectedRowIds(
      new Set(matchingRows.map((candidate) => candidate.rowId)),
    );
    setEditingRowId(null);
    setBulkEditorOpen(false);
    updateRowMutation.reset();
    bulkUpdateRowsMutation.reset();
  }

  function handleSelectAllVisible(selected: boolean): void {
    if (!selected) {
      setSelectedRowIds(new Set<string>());
      setBulkEditorOpen(false);
      return;
    }

    if (!activeSelectionGroup) {
      return;
    }

    setSelectedRowIds(
      new Set(selectableRows.slice(0, 200).map((row) => row.rowId)),
    );
  }

  function handleEdit(rowId: string): void {
    setEditingRowId(rowId);
    setBulkEditorOpen(false);
    updateRowMutation.reset();
  }

  function handleSaveRow(
    rowId: string,
    request: UpdateCatalogPriceListRowRequest,
  ): void {
    updateRowMutation.mutate({
      rowId,
      request,
    });
  }

  function handleSaveSelectedRows(
    request: BulkUpdateCatalogPriceListRowsRequest,
  ): void {
    bulkUpdateRowsMutation.mutate(request);
  }

  function handleIssueCodeSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();
    setIssueCode(issueCodeInput.trim());
    setPage(1);
    clearRowEditingContext();
  }

  function handleIssueCodeClick(code: string): void {
    setIssueCodeInput(code);
    setIssueCode(code);
    setPage(1);
    clearRowEditingContext();
  }

  function handleResetFilters(): void {
    setRowStatus(null);
    setMatchStatus(null);
    setIssueCode("");
    setIssueCodeInput("");
    setPage(1);
    clearRowEditingContext();
  }

  function handleProcess(): void {
    const confirmed = window.confirm(
      priceList?.status === "Uploaded"
        ? "Запустить обработку прайс-листа?"
        : "Повторно обработать прайс-лист? Существующие строки будут сформированы заново.",
    );

    if (confirmed) {
      processMutation.mutate(priceListId);
    }
  }

  function handleActivate(): void {
    const confirmed = window.confirm(
      "Активировать эту версию? Предыдущая активная версия производителя будет автоматически отправлена в архив.",
    );

    if (confirmed) {
      activateMutation.mutate(priceListId);
    }
  }

  if (priceListQuery.isLoading) {
    return (
      <div
        role="status"
        className="flex min-h-64 items-center justify-center rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] text-sm text-[var(--app-muted)]"
      >
        Загружаем прайс-лист...
      </div>
    );
  }

  if (priceListQuery.isError || !priceList) {
    return (
      <PageWorkspace
        eyebrow="Настройка и качество"
        title="Прайс-лист не найден"
        description={getApiErrorMessage(
          priceListQuery.error,
          "Не удалось загрузить прайс-лист.",
        )}
      >
        <Link
          href="/catalog/price-lists"
          className="inline-flex min-h-11 w-fit items-center justify-center rounded-xl border border-[var(--app-button-secondary-border)] bg-[var(--app-button-secondary-bg)] px-4 py-2.5 text-sm font-semibold text-[var(--app-text)]"
        >
          Назад к прайс-листам
        </Link>
      </PageWorkspace>
    );
  }

  return (
    <PageWorkspace
      eyebrow="Прайс-листы"
      title={priceList.originalFileName}
      description={`${priceList.manufacturerName} · ${formatFileSize(priceList.fileSizeBytes)} · создан ${formatDate(priceList.createdAtUtc)}`}
      status={<PriceListStatusBadge status={priceList.status} />}
      contentClassName="grid min-w-0 gap-6"
      actions={
        <>
          <Link
            href="/catalog/price-lists"
            className="inline-flex min-h-11 items-center justify-center rounded-xl border border-[var(--app-button-secondary-border)] bg-[var(--app-button-secondary-bg)] px-4 py-2.5 text-sm font-semibold text-[var(--app-text)]"
          >
            Назад
          </Link>

          {canProcess && (
            <AppButton
              type="button"
              variant="warning"
              loading={processMutation.isPending}
              disabled={activateMutation.isPending}
              onClick={handleProcess}
            >
              {priceList.status === "Uploaded"
                ? "Обработать"
                : "Обработать повторно"}
            </AppButton>
          )}

          {canActivate && (
            <AppButton
              type="button"
              variant="primary"
              loading={activateMutation.isPending}
              disabled={processMutation.isPending}
              onClick={handleActivate}
            >
              Активировать
            </AppButton>
          )}
        </>
      }
    >
      {(processMutation.isError || activateMutation.isError) && (
        <section
          role="alert"
          className="rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm text-[var(--app-danger)]"
        >
          {getApiErrorMessage(
            processMutation.error ?? activateMutation.error,
            "Не удалось выполнить действие с прайс-листом.",
          )}
        </section>
      )}

      {priceList.failureReason && (
        <section
          role="alert"
          className="rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-5"
        >
          <h2 className="font-semibold text-[var(--app-danger)]">
            Ошибка обработки
          </h2>

          <p className="mt-2 whitespace-pre-wrap text-sm text-[var(--app-danger)]">
            {priceList.failureReason}
          </p>
        </section>
      )}

      {priceList.status === "Processing" && (
        <section
          aria-labelledby="price-list-progress-title"
          className="rounded-2xl border border-[var(--app-warning-border)] bg-[var(--app-warning-soft)] p-5"
        >
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div>
              <h2
                id="price-list-progress-title"
                className="font-semibold text-[var(--app-text)]"
              >
                Обработка прайс-листа
              </h2>

              <p className="mt-1 text-sm text-[var(--app-muted)]">
                {estimatedRowsCount > 0
                  ? `Прочитано ${readRowsCount.toLocaleString("ru-RU")} из приблизительно ${estimatedRowsCount.toLocaleString("ru-RU")} строк`
                  : `Прочитано ${readRowsCount.toLocaleString("ru-RU")} строк`}
              </p>

              <p className="mt-1 text-sm text-[var(--app-muted)]">
                Сохранено в базе: {savedRowsCount.toLocaleString("ru-RU")}
              </p>
            </div>

            <strong className="text-xl tabular-nums text-[var(--app-text)]">
              {processingPercent}%
            </strong>
          </div>

          <div className="mt-4 h-3 overflow-hidden rounded-full bg-[var(--app-panel)]">
            <div
              className="h-full rounded-full bg-[var(--app-accent)] transition-[width] duration-300"
              style={{
                width: `${processingPercent}%`,
              }}
            />
          </div>
        </section>
      )}

      <section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4"></section>

      <section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <div className="rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5">
          <p className="text-sm text-[var(--app-muted)]">Всего строк</p>

          <p className="mt-2 text-2xl font-bold tabular-nums text-[var(--app-text)]">
            {priceList.rowsCount}
          </p>
        </div>

        <div className="rounded-2xl border border-[var(--app-success-border)] bg-[var(--app-success-soft)] p-5">
          <p className="text-sm text-[var(--app-muted)]">Корректных</p>

          <p className="mt-2 text-2xl font-bold tabular-nums text-[var(--app-success)]">
            {priceList.validRowsCount}
          </p>
        </div>

        <div className="rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-5">
          <p className="text-sm text-[var(--app-muted)]">С ошибками</p>

          <p className="mt-2 text-2xl font-bold tabular-nums text-[var(--app-danger)]">
            {priceList.errorRowsCount}
          </p>
        </div>

        <div className="rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5">
          <p className="text-sm text-[var(--app-muted)]">НДС</p>

          <p className="mt-2 text-2xl font-bold tabular-nums text-[var(--app-text)]">
            {priceList.vatRatePercent.toFixed(2)}%
          </p>
        </div>
      </section>

      {priceList.rowsCount > 0 && (
        <>
          <CatalogPriceListIssueGroupsPanel
            priceListId={priceListId}
            canEdit={canEdit}
            errorRowsCount={priceList.errorRowsCount}
          />
          <section
            aria-labelledby="price-list-row-filters-title"
            className="rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5 sm:p-6"
          >
            <h2
              id="price-list-row-filters-title"
              className="text-xl font-semibold text-[var(--app-text)]"
            >
              Фильтры строк
            </h2>

            <div className="mt-5 grid gap-4 lg:grid-cols-3">
              <label className="grid gap-2">
                <span className="text-sm font-medium text-[var(--app-text)]">
                  Состояние строки
                </span>

                <AppSelect
                  ariaLabel="Состояние строки прайс-листа"
                  value={rowStatus ?? ""}
                  onChange={(value) => {
                    setRowStatus(
                      value ? (value as CatalogPriceListRowStatus) : null,
                    );
                    setPage(1);
                  }}
                  options={[
                    {
                      value: "",
                      label: "Все состояния",
                    },
                    ...catalogPriceListRowStatuses.map((status) => ({
                      value: status,
                      label: getRowStatusLabel(status),
                    })),
                  ]}
                />
              </label>

              <label className="grid gap-2">
                <span className="text-sm font-medium text-[var(--app-text)]">
                  Сопоставление
                </span>

                <AppSelect
                  ariaLabel="Состояние сопоставления товара"
                  value={matchStatus ?? ""}
                  onChange={(value) => {
                    setMatchStatus(
                      value ? (value as CatalogPriceListRowMatchStatus) : null,
                    );
                    setPage(1);
                  }}
                  options={[
                    {
                      value: "",
                      label: "Все варианты",
                    },
                    ...catalogPriceListRowMatchStatuses.map((status) => ({
                      value: status,
                      label: getMatchStatusLabel(status),
                    })),
                  ]}
                />
              </label>

              <form onSubmit={handleIssueCodeSubmit} className="grid gap-2">
                <span className="text-sm font-medium text-[var(--app-text)]">
                  Код ошибки
                </span>

                <div className="flex gap-2">
                  <AppInput
                    value={issueCodeInput}
                    onChange={(event) => setIssueCodeInput(event.target.value)}
                    placeholder="Например: product.not_found"
                  />

                  <AppButton type="submit" variant="secondary">
                    Применить
                  </AppButton>
                </div>
              </form>
            </div>

            <div className="mt-4 flex justify-end">
              <AppButton
                type="button"
                variant="ghost"
                onClick={handleResetFilters}
              >
                Сбросить фильтры
              </AppButton>
            </div>
          </section>

          <section
            aria-labelledby="price-list-rows-title"
            className="rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5 sm:p-6"
          >
            <div className="flex flex-col justify-between gap-3 sm:flex-row sm:items-center">
              <div>
                <h2
                  id="price-list-rows-title"
                  className="text-xl font-semibold text-[var(--app-text)]"
                >
                  Строки прайса
                </h2>

                <p className="mt-1 text-sm text-[var(--app-muted)]">
                  Найдено: {totalRows}. Страница {page} из {totalPages}.
                </p>
              </div>

              {rowsQuery.isFetching && (
                <p role="status" className="text-sm text-[var(--app-accent)]">
                  Обновляем строки...
                </p>
              )}
            </div>

            {canEdit && selectedRows.length > 0 && (
              <div className="mt-5 grid gap-4">
                <div className="flex flex-col gap-3 rounded-2xl border border-[var(--app-warning-border)] bg-[var(--app-warning-soft)] p-4 sm:flex-row sm:items-center sm:justify-between">
                  <div>
                    <p className="text-sm font-semibold text-[var(--app-text)]">
                      Выбрано строк: {selectedRows.length}
                    </p>

                    {activeSelectionGroup && (
                      <p className="mt-1 text-xs text-[var(--app-muted)]">
                        {activeSelectionGroup.issueCode} ·{" "}
                        {activeSelectionGroup.field} ·{" "}
                        {activeSelectionGroup.sourceValue || "пустое значение"}
                      </p>
                    )}
                  </div>

                  <div className="flex flex-wrap gap-2">
                    <AppButton
                      type="button"
                      variant="ghost"
                      disabled={bulkUpdateRowsMutation.isPending}
                      onClick={() => {
                        setSelectedRowIds(new Set<string>());
                        setBulkEditorOpen(false);
                        setActiveSelectionGroup(null);
                      }}
                    >
                      Снять выделение
                    </AppButton>

                    <AppButton
                      type="button"
                      variant="warning"
                      disabled={bulkUpdateRowsMutation.isPending}
                      onClick={() => {
                        setEditingRowId(null);
                        setBulkEditorOpen(true);
                        bulkUpdateRowsMutation.reset();
                      }}
                    >
                      Редактировать выбранные
                    </AppButton>
                  </div>
                </div>

                {bulkEditorOpen && (
                  <CatalogPriceListBulkRowEditor
                    key={selectedRows
                      .map((row) => row.rowId)
                      .sort()
                      .join("|")}
                    priceListId={priceListId}
                    rows={selectedRows}
                    groupIssueCode={activeSelectionGroup?.issueCode ?? ""}
                    groupField={activeSelectionGroup?.field ?? ""}
                    groupSourceValue={activeSelectionGroup?.sourceValue ?? ""}
                    saving={bulkUpdateRowsMutation.isPending}
                    errorMessage={
                      bulkUpdateRowsMutation.isError
                        ? getApiErrorMessage(
                            bulkUpdateRowsMutation.error,
                            "Не удалось изменить выбранные строки.",
                          )
                        : null
                    }
                    onSave={handleSaveSelectedRows}
                    onCancel={() => {
                      setBulkEditorOpen(false);
                      bulkUpdateRowsMutation.reset();
                    }}
                  />
                )}
              </div>
            )}

            {rowsQuery.isError ? (
              <div
                role="alert"
                className="mt-5 rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm text-[var(--app-danger)]"
              >
                {getApiErrorMessage(
                  rowsQuery.error,
                  "Не удалось загрузить строки прайса.",
                )}
              </div>
            ) : rowsQuery.isLoading ? (
              <div
                role="status"
                className="mt-5 flex min-h-32 items-center justify-center rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] text-sm text-[var(--app-muted)]"
              >
                Загружаем строки...
              </div>
            ) : rows.length > 0 ? (
              <div className="mt-5 overflow-x-auto rounded-2xl border border-[var(--app-border)]">
                <table className="w-full min-w-[1250px] table-fixed border-collapse text-left text-sm">
                  <caption className="sr-only">Строки прайс-листа</caption>

                  <colgroup>
                    <col className="w-[4%]" />
                    <col className="w-[6%]" />
                    <col className="w-[23%]" />
                    <col className="w-[12%]" />
                    <col className="w-[12%]" />
                    <col className="w-[14%]" />
                    <col className="w-[21%]" />
                    <col className="w-[8%]" />
                  </colgroup>

                  <thead className="bg-[var(--app-surface)] text-[var(--app-muted)]">
                    <tr>
                      <th className="px-4 py-3 font-medium">
                        <input
                          type="checkbox"
                          checked={allVisibleRowsSelected}
                          disabled={!canEdit || rows.length === 0}
                          aria-label="Выбрать все строки страницы"
                          onChange={(event) =>
                            handleSelectAllVisible(event.target.checked)
                          }
                          className="h-4 w-4 rounded border-[var(--app-border)] accent-[var(--app-accent)]"
                        />
                      </th>
                      <th className="px-4 py-3 font-medium">Строка</th>
                      <th className="px-4 py-3 font-medium">Товар</th>
                      <th className="px-4 py-3 font-medium">Прайс 100%</th>
                      <th className="px-4 py-3 font-medium">МРЦ</th>
                      <th className="px-4 py-3 font-medium">Состояние</th>
                      <th className="px-4 py-3 font-medium">Проблемы</th>
                      <th className="px-4 py-3 font-medium">Действия</th>
                    </tr>
                  </thead>

                  <tbody className="divide-y divide-[var(--app-border)]">
                    {rows.map((row) => (
                      <PriceListRow
                        key={row.rowId}
                        row={row}
                        currency={priceList.currency}
                        selected={selectedRowIds.has(row.rowId)}
                        selectable={
                          activeSelectionGroup !== null &&
                          rowBelongsToSelectionGroup(row, activeSelectionGroup)
                        }
                        editable={canEdit}
                        editing={editingRowId === row.rowId}
                        saving={
                          updateRowMutation.isPending &&
                          editingRowId === row.rowId
                        }
                        saveErrorMessage={
                          updateRowMutation.isError &&
                          editingRowId === row.rowId
                            ? getApiErrorMessage(
                                updateRowMutation.error,
                                "Не удалось сохранить строку.",
                              )
                            : null
                        }
                        priceListId={priceListId}
                        onSelectedChange={handleSelectedChange}
                        onSelectIssueGroup={handleSelectIssueGroup}
                        onEdit={handleEdit}
                        onCancelEdit={() => {
                          setEditingRowId(null);
                          updateRowMutation.reset();
                        }}
                        onSave={(request) => handleSaveRow(row.rowId, request)}
                        onIssueCodeClick={handleIssueCodeClick}
                      />
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <div className="mt-5 rounded-2xl border border-dashed border-[var(--app-border-strong)] bg-[var(--app-surface)] p-6 text-sm text-[var(--app-muted)]">
                Строки с выбранными условиями не найдены.
              </div>
            )}

            <nav
              aria-label="Страницы строк прайс-листа"
              className="mt-5 flex items-center justify-between border-t border-[var(--app-border)] pt-4"
            >
              <AppButton
                type="button"
                variant="secondary"
                disabled={page <= 1 || rowsQuery.isFetching}
                onClick={() => {
                  clearRowEditingContext();
                  setPage((current) => Math.max(1, current - 1));
                }}
              >
                Назад
              </AppButton>

              <AppButton
                type="button"
                variant="secondary"
                disabled={
                  page >= totalPages ||
                  backendTotalPages === 0 ||
                  rowsQuery.isFetching
                }
                onClick={() => {
                  clearRowEditingContext();
                  setPage((current) => Math.min(totalPages, current + 1));
                }}
              >
                Вперёд
              </AppButton>
            </nav>
          </section>
        </>
      )}
    </PageWorkspace>
  );
}
