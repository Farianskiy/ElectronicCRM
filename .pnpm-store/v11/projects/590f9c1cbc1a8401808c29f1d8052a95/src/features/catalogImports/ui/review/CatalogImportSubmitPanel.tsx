"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { submitCatalogImportBatch } from "../../api/submitCatalogImportBatch";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { catalogImportQueryKeys } from "../../model/queryKeys";

interface CatalogImportSubmitPanelProps {
  batchId: string;
  originalFileName: string;
  rowsCount: number;
  validRowsCount: number;
  errorRowsCount: number;
  canSubmit: boolean;
}

export function CatalogImportSubmitPanel({
  batchId,
  originalFileName,
  rowsCount,
  validRowsCount,
  errorRowsCount,
  canSubmit,
}: CatalogImportSubmitPanelProps) {
  const queryClient = useQueryClient();

  const submitMutation = useMutation({
    mutationFn: () => submitCatalogImportBatch(batchId),

    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: catalogImportQueryKeys.details(batchId),
        }),

        queryClient.invalidateQueries({
          queryKey: catalogImportQueryKeys.myRoot,
        }),

        queryClient.invalidateQueries({
          queryKey: catalogImportQueryKeys.mapping(batchId),
        }),

        queryClient.invalidateQueries({
          queryKey: catalogImportQueryKeys.history(batchId),
        }),
      ]);
    },
  });

  if (!canSubmit) {
    return null;
  }

  function handleSubmit(): void {
    const confirmed = window.confirm(
      [
        `Отправить пакет «${originalFileName}» на техническую проверку?`,
        "",
        `Всего строк: ${rowsCount}`,
        `Корректных строк: ${validRowsCount}`,
        `Строк с ошибками: ${errorRowsCount}`,
        "",
        "После отправки редактирование пакета будет недоступно до решения Technical.",
      ].join("\n"),
    );

    if (!confirmed) {
      return;
    }

    submitMutation.mutate();
  }

  return (
    <section className="rounded-3xl border border-blue-500/30 bg-blue-500/[0.07] p-6">
      <div className="flex flex-col justify-between gap-5 lg:flex-row lg:items-center">
        <div>
          <h2 className="text-xl font-semibold text-blue-100">
            Пакет готов к проверке
          </h2>

          <p className="mt-2 max-w-3xl text-sm leading-6 text-slate-300">
            Все строки прошли валидацию. После отправки пакет попадёт в очередь
            технического специалиста, а редактирование будет временно
            заблокировано.
          </p>

          <div className="mt-4 flex flex-wrap gap-3 text-sm">
            <span className="rounded-full border border-white/10 bg-black/20 px-3 py-1 text-slate-300">
              Всего строк: {rowsCount}
            </span>

            <span className="rounded-full border border-green-500/30 bg-green-500/10 px-3 py-1 text-green-200">
              Корректных: {validRowsCount}
            </span>

            <span
              className={[
                "rounded-full border px-3 py-1",
                errorRowsCount > 0
                  ? "border-red-500/30 bg-red-500/10 text-red-200"
                  : "border-white/10 bg-black/20 text-slate-400",
              ].join(" ")}
            >
              С ошибками: {errorRowsCount}
            </span>
          </div>
        </div>

        <button
          type="button"
          disabled={submitMutation.isPending}
          onClick={handleSubmit}
          className="shrink-0 rounded-2xl bg-blue-500 px-6 py-3 text-sm font-semibold text-white transition hover:bg-blue-400 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {submitMutation.isPending ? "Отправляем..." : "Отправить на проверку"}
        </button>
      </div>

      {submitMutation.isError && (
        <div className="mt-5 rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-200">
          {getApiErrorMessage(
            submitMutation.error,
            "Не удалось отправить пакет на проверку.",
          )}
        </div>
      )}
    </section>
  );
}
