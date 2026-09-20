"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useCurrentUserAccess } from "@/features/auth/model/CurrentUserAccessContext";
import { useAuthSession } from "@/features/auth/model/useAuthSession";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppButton } from "@/shared/ui/AppButton";
import { getRecognitionIntegerDraftEvidence } from "../../api/getRecognitionIntegerDraftEvidence";
import { recognitionIntegerDraftsQueryRoot } from "../../api/getRecognitionIntegerDrafts";

interface CatalogRecognitionIntegerDraftEvidencePanelProps {
  draftId: string;
}

export function CatalogRecognitionIntegerDraftEvidencePanel({
  draftId,
}: CatalogRecognitionIntegerDraftEvidencePanelProps) {
  const [expanded, setExpanded] = useState(false);
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
      "evidence",
      session?.userId,
      draftId,
      page,
    ],
    queryFn: () => getRecognitionIntegerDraftEvidence(draftId, page),
    enabled: canRead && expanded,
    staleTime: 0,
    retry: false,
  });

  if (!canRead) {
    return null;
  }

  return (
    <div className="grid gap-3">
      <AppButton
        type="button"
        variant="secondary"
        onClick={() => setExpanded((value) => !value)}
      >
        {expanded ? "Скрыть учебные примеры" : "Показать учебные примеры"}
      </AppButton>

      {expanded && (
        <div className="grid gap-3 rounded-lg border border-[var(--app-border)] p-3">
          <p className="text-[var(--app-muted)]">
            Основания проверки при сохранении черновика. Статус отзыва
            загружается с сервера. Повторная проверка шаблона здесь не
            выполняется.
          </p>

          <AppButton
            type="button"
            variant="secondary"
            disabled={query.isFetching}
            onClick={() => void query.refetch()}
          >
            Обновить состояние примеров
          </AppButton>

          {query.isFetching && (
            <p role="status">Загружаем учебные примеры...</p>
          )}

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
              {query.data.items.length === 0 && (
                <p>На этой странице нет учебных примеров.</p>
              )}

              {query.data.items.map((example) => (
                <article
                  key={example.exampleId}
                  className="grid gap-2 rounded-lg border border-[var(--app-border)] p-3"
                >
                  <p className="whitespace-pre-wrap break-words font-medium">
                    {example.productName}
                  </p>
                  <p>
                    Выделенный фрагмент: «{example.rawValue}» →{" "}
                    {example.normalizedValue}
                  </p>
                  <p>
                    {example.isSupporting
                      ? "Поддерживал шаблон при сохранении"
                      : "Входил в проверку, но не поддерживал этот шаблон"}
                  </p>
                  <p className="text-[var(--app-muted)]">
                    Подтверждён:{" "}
                    {new Date(example.confirmedAtUtc).toLocaleString("ru-RU")}
                  </p>

                  {example.revokedAtUtc ? (
                    <p className="text-[var(--app-danger)]">
                      Подтверждение отозвано:{" "}
                      {new Date(example.revokedAtUtc).toLocaleString("ru-RU")}.
                      Пример сохранён здесь как историческое основание.
                    </p>
                  ) : (
                    <p>Подтверждение не отозвано на момент загрузки.</p>
                  )}

                  <p className="break-all text-[var(--app-muted)]">
                    Пример: {example.exampleId}
                  </p>
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

            <span>Страница примеров {page}</span>

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
        </div>
      )}
    </div>
  );
}
