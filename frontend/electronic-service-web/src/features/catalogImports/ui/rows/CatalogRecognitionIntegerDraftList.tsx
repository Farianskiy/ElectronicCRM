"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useCurrentUserAccess } from "@/features/auth/model/CurrentUserAccessContext";
import { useAuthSession } from "@/features/auth/model/useAuthSession";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppButton } from "@/shared/ui/AppButton";
import { CatalogRecognitionIntegerDraftEvidencePanel } from "./CatalogRecognitionIntegerDraftEvidencePanel";
import { CatalogRecognitionIntegerDraftRecheckPanel } from "./CatalogRecognitionIntegerDraftRecheckPanel";
import { CatalogRecognitionIntegerDraftBatchPreviewPanel } from "./CatalogRecognitionIntegerDraftBatchPreviewPanel";
import {
  getRecognitionIntegerDrafts,
  recognitionIntegerDraftsQueryRoot,
} from "../../api/getRecognitionIntegerDrafts";
import type { RecognitionTrainingScope } from "../../model/recognitionLiteralProposals";

interface CatalogRecognitionIntegerDraftListProps {
  batchId: string;
  scope: RecognitionTrainingScope;
}

export function CatalogRecognitionIntegerDraftList({
  batchId,
  scope,
}: CatalogRecognitionIntegerDraftListProps) {
  const [page, setPage] = useState(1);
  const access = useCurrentUserAccess();
  const session = useAuthSession();
  const canRead =
    Boolean(session) &&
    !access.isLoading &&
    !access.isError &&
    access.hasPermission("DictionariesManage");

  const query = useQuery({
    queryKey: [
      ...recognitionIntegerDraftsQueryRoot,
      "list",
      session?.userId,
      scope,
      page,
    ],
    queryFn: () => getRecognitionIntegerDrafts(scope, page),
    enabled: canRead,
    staleTime: 0,
    retry: false,
  });

  if (!canRead) {
    return null;
  }

  function refresh(): void {
    if (query.isFetching) {
      return;
    }

    if (page !== 1) {
      setPage(1);
      return;
    }

    void query.refetch();
  }

  return (
    <section className="grid gap-3 rounded-xl border border-[var(--app-border)] p-4 text-sm">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h4 className="font-semibold">Сохранённые числовые шаблоны</h4>
        <AppButton
          type="button"
          variant="secondary"
          disabled={query.isFetching}
          onClick={refresh}
        >
          Обновить список
        </AppButton>
      </div>

      <p className="text-[var(--app-muted)]">
        Черновики выбранного производителя, типа товара и характеристики. Они
        ещё не включены в распознавание. Счётчики показывают результаты проверки
        при сохранении, а не текущую перепроверку.
      </p>

      {query.isFetching && <p role="status">Загружаем числовые шаблоны...</p>}

      {query.isError && (
        <p role="alert" className="text-[var(--app-danger)]">
          {getApiErrorMessage(
            query.error,
            "Не удалось загрузить числовые шаблоны.",
          )}
        </p>
      )}

      {query.isSuccess && (
        <>
          {query.data.items.length === 0 && (
            <p className="text-[var(--app-muted)]">
              На этой странице нет сохранённых числовых шаблонов.
            </p>
          )}

          {query.data.items.map((draft) => (
            <article
              key={draft.id}
              className="grid gap-2 rounded-lg border border-[var(--app-border)] p-3"
            >
              <p className="font-semibold">Черновик — не активирован</p>

              {draft.suffixes.map((suffix, index) => (
                <p
                  key={index}
                  className="whitespace-pre-wrap break-all font-mono"
                >
                  {draft.prefix}
                  <span className="font-bold">{"{число}"}</span>
                  {suffix}
                </p>
              ))}

              <p>
                Сохранён: {new Date(draft.createdAtUtc).toLocaleString("ru-RU")}
              </p>
              <p>Версия генератора: {draft.generatorVersion}</p>
              <p>
                Названий с совпадением при сохранении: {draft.matchedNameCount}
              </p>
              <p>Разных значений: {draft.distinctValueCount}</p>
              <p>
                Проверено подтверждений: {draft.checkedExampleCount};
                поддерживающих: {draft.supportingExampleCount}
              </p>
              <p className="break-all text-[var(--app-muted)]">
                Идентификатор: {draft.id}
              </p>

              <CatalogRecognitionIntegerDraftEvidencePanel
                key={`integer-evidence-${draft.id}`}
                draftId={draft.id}
              />

              <CatalogRecognitionIntegerDraftRecheckPanel
                key={`integer-recheck-${draft.id}`}
                draftId={draft.id}
              />

              <CatalogRecognitionIntegerDraftBatchPreviewPanel
                key={`integer-batch-preview-${draft.id}-${batchId}`}
                draftId={draft.id}
                batchId={batchId}
              />
            </article>
          ))}
        </>
      )}

      <div className="flex items-center gap-2">
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
          disabled={
            !query.isSuccess ||
            !query.data?.hasMore ||
            query.isFetching ||
            page >= 10000
          }
          onClick={() => setPage((value) => value + 1)}
        >
          Далее
        </AppButton>
      </div>
    </section>
  );
}
