"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState, type FormEvent } from "react";
import {
  applyCatalogPriceListIssueGroup,
  getCatalogPriceListIssueGroups,
} from "@/features/catalogPriceLists/api/catalogPriceListDetailsApi";
import { catalogPriceListQueryKeys } from "@/features/catalogPriceLists/model/queryKeys";
import type {
  ApplyCatalogPriceListIssueGroupRequest,
  ApplyCatalogPriceListIssueGroupResponse,
  CatalogPriceListIssueGroup,
  CatalogPriceListProductSearchItem,
} from "@/features/catalogPriceLists/model/types";
import { ProductPicker } from "@/features/catalogPriceLists/ui/CatalogPriceListRowEditors";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppButton } from "@/shared/ui/AppButton";
import { AppInput } from "@/shared/ui/AppInput";

const groupsPageSize = 25;

function supportsGroupCorrection(field: string): boolean {
  const normalizedField = field.trim().toLowerCase();

  return normalizedField === "unit" || normalizedField === "productid";
}

function IssueGroupEditor({
  priceListId,
  group,
  saving,
  errorMessage,
  onSave,
  onCancel,
}: {
  priceListId: string;
  group: CatalogPriceListIssueGroup;
  saving: boolean;
  errorMessage?: string | null;
  onSave: (request: ApplyCatalogPriceListIssueGroupRequest) => void;
  onCancel: () => void;
}) {
  const normalizedField = group.field.trim().toLowerCase();

  const isUnitGroup = normalizedField === "unit";

  const isProductGroup = normalizedField === "productid";

  const [unit, setUnit] = useState("");
  const [selectedProduct, setSelectedProduct] =
    useState<CatalogPriceListProductSearchItem | null>(null);
  const [validationError, setValidationError] = useState<string | null>(null);

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    if (isProductGroup && !selectedProduct) {
      setValidationError("Найдите и выберите товар производителя.");

      return;
    }

    if (!isUnitGroup && !isProductGroup) {
      setValidationError(
        `Групповое исправление поля «${group.field}» не поддерживается.`,
      );

      return;
    }

    setValidationError(null);

    if (isProductGroup) {
      onSave({
        unit: null,
        productId: selectedProduct!.productId,
      });

      return;
    }

    onSave({
      unit: unit.trim() || null,
      productId: null,
    });
  }

  return (
    <form
      onSubmit={handleSubmit}
      className="mt-4 grid gap-5 rounded-2xl border border-[var(--app-warning-border)] bg-[var(--app-warning-soft)] p-5"
    >
      <div>
        <h3 className="text-lg font-semibold text-[var(--app-text)]">
          Исправление всей группы
        </h3>

        <p className="mt-1 text-sm text-[var(--app-muted)]">
          Backend повторно найдёт и обработает все строки этой группы, а не
          только строки текущей страницы.
        </p>

        <dl className="mt-4 grid gap-2 rounded-xl border border-[var(--app-border)] bg-[var(--app-panel)] p-4 text-sm">
          <div className="flex justify-between gap-4">
            <dt className="text-[var(--app-muted)]">Код ошибки</dt>

            <dd className="font-semibold text-[var(--app-text)]">
              {group.issueCode}
            </dd>
          </div>

          <div className="flex justify-between gap-4">
            <dt className="text-[var(--app-muted)]">Поле</dt>

            <dd className="font-semibold text-[var(--app-text)]">
              {group.field}
            </dd>
          </div>

          <div className="flex justify-between gap-4">
            <dt className="text-[var(--app-muted)]">Исходное значение</dt>

            <dd className="break-all text-right font-semibold text-[var(--app-text)]">
              {group.sourceValue || "пустое значение"}
            </dd>
          </div>

          <div className="flex justify-between gap-4">
            <dt className="text-[var(--app-muted)]">Будет обработано строк</dt>

            <dd className="font-semibold tabular-nums text-[var(--app-warning)]">
              {group.rowsCount}
            </dd>
          </div>
        </dl>
      </div>

      {isUnitGroup && (
        <label className="grid gap-2">
          <span className="text-sm font-medium text-[var(--app-text)]">
            Новая единица измерения
          </span>

          <AppInput
            value={unit}
            disabled={saving}
            onChange={(event) => setUnit(event.target.value)}
            placeholder="Например: шт"
          />
        </label>
      )}

      {isProductGroup && (
        <ProductPicker
          priceListId={priceListId}
          selectedProductId={selectedProduct?.productId}
          disabled={saving}
          onSelect={setSelectedProduct}
          onClear={() => setSelectedProduct(null)}
        />
      )}

      {!isUnitGroup && !isProductGroup && (
        <div className="rounded-xl border border-[var(--app-warning-border)] bg-[var(--app-warning-soft)] p-4 text-sm text-[var(--app-warning)]">
          Это поле пока необходимо исправлять по отдельным строкам.
        </div>
      )}

      {(validationError || errorMessage) && (
        <div
          role="alert"
          className="rounded-xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm text-[var(--app-danger)]"
        >
          {validationError ?? errorMessage}
        </div>
      )}

      <div className="flex flex-wrap justify-end gap-2">
        <AppButton
          type="button"
          variant="secondary"
          disabled={saving}
          onClick={onCancel}
        >
          Отмена
        </AppButton>

        <AppButton
          type="submit"
          variant="warning"
          loading={saving}
          disabled={!isUnitGroup && !isProductGroup}
        >
          Применить к {group.rowsCount} строкам
        </AppButton>
      </div>
    </form>
  );
}

export function CatalogPriceListIssueGroupsPanel({
  priceListId,
  canEdit,
  errorRowsCount,
}: {
  priceListId: string;
  canEdit: boolean;
  errorRowsCount: number;
}) {
  const queryClient = useQueryClient();

  const [issueCodeInput, setIssueCodeInput] = useState("");
  const [issueCode, setIssueCode] = useState("");
  const [page, setPage] = useState(1);
  const [activeGroup, setActiveGroup] =
    useState<CatalogPriceListIssueGroup | null>(null);
  const [lastResult, setLastResult] =
    useState<ApplyCatalogPriceListIssueGroupResponse | null>(null);

  const groupsQuery = useQuery({
    queryKey: catalogPriceListQueryKeys.issueGroups(
      priceListId,
      issueCode,
      page,
      groupsPageSize,
    ),
    queryFn: () =>
      getCatalogPriceListIssueGroups({
        priceListId,
        issueCode,
        page,
        pageSize: groupsPageSize,
      }),
    enabled: priceListId.length > 0 && errorRowsCount > 0,
    placeholderData: (previousData) => previousData,
  });

  const applyMutation = useMutation({
    mutationFn: ({
      groupKey,
      request,
    }: {
      groupKey: string;
      request: ApplyCatalogPriceListIssueGroupRequest;
    }) => applyCatalogPriceListIssueGroup(priceListId, groupKey, request),
    onSuccess: async (result) => {
      setLastResult(result);
      setActiveGroup(null);

      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: catalogPriceListQueryKeys.details(priceListId),
        }),
        queryClient.invalidateQueries({
          queryKey: catalogPriceListQueryKeys.rowsRoot(priceListId),
        }),
        queryClient.invalidateQueries({
          queryKey: catalogPriceListQueryKeys.issueGroupsRoot(priceListId),
        }),
        queryClient.invalidateQueries({
          queryKey: catalogPriceListQueryKeys.versionsRoot,
        }),
      ]);
    },
  });

  const groups = groupsQuery.data?.items ?? [];

  const totalPages = Math.max(1, groupsQuery.data?.totalPages ?? 0);

  function handleFilterSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    setIssueCode(issueCodeInput.trim());
    setPage(1);
    setActiveGroup(null);
    applyMutation.reset();
  }

  function handleOpenGroup(group: CatalogPriceListIssueGroup): void {
    setActiveGroup(group);
    setLastResult(null);
    applyMutation.reset();
  }

  return (
    <section
      aria-labelledby="price-list-issue-groups-title"
      className="rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5 sm:p-6"
    >
      <div className="flex flex-col justify-between gap-3 lg:flex-row lg:items-start">
        <div>
          <h2
            id="price-list-issue-groups-title"
            className="text-xl font-semibold text-[var(--app-text)]"
          >
            Группы одинаковых ошибок
          </h2>

          <p className="mt-1 text-sm text-[var(--app-muted)]">
            Исправление применяется ко всей серверной группе, включая строки на
            других страницах.
          </p>
        </div>

        <form
          onSubmit={handleFilterSubmit}
          className="flex flex-col gap-2 sm:flex-row"
        >
          <AppInput
            value={issueCodeInput}
            onChange={(event) => setIssueCodeInput(event.target.value)}
            placeholder="Код ошибки"
          />

          <AppButton
            type="submit"
            variant="secondary"
            loading={groupsQuery.isFetching}
          >
            Найти
          </AppButton>

          <AppButton
            type="button"
            variant="ghost"
            onClick={() => {
              setIssueCodeInput("");
              setIssueCode("");
              setPage(1);
              setActiveGroup(null);
            }}
          >
            Сбросить
          </AppButton>
        </form>
      </div>

      {lastResult && (
        <div
          role="status"
          className="mt-5 rounded-2xl border border-[var(--app-success-border)] bg-[var(--app-success-soft)] p-4 text-sm text-[var(--app-success)]"
        >
          Исправлено строк: {lastResult.processedRowsCount}. Осталось строк с
          ошибками: {lastResult.errorRowsCount}.
        </div>
      )}

      {groupsQuery.isError && (
        <div
          role="alert"
          className="mt-5 rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm text-[var(--app-danger)]"
        >
          {getApiErrorMessage(
            groupsQuery.error,
            "Не удалось получить группы ошибок.",
          )}
        </div>
      )}

      {errorRowsCount === 0 && (
        <p className="mt-5 rounded-2xl border border-[var(--app-success-border)] bg-[var(--app-success-soft)] p-4 text-sm text-[var(--app-success)]">
          Ошибок, доступных для группировки, нет.
        </p>
      )}

      {groups.length > 0 && (
        <div className="mt-5 grid gap-3">
          {groups.map((group) => {
            const supported = supportsGroupCorrection(group.field);

            return (
              <article
                key={group.groupKey}
                className="rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4"
              >
                <div className="flex flex-col justify-between gap-4 lg:flex-row lg:items-center">
                  <div className="min-w-0">
                    <div className="flex flex-wrap gap-2">
                      <span className="rounded-full border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] px-3 py-1 text-xs font-semibold text-[var(--app-danger)]">
                        {group.issueCode}
                      </span>

                      <span className="rounded-full border border-[var(--app-border)] bg-[var(--app-panel)] px-3 py-1 text-xs text-[var(--app-muted)]">
                        {group.field}
                      </span>
                    </div>

                    <p className="mt-3 break-all font-medium text-[var(--app-text)]">
                      {group.sourceValue || "Пустое значение"}
                    </p>

                    <p className="mt-2 text-sm text-[var(--app-muted)]">
                      Строк: {group.rowsCount}. Примеры:{" "}
                      {group.exampleRowNumbers.join(", ")}
                    </p>
                  </div>

                  <AppButton
                    type="button"
                    variant={supported ? "warning" : "secondary"}
                    disabled={!canEdit || !supported || applyMutation.isPending}
                    onClick={() => handleOpenGroup(group)}
                  >
                    {supported ? "Исправить всю группу" : "Только вручную"}
                  </AppButton>
                </div>

                {activeGroup?.groupKey === group.groupKey && (
                  <IssueGroupEditor
                    key={group.groupKey}
                    priceListId={priceListId}
                    group={group}
                    saving={applyMutation.isPending}
                    errorMessage={
                      applyMutation.isError
                        ? getApiErrorMessage(
                            applyMutation.error,
                            "Не удалось исправить группу.",
                          )
                        : null
                    }
                    onSave={(request) =>
                      applyMutation.mutate({
                        groupKey: group.groupKey,
                        request,
                      })
                    }
                    onCancel={() => {
                      setActiveGroup(null);
                      applyMutation.reset();
                    }}
                  />
                )}
              </article>
            );
          })}
        </div>
      )}

      {groupsQuery.isSuccess && groups.length === 0 && errorRowsCount > 0 && (
        <p className="mt-5 rounded-2xl border border-dashed border-[var(--app-border-strong)] p-5 text-sm text-[var(--app-muted)]">
          Группы с выбранным кодом ошибки не найдены.
        </p>
      )}

      {groupsQuery.data && groupsQuery.data.totalPages > 1 && (
        <nav
          aria-label="Страницы групп ошибок"
          className="mt-5 flex items-center justify-between border-t border-[var(--app-border)] pt-4"
        >
          <AppButton
            type="button"
            variant="secondary"
            disabled={page <= 1 || groupsQuery.isFetching}
            onClick={() => {
              setActiveGroup(null);
              setPage((current) => Math.max(1, current - 1));
            }}
          >
            Назад
          </AppButton>

          <span className="text-sm text-[var(--app-muted)]">
            Страница {page} из {totalPages}
          </span>

          <AppButton
            type="button"
            variant="secondary"
            disabled={page >= totalPages || groupsQuery.isFetching}
            onClick={() => {
              setActiveGroup(null);
              setPage((current) => Math.min(totalPages, current + 1));
            }}
          >
            Вперёд
          </AppButton>
        </nav>
      )}
    </section>
  );
}
