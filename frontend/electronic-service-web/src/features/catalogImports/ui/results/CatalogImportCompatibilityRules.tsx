"use client";

import { useMemo, useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  applyComponentCompatibilityImport,
  previewComponentCompatibilityImportBatch,
} from "@/features/componentCompatibility/api/componentCompatibilityImport";
import type { ComponentCompatibilityImportPreviewRow } from "@/features/componentCompatibility/model/types";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppButton } from "@/shared/ui/AppButton";

interface CatalogImportCompatibilityRulesProps {
  batchId: string;
}

function isReadyRow(row: ComponentCompatibilityImportPreviewRow): boolean {
  return (
    (row.status === "WillCreate" || row.status === "WillUpdate") &&
    row.componentProductId !== null &&
    row.mainProductTypeId !== null
  );
}

function isBroadRow(row: ComponentCompatibilityImportPreviewRow): boolean {
  return (
    row.status === "RequiresConfirmation" &&
    row.componentProductId !== null &&
    row.mainProductTypeId !== null
  );
}

export function CatalogImportCompatibilityRules({
  batchId,
}: CatalogImportCompatibilityRulesProps) {
  const queryClient = useQueryClient();
  const [allowRulesWithoutConditions, setAllowRulesWithoutConditions] =
    useState(false);
  const previewMutation = useMutation({
    mutationFn: () => previewComponentCompatibilityImportBatch(batchId),
    onSuccess: () => setAllowRulesWithoutConditions(false),
  });
  const applyMutation = useMutation({
    mutationFn: applyComponentCompatibilityImport,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["component-offers"] });
    },
  });

  const rows = useMemo(
    () => previewMutation.data?.rows ?? [],
    [previewMutation.data?.rows],
  );
  const readyRows = useMemo(() => rows.filter(isReadyRow), [rows]);
  const broadRows = useMemo(() => rows.filter(isBroadRow), [rows]);
  const unchangedRowsCount = rows.filter(
    (row) => row.status === "Unchanged",
  ).length;
  const errorRows = rows.filter(
    (row) =>
      !isReadyRow(row) &&
      !isBroadRow(row) &&
      row.status !== "Unchanged",
  );

  function handleApply(): void {
    const rowsToApply = [...readyRows, ...broadRows];

    if (
      rowsToApply.length === 0 ||
      (broadRows.length > 0 && !allowRulesWithoutConditions)
    ) {
      return;
    }

    applyMutation.mutate({
      rows: rowsToApply.map((row) => ({
        componentProductId: row.componentProductId as string,
        mainProductTypeId: row.mainProductTypeId as string,
        needDefinitionId: row.needDefinitionId,
        isActive: row.isActive,
        constraints: row.constraints,
      })),
      allowRulesWithoutConditions,
    });
  }

  return (
    <section className="rounded-3xl border border-cyan-500/20 bg-cyan-500/[0.04] p-6">
      <div className="flex flex-col justify-between gap-4 md:flex-row md:items-center">
        <div>
          <h2 className="text-xl font-semibold text-white">
            Правила совместимости из этого Excel
          </h2>
          <p className="mt-2 max-w-3xl text-sm leading-6 text-slate-400">
            Товары уже загружены. Теперь можно проверить лист
            «Совместимость» и только после проверки создать или обновить
            правила.
          </p>
        </div>

        <AppButton
          type="button"
          variant="secondary"
          loading={previewMutation.isPending}
          onClick={() => {
            applyMutation.reset();
            previewMutation.mutate();
          }}
        >
          Проверить правила
        </AppButton>
      </div>

      {previewMutation.isError && (
        <div className="mt-5 rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-200">
          {getApiErrorMessage(
            previewMutation.error,
            "Не удалось проверить лист совместимости.",
          )}
        </div>
      )}

      {previewMutation.isSuccess && (
        <div className="mt-5 grid gap-4">
          <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
            <Summary label="Будет создано или изменено" value={readyRows.length} />
            <Summary label="Без изменений" value={unchangedRowsCount} />
            <Summary label="Нужно подтверждение" value={broadRows.length} />
            <Summary label="Ошибки" value={errorRows.length} />
          </div>

          {errorRows.length > 0 && (
            <div className="rounded-2xl border border-red-500/30 bg-red-500/10 p-4">
              <p className="font-medium text-red-200">
                Строки с ошибками не будут применены
              </p>
              <ul className="mt-3 grid gap-2 text-sm text-red-100/80">
                {errorRows.slice(0, 10).map((row) => (
                  <li key={`${row.rowNumber}-${row.componentArticle}`}>
                    Строка {row.rowNumber}, {row.componentArticle}: {row.message}
                  </li>
                ))}
              </ul>
            </div>
          )}

          {broadRows.length > 0 && (
            <label className="flex items-start gap-3 rounded-2xl border border-amber-500/40 bg-amber-500/10 p-4 text-sm text-amber-100">
              <input
                type="checkbox"
                checked={allowRulesWithoutConditions}
                onChange={(event) =>
                  setAllowRulesWithoutConditions(event.target.checked)
                }
                className="mt-1"
              />
              <span>
                Подтверждаю {broadRows.length} правил без условий. Такое
                комплектующее будет подходить всем товарам выбранного типа.
              </span>
            </label>
          )}

          <div className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-white/10 bg-black/20 p-4">
            <p className="text-sm text-slate-400">
              Применятся только корректные строки. Неизменённые правила будут
              пропущены.
            </p>
            <AppButton
              type="button"
              variant="primary"
              loading={applyMutation.isPending}
              disabled={
                readyRows.length + broadRows.length === 0 ||
                (broadRows.length > 0 && !allowRulesWithoutConditions) ||
                applyMutation.isSuccess
              }
              onClick={handleApply}
            >
              Применить правила
            </AppButton>
          </div>

          {applyMutation.isSuccess && (
            <div className="rounded-2xl border border-green-500/30 bg-green-500/10 p-4 text-sm text-green-200">
              Правила применены: {applyMutation.data.createdRulesCount}.
            </div>
          )}

          {applyMutation.isError && (
            <div className="rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-200">
              {getApiErrorMessage(
                applyMutation.error,
                "Не удалось применить правила совместимости.",
              )}
            </div>
          )}
        </div>
      )}
    </section>
  );
}

function Summary({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
      <p className="text-xs uppercase tracking-wide text-slate-500">{label}</p>
      <p className="mt-2 text-2xl font-semibold text-white">{value}</p>
    </div>
  );
}
