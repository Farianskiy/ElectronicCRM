"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useCurrentUserAccess } from "@/features/auth/model/CurrentUserAccessContext";
import { useAuthSession } from "@/features/auth/model/useAuthSession";
import type { CatalogProductTypeCharacteristicMetadata } from "@/features/catalogMetadata/model/types";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppButton } from "@/shared/ui/AppButton";
import {
  getRecognitionMultiIntegerDraftEvidence,
  type RecognitionMultiIntegerDraftEvidenceItem,
} from "../../api/getRecognitionMultiIntegerDraftEvidence";
import { recognitionMultiIntegerDraftsQueryRoot } from "../../api/getRecognitionMultiIntegerDrafts";

interface CatalogRecognitionMultiIntegerDraftEvidencePanelProps {
  draftId: string;
  characteristics: CatalogProductTypeCharacteristicMetadata[];
  disabled: boolean;
}

function HighlightedName({
  example,
}: {
  example: RecognitionMultiIntegerDraftEvidenceItem;
}) {
  const start = example.spanStart;
  const end = start + example.spanLength;

  const validSpan =
    Number.isInteger(start) &&
    Number.isInteger(example.spanLength) &&
    start >= 0 &&
    example.spanLength > 0 &&
    end <= example.productName.length &&
    example.productName.slice(start, end) === example.rawValue;

  if (!validSpan) {
    return (
      <div className="grid gap-1">
        <p className="whitespace-pre-wrap break-all">{example.productName}</p>
        <p className="text-[var(--app-danger)]">
          Сохранённый фрагмент не совпадает с позицией в названии. Требуется
          проверка данных.
        </p>
      </div>
    );
  }

  return (
    <p className="whitespace-pre-wrap break-all">
      {example.productName.slice(0, start)}
      <mark className="rounded bg-amber-200 px-0.5 text-slate-950">
        {example.productName.slice(start, end)}
      </mark>
      {example.productName.slice(end)}
    </p>
  );
}

export function CatalogRecognitionMultiIntegerDraftEvidencePanel({
  draftId,
  characteristics,
  disabled,
}: CatalogRecognitionMultiIntegerDraftEvidencePanelProps) {
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
      ...recognitionMultiIntegerDraftsQueryRoot,
      "evidence",
      session?.userId,
      draftId,
      page,
    ],
    queryFn: ({ signal }) =>
      getRecognitionMultiIntegerDraftEvidence(draftId, page, signal),
    enabled: canRead && expanded && !disabled,
    staleTime: 0,
    retry: false,
  });

  function characteristicName(id: string): string {
    return characteristics.find((item) => item.id === id)?.name ?? id;
  }

  if (!canRead) {
    return null;
  }

  return (
    <section className="grid gap-3 rounded-lg border border-[var(--app-border)] p-3">
      <AppButton
        type="button"
        variant="secondary"
        disabled={disabled}
        onClick={() => setExpanded((current) => !current)}
      >
        {expanded ? "Скрыть учебные примеры" : "Показать учебные примеры"}
      </AppButton>

      {expanded && !disabled && (
        <>
          <p className="text-[var(--app-muted)]">
            Это основания проверки при сохранении черновика. Одна запись —
            подтверждение одной характеристики. Отзыв подтверждения показан по
            последней загрузке с сервера; сам шаблон здесь не перепроверяется.
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
                "Не удалось загрузить основания черновика.",
              )}
            </p>
          )}

          {query.isSuccess && (
            <>
              {query.data.items.length === 0 && (
                <p className="text-[var(--app-muted)]">
                  На этой странице нет учебных примеров.
                </p>
              )}

              {query.data.items.map((example) => (
                <article
                  key={example.exampleId}
                  className="grid gap-2 rounded-lg border border-[var(--app-border)] p-3"
                >
                  <p className="font-semibold">
                    {characteristicName(example.characteristicDefinitionId)}
                  </p>

                  <HighlightedName example={example} />

                  <p className="whitespace-pre-wrap break-all">
                    Фрагмент: «{example.rawValue}» → {example.normalizedValue}
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
              disabled={query.isFetching || page <= 1}
              onClick={() => setPage((current) => Math.max(1, current - 1))}
            >
              Назад
            </AppButton>

            <span>Страница {page}</span>

            <AppButton
              type="button"
              variant="secondary"
              disabled={
                query.isFetching ||
                !query.isSuccess ||
                !query.data.hasMore ||
                page >= 10000
              }
              onClick={() => setPage((current) => current + 1)}
            >
              Далее
            </AppButton>
          </div>
        </>
      )}
    </section>
  );
}
