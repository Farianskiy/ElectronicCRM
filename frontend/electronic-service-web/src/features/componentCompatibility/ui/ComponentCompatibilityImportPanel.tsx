"use client";

import { useMemo, useState, type FormEvent } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  applyComponentCompatibilityImport,
  downloadComponentCompatibilityTemplate,
  previewComponentCompatibilityImport,
} from "../api/componentCompatibilityImport";
import type { ComponentCompatibilityImportRowStatus } from "../model/types";
import { useCurrentUserAccess } from "@/features/auth/model/CurrentUserAccessContext";
import { CreateCatalogProductDialog } from "@/features/catalogProducts/ui/CreateCatalogProductDialog";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppButton } from "@/shared/ui/AppButton";

function getStatusLabel(status: ComponentCompatibilityImportRowStatus): string {
  switch (status) {
    case "Ready":
      return "Будет создано";
    case "AlreadyExists":
      return "Уже существует";
    case "ComponentNotFound":
      return "Комплектующее не найдено";
    case "MainProductTypeNotFound":
      return "Тип товара не найден";
    case "NeedNotFound":
      return "Потребность не найдена";
    case "CharacteristicNotFound":
      return "Характеристика не найдена";
    case "Duplicate":
      return "Повтор в файле";
    case "Invalid":
      return "Некорректная строка";
  }
}

function getStatusClassName(status: ComponentCompatibilityImportRowStatus): string {
  if (status === "Ready") {
    return "border-[var(--app-success-border)] bg-[var(--app-success-soft)] text-[var(--app-success)]";
  }

  if (status === "AlreadyExists") {
    return "border-[var(--app-border)] bg-[var(--app-surface)] text-[var(--app-muted)]";
  }

  return "border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] text-[var(--app-danger)]";
}

export function ComponentCompatibilityImportPanel() {
  const queryClient = useQueryClient();
  const { hasPermission } = useCurrentUserAccess();
  const canEditProducts = hasPermission("ProductsEdit");
  const [file, setFile] = useState<File | null>(null);
  const [missingComponentArticle, setMissingComponentArticle] = useState<
    string | null
  >(null);
  const previewMutation = useMutation({
    mutationFn: previewComponentCompatibilityImport,
  });
  const applyMutation = useMutation({
    mutationFn: applyComponentCompatibilityImport,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["component-offers"] });
    },
  });
  const templateMutation = useMutation({
    mutationFn: downloadComponentCompatibilityTemplate,
  });
  const readyRows = useMemo(
    () =>
      (previewMutation.data?.rows ?? []).filter(
        (row) =>
          row.status === "Ready" &&
          row.componentProductId !== null &&
          row.needDefinitionId !== null,
      ),
    [previewMutation.data?.rows],
  );

  function handlePreview(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    if (!file) {
      return;
    }

    applyMutation.reset();
    previewMutation.mutate(file);
  }

  function handleApply(): void {
    if (readyRows.length === 0) {
      return;
    }

    applyMutation.mutate(
      readyRows.map((row) => ({
        componentProductId: row.componentProductId as string,
        needDefinitionId: row.needDefinitionId as string,
        constraints: row.constraints,
      })),
    );
  }

  async function handleComponentCreated(): Promise<void> {
    setMissingComponentArticle(null);
    await queryClient.invalidateQueries({ queryKey: ["catalog-products"] });

    if (!file) {
      return;
    }

    applyMutation.reset();
    previewMutation.mutate(file);
  }

  return (
    <section className="min-w-0 rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5 shadow-sm shadow-[var(--app-shadow)] sm:p-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <h2 className="text-lg font-semibold text-[var(--app-text)]">
          Правила совместимости комплектующих из Excel
        </h2>
        <AppButton
          type="button"
          size="sm"
          variant="secondary"
          loading={templateMutation.isPending}
          onClick={() => templateMutation.mutate()}
        >
          Скачать шаблон
        </AppButton>
      </div>
      <p className="mt-2 text-sm leading-6 text-[var(--app-muted)]">
        Одно правило действует сразу для всех подходящих основных товаров.
        Обязательные колонки: «Артикул комплектующего» (можно «Артикул»),
        «Тип основного товара» и «Потребность». Остальные заполненные колонки
        считаются характеристиками совместимости; варианты разделяются точкой с
        запятой.
      </p>

      <form
        onSubmit={handlePreview}
        className="mt-5 grid min-w-0 gap-4 lg:grid-cols-[minmax(280px,1fr)_auto] lg:items-end"
      >
        <label className="grid min-w-0 gap-2">
          <span className="text-sm font-medium text-[var(--app-text)]">
            XLSX-файл правил
          </span>
          <input
            type="file"
            accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            onChange={(event) => {
              setFile(event.target.files?.[0] ?? null);
              previewMutation.reset();
              applyMutation.reset();
            }}
            className="min-h-11 w-full rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] px-3 py-2 text-sm text-[var(--app-text)] file:mr-3 file:rounded-lg file:border-0 file:bg-[var(--app-accent-soft)] file:px-3 file:py-1.5 file:font-semibold file:text-[var(--app-accent)]"
          />
        </label>
        <AppButton
          type="submit"
          variant="primary"
          loading={previewMutation.isPending}
          disabled={!file}
        >
          Проверить правила
        </AppButton>
      </form>

      {previewMutation.isError && (
        <div
          role="alert"
          className="mt-4 rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm text-[var(--app-danger)]"
        >
          {getApiErrorMessage(
            previewMutation.error,
            "Не удалось проверить файл правил.",
          )}
        </div>
      )}

      {previewMutation.data && (
        <div className="mt-5 grid gap-4">
          <div className="grid gap-3 sm:grid-cols-3">
            <div className="rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4">
              <p className="text-sm text-[var(--app-muted)]">Прочитано</p>
              <p className="mt-1 text-2xl font-bold text-[var(--app-text)]">
                {previewMutation.data.readRowsCount}
              </p>
            </div>
            <div className="rounded-2xl border border-[var(--app-success-border)] bg-[var(--app-success-soft)] p-4">
              <p className="text-sm text-[var(--app-success)]">Будет создано</p>
              <p className="mt-1 text-2xl font-bold text-[var(--app-success)]">
                {previewMutation.data.readyRowsCount}
              </p>
            </div>
            <div className="rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4">
              <p className="text-sm text-[var(--app-danger)]">Пропущено</p>
              <p className="mt-1 text-2xl font-bold text-[var(--app-danger)]">
                {previewMutation.data.skippedRowsCount}
              </p>
            </div>
          </div>

          <div className="flex flex-col gap-3 rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4 sm:flex-row sm:items-center sm:justify-between">
            <p className="text-sm text-[var(--app-muted)]">
              Сначала проверьте строки ниже. Ошибочные и уже существующие
              правила применяться не будут.
            </p>
            <AppButton
              type="button"
              variant="primary"
              loading={applyMutation.isPending}
              disabled={readyRows.length === 0 || applyMutation.isSuccess}
              onClick={handleApply}
            >
              Создать правила
            </AppButton>
          </div>

          {applyMutation.isError && (
            <div
              role="alert"
              className="rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm text-[var(--app-danger)]"
            >
              {getApiErrorMessage(
                applyMutation.error,
                "Не удалось создать правила совместимости.",
              )}
            </div>
          )}

          {applyMutation.data && (
            <div
              role="status"
              className="rounded-2xl border border-[var(--app-success-border)] bg-[var(--app-success-soft)] p-4 text-sm text-[var(--app-success)]"
            >
              Создано правил: {applyMutation.data.createdRulesCount}.
            </div>
          )}

          <div className="overflow-x-auto rounded-2xl border border-[var(--app-border)]">
            <table className="w-full min-w-[1050px] border-collapse text-left text-sm">
              <thead className="bg-[var(--app-surface)] text-[var(--app-muted)]">
                <tr>
                  <th className="px-4 py-3 font-medium">Строка</th>
                  <th className="px-4 py-3 font-medium">Комплектующее</th>
                  <th className="px-4 py-3 font-medium">Тип основного товара</th>
                  <th className="px-4 py-3 font-medium">Потребность</th>
                  <th className="px-4 py-3 font-medium">Условия</th>
                  <th className="px-4 py-3 font-medium">Результат</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[var(--app-border)]">
                {previewMutation.data.rows.slice(0, 200).map((row) => (
                  <tr key={`${row.rowNumber}-${row.componentArticle}`}>
                    <td className="px-4 py-4 text-[var(--app-muted)]">
                      {row.rowNumber}
                    </td>
                    <td className="px-4 py-4 font-medium text-[var(--app-text)]">
                      {row.componentArticle || "—"}
                      {canEditProducts &&
                        row.status === "ComponentNotFound" &&
                        row.componentArticle.trim().length > 0 && (
                          <AppButton
                            type="button"
                            size="sm"
                            variant="secondary"
                            className="mt-3"
                            onClick={() =>
                              setMissingComponentArticle(row.componentArticle)
                            }
                          >
                            Добавить в каталог
                          </AppButton>
                        )}
                    </td>
                    <td className="px-4 py-4 text-[var(--app-text)]">
                      {row.mainProductType || "—"}
                    </td>
                    <td className="px-4 py-4 text-[var(--app-text)]">
                      {row.need || "—"}
                    </td>
                    <td className="px-4 py-4 text-[var(--app-muted)]">
                      {row.constraints.length === 0
                        ? "Без ограничений"
                        : row.constraints
                            .map(
                              (item) =>
                                `${item.characteristicName}: ${item.value}`,
                            )
                            .join("; ")}
                    </td>
                    <td className="px-4 py-4">
                      <span
                        className={`inline-flex rounded-full border px-3 py-1 text-xs font-semibold ${getStatusClassName(row.status)}`}
                      >
                        {getStatusLabel(row.status)}
                      </span>
                      {row.message && (
                        <p className="mt-2 text-xs text-[var(--app-muted)]">
                          {row.message}
                        </p>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {missingComponentArticle && (
        <CreateCatalogProductDialog
          article={missingComponentArticle}
          name={missingComponentArticle}
          manufacturerName={null}
          kind="Component"
          priceAmount={0}
          onClose={() => setMissingComponentArticle(null)}
          onCreated={handleComponentCreated}
        />
      )}
    </section>
  );
}
