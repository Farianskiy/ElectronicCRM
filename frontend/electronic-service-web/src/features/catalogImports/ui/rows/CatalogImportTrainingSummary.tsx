"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { useCurrentUserAccess } from "@/features/auth/model/CurrentUserAccessContext";
import { useAuthSession } from "@/features/auth/model/useAuthSession";
import { getCatalogProductTypeCharacteristics } from "@/features/catalogMetadata/api/getCatalogProductTypeCharacteristics";
import { getCatalogProductTypes } from "@/features/catalogMetadata/api/getCatalogProductTypes";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppButton } from "@/shared/ui/AppButton";
import { analyzeCatalogImportBatch } from "../../api/analyzeCatalogImportBatch";
import {
  catalogImportTrainingSummaryQueryKey,
  getCatalogImportTrainingSummary,
  type CatalogImportTrainingSummaryGroup,
} from "../../api/getCatalogImportTrainingSummary";
import {
  getRecognitionRuleSetVersions,
  recognitionRuleSetVersionsQueryRoot,
} from "../../api/getRecognitionRuleSetVersions";
import { CatalogRecognitionLiteralProposalPreview } from "./CatalogRecognitionLiteralProposalPreview";
import { CatalogRecognitionRuleSetActivation } from "./CatalogRecognitionRuleSetActivation";
import { CatalogRecognitionRuleSetVersionCreate } from "./CatalogRecognitionRuleSetVersionCreate";
import type { AnalyzeCatalogImportBatchResponse } from "../../model/types";
import { catalogImportQueryKeys } from "../../model/queryKeys";

interface CatalogImportTrainingSummaryProps {
  batchId: string;
  canReanalyze: boolean;
  onAnalysisChange: (
    analysis: AnalyzeCatalogImportBatchResponse | null,
  ) => void;
}

function getGroupKey(group: CatalogImportTrainingSummaryGroup): string {
  return `${group.manufacturerId}:${group.productTypeId}:${group.characteristicDefinitionId}`;
}

function getReadinessText(distinctValuesCount: number): string {
  if (distinctValuesCount >= 2) {
    return "Есть разные значения — можно проверить предложение правила";
  }

  return "Покажите ещё хотя бы одно другое значение этой характеристики";
}

export function CatalogImportTrainingSummary({
  batchId,
  canReanalyze,
  onAnalysisChange,
}: CatalogImportTrainingSummaryProps) {
  const [openedGroupKey, setOpenedGroupKey] = useState<string | null>(null);
  const [versionComposerGroupKey, setVersionComposerGroupKey] = useState<
    string | null
  >(null);
  const access = useCurrentUserAccess();
  const canManageTraining =
    !access.isLoading &&
    !access.isError &&
    access.hasPermission("DictionariesManage");

  const summaryQuery = useQuery({
    queryKey: catalogImportTrainingSummaryQueryKey(batchId),
    queryFn: () => getCatalogImportTrainingSummary(batchId),
    enabled: batchId.length > 0 && canManageTraining,
  });

  if (!canManageTraining) {
    return null;
  }

  return (
    <section
      aria-labelledby="catalog-import-training-summary-title"
      className="mt-4 rounded-2xl border border-[var(--app-accent-border)] bg-[var(--app-accent-soft)] p-4"
    >
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h3
            id="catalog-import-training-summary-title"
            className="text-sm font-semibold text-[var(--app-text)]"
          >
            Обучение на этом Excel
          </h3>

          <p className="mt-1 max-w-4xl text-xs leading-5 text-[var(--app-muted)]">
            В редакторе строки укажите правильное значение характеристики,
            выделите его в наименовании и нажмите «Сохранить и подтвердить для
            обучения». CRM сгруппирует подтверждения и покажет, когда накопились
            разные примеры. Ничего не включается автоматически.
          </p>
        </div>

        {summaryQuery.isFetching && !summaryQuery.isLoading && (
          <span role="status" className="text-xs text-[var(--app-accent)]">
            Обновляем…
          </span>
        )}
      </div>

      {summaryQuery.isLoading && (
        <p role="status" className="mt-4 text-sm text-[var(--app-muted)]">
          Считаем подтверждённые примеры…
        </p>
      )}

      {summaryQuery.isError && (
        <p role="alert" className="mt-4 text-sm text-[var(--app-danger)]">
          {getApiErrorMessage(
            summaryQuery.error,
            "Не удалось загрузить сводку обучения.",
          )}
        </p>
      )}

      {summaryQuery.data && (
        <>
          <dl className="mt-4 grid gap-3 sm:grid-cols-3">
            <div className="rounded-xl border border-[var(--app-border)] bg-[var(--app-panel)] p-3">
              <dt className="text-xs text-[var(--app-muted)]">
                Подтверждено для обучения
              </dt>
              <dd className="mt-1 text-xl font-semibold tabular-nums">
                {summaryQuery.data.activeExamplesCount.toLocaleString("ru-RU")}
              </dd>
            </div>

            <div className="rounded-xl border border-[var(--app-border)] bg-[var(--app-panel)] p-3">
              <dt className="text-xs text-[var(--app-muted)]">
                Групп характеристик
              </dt>
              <dd className="mt-1 text-xl font-semibold tabular-nums">
                {summaryQuery.data.groups.length.toLocaleString("ru-RU")}
              </dd>
            </div>

            <div className="rounded-xl border border-[var(--app-border)] bg-[var(--app-panel)] p-3">
              <dt className="text-xs text-[var(--app-muted)]">
                Готовы к проверке предложения
              </dt>
              <dd className="mt-1 text-xl font-semibold tabular-nums">
                {summaryQuery.data.groups
                  .filter((group) => group.distinctValuesCount >= 2)
                  .length.toLocaleString("ru-RU")}
              </dd>
            </div>
          </dl>

          {summaryQuery.data.groups.length === 0 ? (
            <div className="mt-4 rounded-xl border border-dashed border-[var(--app-border)] bg-[var(--app-panel)] p-4 text-sm">
              <p className="font-medium">Учебных примеров пока нет.</p>
              <p className="mt-1 text-[var(--app-muted)]">
                Откройте нужную строку кнопкой «Редактировать», заполните
                характеристику, выделите соответствующий фрагмент наименования и
                подтвердите пример для обучения.
              </p>
            </div>
          ) : (
            <div className="mt-4 grid gap-3">
              {summaryQuery.data.groups.map((group) => {
                const canPrepareProposal = group.distinctValuesCount >= 2;
                const groupKey = getGroupKey(group);
                const isOpened = openedGroupKey === groupKey;

                return (
                  <article
                    key={groupKey}
                    className="rounded-xl border border-[var(--app-border)] bg-[var(--app-panel)] p-4"
                  >
                    <div className="flex flex-wrap items-start justify-between gap-3">
                      <div>
                        <p className="font-semibold text-[var(--app-text)]">
                          {group.characteristicName}
                        </p>
                        <p className="mt-1 text-xs text-[var(--app-muted)]">
                          {group.manufacturerName} · {group.productTypeName}
                        </p>
                      </div>

                      <span
                        className={[
                          "rounded-full border px-2.5 py-1 text-xs font-medium",
                          canPrepareProposal
                            ? "border-[var(--app-success-border)] bg-[var(--app-success-soft)] text-[var(--app-success)]"
                            : "border-[var(--app-warning-border)] bg-[var(--app-warning-soft)] text-[var(--app-warning)]",
                        ].join(" ")}
                      >
                        {canPrepareProposal ? "Можно проверять" : "Нужны примеры"}
                      </span>
                    </div>

                    <div className="mt-3 flex flex-wrap items-center gap-x-4 gap-y-2 text-xs text-[var(--app-muted)]">
                      <span>
                        Подтверждений: {group.activeExamplesCount.toLocaleString("ru-RU")}
                      </span>
                      <span>
                        Разных значений: {group.distinctValuesCount.toLocaleString("ru-RU")}
                      </span>
                    </div>

                    <p className="mt-2 text-sm text-[var(--app-muted)]">
                      {getReadinessText(group.distinctValuesCount)}.
                    </p>

                    {canPrepareProposal && (
                      <AppButton
                        type="button"
                        variant={isOpened ? "secondary" : "primary"}
                        className="mt-3"
                        onClick={() => {
                          setOpenedGroupKey(isOpened ? null : groupKey);

                          if (isOpened) {
                            setVersionComposerGroupKey(null);
                          }
                        }}
                      >
                        {isOpened
                          ? "Закрыть предложения"
                          : "Проверить и подготовить правило"}
                      </AppButton>
                    )}

                    {isOpened && (
                      <div className="mt-4 border-t border-[var(--app-border)] pt-4">
                        <CatalogRecognitionLiteralProposalPreview
                          batchId={batchId}
                          manufacturerId={group.manufacturerId}
                          productTypeId={group.productTypeId}
                          characteristicDefinitionId={
                            group.characteristicDefinitionId
                          }
                          characteristicName={group.characteristicName}
                          disabled={false}
                        />

                        <section className="mt-4 grid gap-3 rounded-xl border border-[var(--app-role-border)] bg-[var(--app-role-soft)] p-4">
                          <div>
                            <h4 className="font-semibold text-[var(--app-text)]">
                              Собрать версию правил
                            </h4>
                            <p className="mt-1 text-sm text-[var(--app-muted)]">
                              Когда нужные предложения сохранены как черновики,
                              объедините их в неактивную версию. Она ещё не
                              влияет на импорт и должна пройти отдельную оценку.
                            </p>
                          </div>

                          <AppButton
                            type="button"
                            variant="secondary"
                            onClick={() =>
                              setVersionComposerGroupKey(
                                versionComposerGroupKey === groupKey
                                  ? null
                                  : groupKey,
                              )
                            }
                          >
                            {versionComposerGroupKey === groupKey
                              ? "Закрыть сборку версии"
                              : "Выбрать черновики и создать версию"}
                          </AppButton>

                          {versionComposerGroupKey === groupKey && (
                            <CatalogImportRuleSetVersionComposer
                              batchId={batchId}
                              group={group}
                              canReanalyze={canReanalyze}
                              onAnalysisChange={onAnalysisChange}
                            />
                          )}
                        </section>
                      </div>
                    )}
                  </article>
                );
              })}
            </div>
          )}

          {(summaryQuery.data.evaluationExamplesCount > 0 ||
            summaryQuery.data.revokedExamplesCount > 0) && (
            <p className="mt-3 text-xs text-[var(--app-muted)]">
              Не входят в обучение: контрольных примеров — {summaryQuery.data.evaluationExamplesCount.toLocaleString("ru-RU")},
              отозванных — {summaryQuery.data.revokedExamplesCount.toLocaleString("ru-RU")}.
            </p>
          )}
        </>
      )}
    </section>
  );
}

function CatalogImportRuleSetVersionComposer({
  batchId,
  group,
  canReanalyze,
  onAnalysisChange,
}: {
  batchId: string;
  group: CatalogImportTrainingSummaryGroup;
  canReanalyze: boolean;
  onAnalysisChange: (
    analysis: AnalyzeCatalogImportBatchResponse | null,
  ) => void;
}) {
  const session = useAuthSession();
  const [activeVersionId, setActiveVersionId] = useState<string | null>(null);
  const [versionToEvaluateId, setVersionToEvaluateId] = useState<string | null>(
    null,
  );
  const productTypesQuery = useQuery({
    queryKey: ["catalog-product-types"],
    queryFn: getCatalogProductTypes,
    staleTime: 5 * 60 * 1000,
  });

  const productType = productTypesQuery.data?.find(
    (item) => item.id === group.productTypeId,
  );

  const characteristicsQuery = useQuery({
    queryKey: ["catalog-product-type-characteristics", productType?.code],
    queryFn: () => getCatalogProductTypeCharacteristics(productType!.code),
    enabled: Boolean(productType?.code),
    staleTime: 5 * 60 * 1000,
  });

  const versionsQuery = useQuery({
    queryKey: [
      ...recognitionRuleSetVersionsQueryRoot,
      "import",
      session?.userId,
      group.manufacturerId,
      group.productTypeId,
      1,
    ],
    queryFn: ({ signal }) =>
      getRecognitionRuleSetVersions(
        group.manufacturerId,
        group.productTypeId,
        1,
        signal,
      ),
    enabled: Boolean(session?.userId),
    staleTime: 0,
  });

  if (productTypesQuery.isPending) {
    return (
      <p role="status" className="text-sm text-[var(--app-muted)]">
        Загружаем тип товара…
      </p>
    );
  }

  if (productTypesQuery.isError) {
    return (
      <p role="alert" className="text-sm text-[var(--app-danger)]">
        {getApiErrorMessage(
          productTypesQuery.error,
          "Не удалось подготовить сборку версии.",
        )}
      </p>
    );
  }

  if (!productType) {
    return (
      <p role="alert" className="text-sm text-[var(--app-danger)]">
        Тип товара больше недоступен.
      </p>
    );
  }

  if (characteristicsQuery.isPending) {
    return (
      <p role="status" className="text-sm text-[var(--app-muted)]">
        Загружаем доступные характеристики и черновики…
      </p>
    );
  }

  if (characteristicsQuery.isError) {
    return (
      <p role="alert" className="text-sm text-[var(--app-danger)]">
        {getApiErrorMessage(
          characteristicsQuery.error,
          "Не удалось загрузить характеристики типа товара.",
        )}
      </p>
    );
  }

  if (!characteristicsQuery.data) {
    return null;
  }

  return (
    <>
      <CatalogRecognitionRuleSetVersionCreate
        manufacturerId={group.manufacturerId}
        productTypeId={group.productTypeId}
        characteristics={characteristicsQuery.data}
        disabled={false}
        initialKind={1}
        initialCharacteristicId={group.characteristicDefinitionId}
        suggestedName={`Правила ${group.manufacturerName} — ${group.productTypeName}`}
        onCreated={(version) => setVersionToEvaluateId(version.id)}
      />

      <section className="mt-4 grid gap-3 rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <h4 className="font-semibold text-[var(--app-text)]">
              Версии, доступные для проверки
            </h4>
            <p className="mt-1 text-sm text-[var(--app-muted)]">
              Здесь можно продолжить проверку ранее созданной версии после
              обновления страницы или повторного открытия импорта.
            </p>
          </div>

          <AppButton
            type="button"
            variant="secondary"
            disabled={versionsQuery.isFetching}
            onClick={() => void versionsQuery.refetch()}
          >
            Обновить версии
          </AppButton>
        </div>

        {versionsQuery.isPending && (
          <p role="status" className="text-sm text-[var(--app-muted)]">
            Загружаем версии…
          </p>
        )}

        {versionsQuery.isError && (
          <p role="alert" className="text-sm text-[var(--app-danger)]">
            {getApiErrorMessage(
              versionsQuery.error,
              "Не удалось загрузить версии правил.",
            )}
          </p>
        )}

        {versionsQuery.data?.items.length === 0 && (
          <p className="text-sm text-[var(--app-muted)]">
            Сохранённых версий пока нет.
          </p>
        )}

        {versionsQuery.data?.items.map((version) => (
          <article
            key={version.id}
            className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-[var(--app-border)] bg-[var(--app-panel)] p-3"
          >
            <div>
              <p className="font-medium text-[var(--app-text)]">
                Версия {version.versionNumber}: {version.name}
              </p>
              <p className="mt-1 text-xs text-[var(--app-muted)]">
                Черновиков: {version.entryCount.toLocaleString("ru-RU")} ·{" "}
                {new Date(version.createdAtUtc).toLocaleString("ru-RU")}
              </p>
            </div>

            <AppButton
              type="button"
              variant={
                versionToEvaluateId === version.id ? "primary" : "secondary"
              }
              onClick={() => setVersionToEvaluateId(version.id)}
            >
              {versionToEvaluateId === version.id
                ? "Выбрана для проверки"
                : "Проверить на этом Excel"}
            </AppButton>
          </article>
        ))}
      </section>

      {versionToEvaluateId && session?.userId && (
        <section className="mt-4 grid gap-3 rounded-xl border border-[var(--app-accent-border)] bg-[var(--app-accent-soft)] p-4">
          <div>
            <h4 className="font-semibold text-[var(--app-text)]">
              Проверить созданную версию на этом Excel
            </h4>
            <p className="mt-1 text-sm text-[var(--app-muted)]">
              Сначала создайте сравнительный отчёт и просмотрите предложенные
              изменения. Сама проверка не меняет строки импорта и не включает
              правила.
            </p>
          </div>

          <CatalogRecognitionRuleSetActivation
            userId={session.userId}
            batchId={batchId}
            manufacturerId={group.manufacturerId}
            productTypeId={group.productTypeId}
            versionId={versionToEvaluateId}
            disabled={false}
            onActiveChange={(isActive) =>
              setActiveVersionId(isActive ? versionToEvaluateId : null)
            }
          />

          {activeVersionId === versionToEvaluateId && (
            <CatalogImportReanalysis
              batchId={batchId}
              canReanalyze={canReanalyze}
              onAnalysisChange={onAnalysisChange}
            />
          )}
        </section>
      )}
    </>
  );
}

function CatalogImportReanalysis({
  batchId,
  canReanalyze,
  onAnalysisChange,
}: {
  batchId: string;
  canReanalyze: boolean;
  onAnalysisChange: (
    analysis: AnalyzeCatalogImportBatchResponse | null,
  ) => void;
}) {
  const queryClient = useQueryClient();
  const [result, setResult] = useState<AnalyzeCatalogImportBatchResponse | null>(
    null,
  );

  const mutation = useMutation({
    mutationFn: () => analyzeCatalogImportBatch(batchId),
    onMutate: () => {
      setResult(null);
      onAnalysisChange(null);
    },
    onSuccess: async (analysis) => {
      setResult(analysis);
      onAnalysisChange(analysis);

      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: catalogImportQueryKeys.details(batchId),
        }),
        queryClient.invalidateQueries({
          queryKey: catalogImportQueryKeys.mapping(batchId),
        }),
        queryClient.invalidateQueries({
          queryKey: catalogImportQueryKeys.rowsRoot(batchId),
        }),
        queryClient.invalidateQueries({
          queryKey: catalogImportQueryKeys.rowProblemCodesRoot(batchId),
        }),
        queryClient.invalidateQueries({
          queryKey: [
            ...catalogImportQueryKeys.root,
            "manufacturer-groups",
            batchId,
          ],
        }),
        queryClient.invalidateQueries({
          queryKey: catalogImportQueryKeys.nameExplanations(batchId),
        }),
        queryClient.invalidateQueries({
          queryKey: catalogImportQueryKeys.history(batchId),
        }),
        queryClient.invalidateQueries({
          queryKey: catalogImportQueryKeys.myRoot,
        }),
      ]);
    },
  });

  return (
    <section className="grid gap-3 rounded-xl border border-[var(--app-success-border)] bg-[var(--app-success-soft)] p-4">
      <div>
        <h5 className="font-semibold text-[var(--app-text)]">
          Применить включённые правила к этому Excel
        </h5>
        <p className="mt-1 text-sm text-[var(--app-muted)]">
          Версия активна. Запустите повторный анализ — CRM заново прочитает
          исходный файл, применит действующие правила и обновит строки этого
          пакета. Сохранённые ручные исправления останутся без изменений.
        </p>
      </div>

      <AppButton
        type="button"
        disabled={!canReanalyze || mutation.isPending}
        onClick={() => mutation.mutate()}
      >
        {mutation.isPending
          ? "Повторно анализируем Excel…"
          : "Повторно проанализировать этот Excel"}
      </AppButton>

      {!canReanalyze && (
        <p className="text-sm text-[var(--app-warning)]">
          Пакет уже передан дальше по процессу и сейчас недоступен для
          повторного анализа. Создайте новый импорт этого файла либо верните
          пакет на редактирование.
        </p>
      )}

      {mutation.isError && (
        <p role="alert" className="text-sm text-[var(--app-danger)]">
          {getApiErrorMessage(
            mutation.error,
            "Не удалось повторно проанализировать Excel.",
          )}
        </p>
      )}

      {result && (
        <div
          role="status"
          className="rounded-lg border border-[var(--app-success-border)] bg-[var(--app-panel)] p-3 text-sm"
        >
          <p className="font-medium text-[var(--app-success)]">
            Повторный анализ завершён.
          </p>
          <p className="mt-1 text-[var(--app-muted)]">
            Всего строк: {result.rowsCount.toLocaleString("ru-RU")}; без
            ошибок: {result.validRowsCount.toLocaleString("ru-RU")}; с
            ошибками: {result.errorRowsCount.toLocaleString("ru-RU")}.
          </p>
          <p className="mt-1 text-[var(--app-muted)]">
            Правила заполнили значений: {result.recognitionEnrichment.filledValuesCount.toLocaleString("ru-RU")}.
          </p>
        </div>
      )}
    </section>
  );
}
