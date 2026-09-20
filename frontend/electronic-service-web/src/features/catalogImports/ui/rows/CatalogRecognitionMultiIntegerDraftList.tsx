"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useCurrentUserAccess } from "@/features/auth/model/CurrentUserAccessContext";
import { useAuthSession } from "@/features/auth/model/useAuthSession";
import type { CatalogProductTypeCharacteristicMetadata } from "@/features/catalogMetadata/model/types";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { CatalogRecognitionMultiIntegerDraftEvidencePanel } from "./CatalogRecognitionMultiIntegerDraftEvidencePanel";
import { CatalogRecognitionMultiIntegerDraftRecheckPanel } from "./CatalogRecognitionMultiIntegerDraftRecheckPanel";
import { AppButton } from "@/shared/ui/AppButton";
import {
  getRecognitionMultiIntegerDrafts,
  recognitionMultiIntegerDraftsQueryRoot,
} from "../../api/getRecognitionMultiIntegerDrafts";

interface CatalogRecognitionMultiIntegerDraftListProps {
  manufacturerId: string;
  productTypeId: string;
  characteristics: CatalogProductTypeCharacteristicMetadata[];
  disabled: boolean;
}

export function CatalogRecognitionMultiIntegerDraftList({
  manufacturerId,
  productTypeId,
  characteristics,
  disabled,
}: CatalogRecognitionMultiIntegerDraftListProps) {
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
      "list",
      session?.userId,
      manufacturerId,
      productTypeId,
      page,
    ],
    queryFn: () =>
      getRecognitionMultiIntegerDrafts(manufacturerId, productTypeId, page),
    enabled: canRead && !disabled,
    staleTime: 0,
    retry: false,
  });

  function characteristicName(id: string): string {
    return characteristics.find((item) => item.id === id)?.name ?? id;
  }

  function refresh(): void {
    if (disabled || query.isFetching) {
      return;
    }

    if (page !== 1) {
      setPage(1);
      return;
    }

    void query.refetch();
  }

  if (!canRead) {
    return null;
  }

  const locked = disabled || query.isFetching;

  return (
    <section className="grid gap-3 rounded-xl border border-[var(--app-border)] p-4 text-sm">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h4 className="font-semibold">Сохранённые составные шаблоны</h4>

        <AppButton
          type="button"
          variant="secondary"
          disabled={locked}
          onClick={refresh}
        >
          Обновить список
        </AppButton>
      </div>

      <p className="text-[var(--app-muted)]">
        Черновики выбранного производителя и типа товара из всех учебных
        подборок. Для просмотра не требуется размечать текущую строку. Счётчики
        относятся к моменту сохранения, а не к текущей перепроверке.
      </p>

      {disabled && (
        <p className="text-[var(--app-muted)]">Просмотр временно недоступен.</p>
      )}

      {!disabled && query.isFetching && (
        <p role="status">Загружаем составные шаблоны...</p>
      )}

      {!disabled && query.isError && (
        <p role="alert" className="text-[var(--app-danger)]">
          {getApiErrorMessage(
            query.error,
            "Не удалось загрузить составные шаблоны.",
          )}
        </p>
      )}

      {!disabled && query.isSuccess && (
        <>
          {query.data.items.length === 0 && (
            <p className="text-[var(--app-muted)]">
              На этой странице нет сохранённых составных шаблонов.
            </p>
          )}

          {query.data.items.map((draft) => (
            <article
              key={draft.id}
              className="grid gap-2 rounded-lg border border-[var(--app-border)] p-3"
            >
              <p className="font-semibold">Черновик — не активирован</p>

              <p className="whitespace-pre-wrap break-all font-mono">
                {draft.parts.map((part) => (
                  <span key={part.position}>
                    {part.characteristicDefinitionId
                      ? `{${characteristicName(part.characteristicDefinitionId)}}`
                      : part.literal}
                  </span>
                ))}
              </p>

              <p>
                Сохранён: {new Date(draft.createdAtUtc).toLocaleString("ru-RU")}
              </p>
              <p>Версия генератора: {draft.generatorVersion}</p>
              <p>
                Названий с совпадением при сохранении: {draft.matchedNameCount}
              </p>
              <p>
                Полностью поддерживающих названий: {draft.supportingNameCount}
              </p>
              <p>
                Проверено подтверждений: {draft.checkedExampleCount};
                поддерживающих: {draft.supportingExampleCount}
              </p>

              {draft.parts
                .filter((part) => part.characteristicDefinitionId !== null)
                .map((part) => (
                  <p key={part.position}>
                    {characteristicName(part.characteristicDefinitionId!)}:{" "}
                    разных значений при сохранении — {part.distinctValueCount}
                  </p>
                ))}

              <p className="break-all text-[var(--app-muted)]">
                Идентификатор: {draft.id}
              </p>
              <p className="text-[var(--app-muted)]">
                Этот черновик пока не применяется при распознавании.
              </p>

              <CatalogRecognitionMultiIntegerDraftEvidencePanel
                key={`multi-integer-evidence-${draft.id}`}
                draftId={draft.id}
                characteristics={characteristics}
                disabled={disabled}
              />

              <CatalogRecognitionMultiIntegerDraftRecheckPanel
                key={`multi-integer-recheck-${draft.id}`}
                draftId={draft.id}
                characteristics={characteristics}
                disabled={disabled}
              />
            </article>
          ))}
        </>
      )}

      {!disabled && (
        <div className="flex items-center gap-2">
          <AppButton
            type="button"
            variant="secondary"
            disabled={locked || page <= 1}
            onClick={() => setPage((current) => Math.max(1, current - 1))}
          >
            Назад
          </AppButton>

          <span>Страница {page}</span>

          <AppButton
            type="button"
            variant="secondary"
            disabled={
              locked || !query.isSuccess || !query.data.hasMore || page >= 10000
            }
            onClick={() => setPage((current) => current + 1)}
          >
            Далее
          </AppButton>
        </div>
      )}
    </section>
  );
}
