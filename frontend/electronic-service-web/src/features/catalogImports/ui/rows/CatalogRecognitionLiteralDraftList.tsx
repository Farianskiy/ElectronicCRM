"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useCurrentUserAccess } from "@/features/auth/model/CurrentUserAccessContext";
import { useAuthSession } from "@/features/auth/model/useAuthSession";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppButton } from "@/shared/ui/AppButton";
import {
  getRecognitionLiteralDrafts,
  getRecognitionLiteralDraftDetails,
  recognitionLiteralDraftsQueryRoot,
} from "../../api/getRecognitionLiteralDrafts";
import type { RecognitionTrainingScope } from "../../model/recognitionLiteralProposals";
import { CatalogRecognitionLiteralDraftRecheckPanel } from "./CatalogRecognitionLiteralDraftRecheckPanel";
import { CatalogRecognitionLiteralBatchPreviewPanel } from "./CatalogRecognitionLiteralBatchPreviewPanel";

interface CatalogRecognitionLiteralDraftListProps {
  batchId: string;
  scope: RecognitionTrainingScope;
}

export function CatalogRecognitionLiteralDraftList({
  batchId,
  scope,
}: CatalogRecognitionLiteralDraftListProps) {
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
      ...recognitionLiteralDraftsQueryRoot,
      "list",
      session?.userId,
      scope,
      page,
    ],
    queryFn: () => getRecognitionLiteralDrafts(scope, page),
    enabled: canRead,
    staleTime: 0,
  });

  if (!canRead) {
    return null;
  }

  return (
    <section className="grid gap-3 rounded-xl border border-[var(--app-border)] p-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h4 className="font-semibold">Сохранённые черновики</h4>
        <AppButton
          type="button"
          variant="secondary"
          disabled={query.isFetching}
          onClick={() => void query.refetch()}
        >
          Обновить список
        </AppButton>
      </div>

      <p className="text-sm text-[var(--app-muted)]">
        Черновики выбранного производителя, типа товара и характеристики. Они
        пока не применяются при распознавании.
      </p>

      {query.isPending && <p role="status">Загружаем черновики...</p>}

      {query.isError && (
        <p role="alert" className="text-sm text-[var(--app-danger)]">
          {getApiErrorMessage(query.error, "Не удалось загрузить черновики.")}
        </p>
      )}

      {query.isSuccess && (
        <>
          {query.data.items.length === 0 && (
            <p className="text-sm text-[var(--app-muted)]">
              На этой странице нет сохранённых черновиков.
            </p>
          )}

          {query.data.items.map((draft) => (
            <div
              key={draft.id}
              className="grid gap-2 rounded-xl border border-[var(--app-border)] p-3 text-sm"
            >
              <p className="font-semibold">
                «{draft.literal}» → {draft.normalizedValue}
              </p>
              <p>
                Сохранён: {new Date(draft.createdAtUtc).toLocaleString("ru-RU")}
              </p>
              <p>Версия генератора: {draft.generatorVersion}</p>
              <p>
                Названий с совпадением при сохранении: {draft.matchedNameCount}
              </p>
              <p>
                Проверено подтверждений: {draft.checkedExampleCount};
                поддерживающих: {draft.supportingExampleCount}
              </p>
              <p className="break-all text-xs text-[var(--app-muted)]">
                Идентификатор: {draft.id}
              </p>
              <CatalogRecognitionLiteralDraftEvidencePanel draftId={draft.id} />
              <CatalogRecognitionLiteralBatchPreviewPanel
                key={`${session?.userId}-${draft.id}-${batchId}`}
                draftId={draft.id}
                batchId={batchId}
              />
              <CatalogRecognitionLiteralDraftRecheckPanel
                key={`${session?.userId}-${draft.id}`}
                draftId={draft.id}
              />
            </div>
          ))}
        </>
      )}

      <div className="flex flex-wrap items-center gap-2">
        <AppButton
          type="button"
          variant="secondary"
          disabled={page <= 1 || query.isFetching}
          onClick={() => setPage((value) => Math.max(1, value - 1))}
        >
          Назад
        </AppButton>
        <span className="text-sm">Страница {page}</span>
        <AppButton
          type="button"
          variant="secondary"
          disabled={!query.isSuccess || !query.data.hasMore || query.isFetching}
          onClick={() => setPage((value) => value + 1)}
        >
          Далее
        </AppButton>
      </div>
    </section>
  );
}

function CatalogRecognitionLiteralDraftEvidencePanel({
  draftId,
}: {
  draftId: string;
}) {
  const [open, setOpen] = useState(false);
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
      ...recognitionLiteralDraftsQueryRoot,
      "details",
      session?.userId,
      draftId,
    ],
    queryFn: () => getRecognitionLiteralDraftDetails(draftId),
    enabled: open && canRead,
    staleTime: 0,
  });

  if (!canRead) {
    return null;
  }

  const evidence = query.isSuccess ? query.data.evidence : [];
  const visibleEvidence = evidence.slice((page - 1) * 20, page * 20);
  const revokedCount = evidence.filter(
    (item) => item.revokedAtUtc !== null,
  ).length;

  return (
    <div className="grid gap-3">
      <AppButton
        type="button"
        variant="secondary"
        onClick={() => setOpen((value) => !value)}
      >
        {open ? "Скрыть учебные примеры" : "Показать учебные примеры"}
      </AppButton>

      {open && (
        <>
          <AppButton
            type="button"
            variant="secondary"
            disabled={query.isFetching}
            onClick={() => void query.refetch()}
          >
            Обновить состояние примеров
          </AppButton>

          {query.isPending && <p role="status">Загружаем примеры...</p>}

          {query.isError && (
            <p role="alert" className="text-[var(--app-danger)]">
              {getApiErrorMessage(
                query.error,
                "Не удалось загрузить учебные примеры.",
              )}
            </p>
          )}

          {query.isSuccess && (
            <>
              <p className="text-[var(--app-muted)]">
                Состав проверки при сохранении черновика. Отзыв подтверждения
                отображается по текущим данным сервера; повторная проверка
                правила здесь не выполняется.
              </p>

              {revokedCount > 0 && (
                <p role="alert" className="text-[var(--app-danger)]">
                  Отозванных подтверждений: {revokedCount}. Этот черновик нельзя
                  считать актуальным без повторной проверки.
                </p>
              )}

              {evidence.length === 0 && <p>Связанные примеры не найдены.</p>}

              {visibleEvidence.map((item) => (
                <div
                  key={item.exampleId}
                  className="grid gap-1 rounded-xl border border-[var(--app-border)] p-3"
                >
                  <p>{item.productName}</p>
                  <p>
                    Выделенный фрагмент: «{item.rawValue}» →{" "}
                    {item.normalizedValue}
                  </p>
                  <p>
                    {item.isSupporting
                      ? "Поддерживал предложение при сохранении"
                      : "Проверен: предложение на этом примере не сработало"}
                  </p>
                  <p className="text-[var(--app-muted)]">
                    Подтверждён:{" "}
                    {new Date(item.confirmedAtUtc).toLocaleString("ru-RU")}
                  </p>
                  {item.revokedAtUtc && (
                    <p className="text-[var(--app-danger)]">
                      Подтверждение отозвано:{" "}
                      {new Date(item.revokedAtUtc).toLocaleString("ru-RU")}
                    </p>
                  )}
                  <p className="break-all text-xs text-[var(--app-muted)]">
                    Пример: {item.exampleId}
                  </p>
                </div>
              ))}

              {evidence.length > 20 && (
                <div className="flex flex-wrap items-center gap-2">
                  <AppButton
                    type="button"
                    variant="secondary"
                    disabled={page <= 1}
                    onClick={() => setPage((value) => Math.max(1, value - 1))}
                  >
                    Предыдущие примеры
                  </AppButton>
                  <span>Страница примеров {page}</span>
                  <AppButton
                    type="button"
                    variant="secondary"
                    disabled={page * 20 >= evidence.length}
                    onClick={() => setPage((value) => value + 1)}
                  >
                    Следующие примеры
                  </AppButton>
                </div>
              )}
            </>
          )}
        </>
      )}
    </div>
  );
}
