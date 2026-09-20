"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useCurrentUserAccess } from "@/features/auth/model/CurrentUserAccessContext";
import { useAuthSession } from "@/features/auth/model/useAuthSession";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppButton } from "@/shared/ui/AppButton";
import { previewRecognitionLiteralDraftBatch } from "../../api/previewRecognitionLiteralDraftBatch";

interface CatalogRecognitionLiteralBatchPreviewPanelProps {
  draftId: string;
  batchId: string;
}

const statusLabels: Record<string, string> = {
  UnsupportedGeneratorVersion: "Версия генератора не поддерживается",
  InvalidDraft: "Некорректный черновик",
  ScopeUnknown: "Не определён производитель или тип",
  OutsideScope: "Другой производитель или тип",
  MissingName: "Нет наименования",
  NoMatch: "Фрагмент не найден",
  AmbiguousMatch: "Найдено несколько совпадений",
  SuggestedValue: "Предложено значение",
  MatchesCurrentValue: "Совпадает с текущим значением",
  DiffersFromCurrentValue: "Отличается от текущего значения",
};

export function CatalogRecognitionLiteralBatchPreviewPanel({
  draftId,
  batchId,
}: CatalogRecognitionLiteralBatchPreviewPanelProps) {
  const [started, setStarted] = useState(false);
  const [page, setPage] = useState(1);
  const session = useAuthSession();
  const access = useCurrentUserAccess();
  const canRead =
    Boolean(session) &&
    !access.isLoading &&
    !access.isError &&
    access.hasPermission("DictionariesManage");

  const query = useQuery({
    queryKey: [
      "recognition-literal-batch-preview",
      session?.userId,
      draftId,
      batchId,
      page,
    ],
    queryFn: () => previewRecognitionLiteralDraftBatch(draftId, batchId, page),
    enabled: started && canRead,
    staleTime: 0,
    gcTime: 0,
    retry: false,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
    placeholderData: () => undefined,
  });

  function check(): void {
    if (!canRead || query.isFetching) {
      return;
    }

    if (!started) {
      setStarted(true);
      return;
    }

    void query.refetch();
  }

  if (!canRead) {
    return null;
  }

  const result = query.isSuccess && !query.isFetching ? query.data : null;

  return (
    <section className="grid gap-3 rounded-xl border border-[var(--app-border)] p-3 text-sm">
      <AppButton
        type="button"
        variant="secondary"
        disabled={query.isFetching}
        onClick={check}
      >
        {query.isFetching ? "Проверяем строки..." : "Проверить на этом импорте"}
      </AppButton>

      <p className="text-[var(--app-muted)]">
        Проверяются сохранённые строки, по 25 на странице. Несохранённые
        изменения формы не учитываются. Повторное нажатие обновляет текущую
        страницу.
      </p>

      {started && query.isFetching && (
        <p role="status">Получаем строки и проверяем совпадения...</p>
      )}

      {started && query.isError && (
        <p role="alert" className="text-[var(--app-danger)]">
          {getApiErrorMessage(
            query.error,
            "Не удалось проверить строки импорта.",
          )}
        </p>
      )}

      {result && (
        <div className="grid gap-3">
          <p role="status">
            Страница {result.page}. Проверено строк на странице:{" "}
            {result.items.length}.
          </p>
          <p className="text-[var(--app-muted)]">
            Проверено: {new Date(result.checkedAtUtc).toLocaleString("ru-RU")}
          </p>

          {result.items.length === 0 && <p>На этой странице нет строк.</p>}

          {result.items.map((item) => (
            <div
              key={item.rowId}
              className="grid gap-1 rounded-lg border border-[var(--app-border)] p-3"
            >
              <p className="font-semibold">
                Строка {item.rowNumber}:{" "}
                {item.productName ?? "Без наименования"}
              </p>
              <p>
                {statusLabels[item.status] ??
                  `Неизвестный статус: ${item.status}`}
              </p>
              <p>Текущее значение: {item.currentValue || "Не заполнено"}</p>
              <p>
                Предложенное значение: {item.proposedValue ?? "Не предлагается"}
              </p>
              <p>Совпадений фрагмента: {item.matchPositions.length}</p>
            </div>
          ))}

          <p className="text-[var(--app-muted)]">
            Совпадение с текущим значением не подтверждает его правильность.
            Проверка не изменяет строки, не подтверждает учебные примеры и не
            активирует правило.
          </p>
        </div>
      )}

      {started && (
        <div className="flex flex-wrap items-center gap-2">
          <AppButton
            type="button"
            variant="secondary"
            disabled={page <= 1 || query.isFetching}
            onClick={() => setPage((value) => Math.max(1, value - 1))}
          >
            Назад
          </AppButton>
          <span>Страница {page}</span>
          <AppButton
            type="button"
            variant="secondary"
            disabled={!result?.hasMore || query.isFetching}
            onClick={() => setPage((value) => value + 1)}
          >
            Далее
          </AppButton>
        </div>
      )}
    </section>
  );
}
