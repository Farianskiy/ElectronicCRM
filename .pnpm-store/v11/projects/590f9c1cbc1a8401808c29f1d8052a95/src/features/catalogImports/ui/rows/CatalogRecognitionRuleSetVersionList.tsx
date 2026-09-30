"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useCurrentUserAccess } from "@/features/auth/model/CurrentUserAccessContext";
import { useAuthSession } from "@/features/auth/model/useAuthSession";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppButton } from "@/shared/ui/AppButton";
import { CatalogRecognitionRuleSetNamePreview } from "./CatalogRecognitionRuleSetNamePreview";
import { CatalogRecognitionRuleSetBatchPreview } from "./CatalogRecognitionRuleSetBatchPreview";
import { CatalogRecognitionRuleSetActivation } from "./CatalogRecognitionRuleSetActivation";
import {
  getRecognitionRuleSetVersion,
  getRecognitionRuleSetVersions,
  recognitionRuleSetVersionsQueryRoot,
} from "../../api/getRecognitionRuleSetVersions";

interface CatalogRecognitionRuleSetVersionListProps {
  batchId: string;
  manufacturerId: string;
  productTypeId: string;
  disabled: boolean;
}

function ruleKindName(kind: number): string {
  switch (kind) {
    case 1:
      return "Точное соответствие";
    case 2:
      return "Одиночный числовой шаблон";
    case 3:
      return "Составной числовой шаблон";
    default:
      return `Неизвестный вид шаблона (${kind})`;
  }
}

function VersionEntries({
  versionId,
  userId,
}: {
  versionId: string;
  userId: string;
}) {
  const query = useQuery({
    queryKey: [
      ...recognitionRuleSetVersionsQueryRoot,
      "details",
      userId,
      versionId,
    ],
    queryFn: ({ signal }) => getRecognitionRuleSetVersion(versionId, signal),
    staleTime: 0,
    retry: false,
  });

  return (
    <div className="grid gap-2 rounded-lg border border-[var(--app-border)] p-3">
      {query.isFetching && <p role="status">Загружаем состав версии...</p>}

      {query.isError && (
        <div className="grid gap-2">
          <p role="alert" className="text-[var(--app-danger)]">
            {getApiErrorMessage(
              query.error,
              "Не удалось загрузить состав версии.",
            )}
          </p>
          <AppButton
            type="button"
            variant="secondary"
            disabled={query.isFetching}
            onClick={() => void query.refetch()}
          >
            Повторить загрузку
          </AppButton>
        </div>
      )}

      {query.isSuccess && (
        <>
          {query.data.entries.length === 0 && (
            <p className="text-[var(--app-danger)]">
              Состав версии пуст. Требуется проверка сохранённых данных.
            </p>
          )}

          {query.data.entries.map((entry) => (
            <div
              key={entry.position}
              className="grid gap-1 rounded-lg border border-[var(--app-border)] p-2"
            >
              <p className="font-medium">{ruleKindName(entry.kind)}</p>
              <p className="break-all text-[var(--app-muted)]">
                Черновик: {entry.draftId}
              </p>
            </div>
          ))}

          <p className="text-[var(--app-muted)]">
            Порядок элементов не задаёт приоритет распознавания. Здесь показаны
            ссылки на сохранённые черновики, а не результат проверки набора.
          </p>
        </>
      )}
    </div>
  );
}

export function CatalogRecognitionRuleSetVersionList({
  batchId,
  manufacturerId,
  productTypeId,
  disabled,
}: CatalogRecognitionRuleSetVersionListProps) {
  const [page, setPage] = useState(1);
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const access = useCurrentUserAccess();
  const session = useAuthSession();
  const userId = session?.userId;

  const canRead =
    Boolean(userId) &&
    !access.isLoading &&
    !access.isError &&
    access.hasPermission("DictionariesManage");

  const query = useQuery({
    queryKey: [
      ...recognitionRuleSetVersionsQueryRoot,
      "list",
      userId,
      manufacturerId,
      productTypeId,
      page,
    ],
    queryFn: ({ signal }) =>
      getRecognitionRuleSetVersions(
        manufacturerId,
        productTypeId,
        page,
        signal,
      ),
    enabled: canRead && !disabled,
    staleTime: 0,
    retry: false,
  });

  function refresh(): void {
    if (disabled || query.isFetching) {
      return;
    }

    setExpandedId(null);

    if (page !== 1) {
      setPage(1);
      return;
    }

    void query.refetch();
  }

  function changePage(nextPage: number): void {
    setExpandedId(null);
    setPage(nextPage);
  }

  if (!canRead || !userId) {
    return null;
  }

  const locked = disabled || query.isFetching;

  return (
    <section className="grid gap-3 rounded-xl border border-[var(--app-border)] p-4 text-sm">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h4 className="font-semibold">Версии наборов правил</h4>
        <AppButton
          type="button"
          variant="secondary"
          disabled={locked}
          onClick={refresh}
        >
          Обновить версии
        </AppButton>
      </div>

      <p className="text-[var(--app-muted)]">
        Сохранённые наборы выбранного производителя и типа товара. Создание
        версии не активирует её. После проверки и отдельной активации правила
        используются в следующих запусках анализа импорта.
      </p>

      {disabled && (
        <p className="text-[var(--app-muted)]">Просмотр временно недоступен.</p>
      )}

      {!disabled && query.isFetching && (
        <p role="status">Загружаем версии...</p>
      )}

      {!disabled && query.isError && (
        <p role="alert" className="text-[var(--app-danger)]">
          {getApiErrorMessage(query.error, "Не удалось загрузить версии.")}
        </p>
      )}

      {!disabled && query.isSuccess && (
        <>
          {query.data.items.length === 0 && (
            <p className="text-[var(--app-muted)]">
              На этой странице нет сохранённых версий.
            </p>
          )}

          {query.data.items.map((version) => (
            <article
              key={version.id}
              className="grid gap-2 rounded-lg border border-[var(--app-border)] p-3"
            >
              <p className="font-semibold">
                Версия {version.versionNumber}: {version.name}
              </p>
              <p>Шаблонов в составе: {version.entryCount}</p>
              <p>
                Создана:{" "}
                {new Date(version.createdAtUtc).toLocaleString("ru-RU")}
              </p>
              <p className="break-all text-[var(--app-muted)]">
                Идентификатор: {version.id}
              </p>

              <AppButton
                type="button"
                variant="secondary"
                onClick={() =>
                  setExpandedId((current) =>
                    current === version.id ? null : version.id,
                  )
                }
              >
                {expandedId === version.id
                  ? "Скрыть состав"
                  : "Показать состав"}
              </AppButton>

              {expandedId === version.id && (
                <VersionEntries
                  key={`${userId}-${version.id}`}
                  versionId={version.id}
                  userId={userId}
                />
              )}

              <CatalogRecognitionRuleSetNamePreview
                key={JSON.stringify([
                  "name-preview",
                  userId,
                  manufacturerId,
                  productTypeId,
                  version.id,
                ])}
                versionId={version.id}
                manufacturerId={manufacturerId}
                productTypeId={productTypeId}
                disabled={locked}
              />

              <CatalogRecognitionRuleSetBatchPreview
                key={JSON.stringify([
                  "version-batch-preview",
                  userId,
                  batchId,
                  manufacturerId,
                  productTypeId,
                  version.id,
                ])}
                versionId={version.id}
                batchId={batchId}
                disabled={locked}
              />

              <CatalogRecognitionRuleSetActivation
                key={JSON.stringify([
                  "activation",
                  userId,
                  batchId,
                  manufacturerId,
                  productTypeId,
                  version.id,
                ])}
                userId={userId}
                batchId={batchId}
                manufacturerId={manufacturerId}
                productTypeId={productTypeId}
                versionId={version.id}
                disabled={locked}
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
            onClick={() => changePage(page - 1)}
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
            onClick={() => changePage(page + 1)}
          >
            Далее
          </AppButton>
        </div>
      )}
    </section>
  );
}
