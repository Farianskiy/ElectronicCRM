"use client";

import { LearningProvenancePanel } from "./LearningProvenancePanel";
import { useAuthSession } from "@/features/auth/model/useAuthSession";
import { getTrainingExample } from "../api/trainingExamples";
import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { getCatalogManufacturers } from "@/features/catalogMetadata/api/getCatalogManufacturers";
import { getCatalogProductTypes } from "@/features/catalogMetadata/api/getCatalogProductTypes";
import { getCatalogCharacteristicDefinitions } from "@/features/catalogCharacteristicDefinitions/api/getCatalogCharacteristicDefinitions";
import { AppButton } from "@/shared/ui/AppButton";
import { AppSelect } from "@/shared/ui/AppSelect";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import {
  exportConfirmedExamples,
  getTrainingExamples,
  revokeTrainingExample,
  setTrainingExamplePurpose,
  type TrainingExampleFilter,
} from "../api/trainingExamples";

export function TrainingExamplesPanel({
  scope,
  exampleId,
  onSelectExample,
}: {
  scope?: { manufacturerId: string; productTypeId: string };
  exampleId?: string;
  onSelectExample?: (id: string) => void;
} = {}) {
  const client = useQueryClient();
  const session = useAuthSession();
  const [filters, setFilters] = useState<TrainingExampleFilter>({
    status: "active",
    page: 1,
    ...scope,
  });
  const [selectedExample, setSelectedExample] = useState<string | null>(
    exampleId ?? null,
  );
  const [revoking, setRevoking] = useState<string | null>(null);
  const [reason, setReason] = useState("");
  const [notice, setNotice] = useState("");
  const manufacturers = useQuery({
    queryKey: ["catalog-manufacturers"],
    queryFn: getCatalogManufacturers,
  });
  const types = useQuery({
    queryKey: ["catalog-product-types"],
    queryFn: getCatalogProductTypes,
  });
  const definitions = useQuery({
    queryKey: ["training-characteristic-definitions"],
    queryFn: () => getCatalogCharacteristicDefinitions(),
  });
  const examples = useQuery({
    queryKey: ["training-examples", session?.userId, filters],
    queryFn: () => getTrainingExamples(filters),
  });
  const selected = useQuery({
    queryKey: ["training-examples", session?.userId, "selected", exampleId],
    queryFn: () => getTrainingExample(exampleId!),
    enabled: Boolean(exampleId),
  });
  const revoke = useMutation({
    mutationFn: ({ id, text }: { id: string; text: string }) =>
      revokeTrainingExample(id, text),
    onSuccess: async () => {
      setRevoking(null);
      setReason("");
      setNotice(
        "Подтверждение отозвано. Действующие правила остаются активными; их изменение требует отдельной проверки и переключения.",
      );
      await client.invalidateQueries();
    },
  });
  const purpose = useMutation({
    mutationFn: ({
      id,
      evaluationOnly,
    }: {
      id: string;
      evaluationOnly: boolean;
    }) => setTrainingExamplePurpose(id, evaluationOnly),
    onSuccess: async () => {
      setNotice(
        "Назначение изменено. Ранее созданные отчёты и учебные основания требуют повторной проверки.",
      );
      await client.invalidateQueries();
    },
  });
  const download = useMutation({
    mutationFn: () => exportConfirmedExamples(filters),
  });
  const update = (key: keyof TrainingExampleFilter, value: string) =>
    setFilters((old) => ({ ...old, [key]: value || undefined, page: 1 }));
  const options = (items?: { id: string; name: string }[]) => [
    { value: "", label: "Все" },
    ...(items ?? []).map((x) => ({ value: x.id, label: x.name })),
  ];
  const error =
    selected.error ??
    purpose.error ??
    examples.error ??
    revoke.error ??
    download.error ??
    manufacturers.error ??
    types.error ??
    definitions.error;
  const visibleExamples =
    selected.data && !selected.isError
      ? [
          selected.data,
          ...(examples.data?.items ?? []).filter(
            (x) => x.id !== selected.data.id,
          ),
        ]
      : (examples.data?.items ?? []);

  return (
    <section
      id="training-examples"
      className="grid gap-4 rounded-2xl border border-[var(--app-border)] p-5"
    >
      <h2 className="text-xl font-semibold">Обучающие примеры</h2>
      <p className="text-sm text-[var(--app-muted)]">
        Ваши явные подтверждения фрагментов и значений. Отзыв исключает пример
        из новых учебных выборок и экспорта подтверждений. Проверенная обратная
        связь и действующие правила сохраняются.
      </p>
      <div className="grid gap-3 md:grid-cols-4">
        {!scope && (
          <div>
            Производитель
            <AppSelect
              ariaLabel="Производитель примеров"
              value={filters.manufacturerId ?? ""}
              options={options(manufacturers.data)}
              onChange={(v) => update("manufacturerId", v)}
            />
          </div>
        )}
        {!scope && (
          <div>
            Тип товара
            <AppSelect
              ariaLabel="Тип примеров"
              value={filters.productTypeId ?? ""}
              options={options(types.data)}
              onChange={(v) => update("productTypeId", v)}
            />
          </div>
        )}
        <div>
          Характеристика
          <AppSelect
            ariaLabel="Характеристика примеров"
            value={filters.characteristicDefinitionId ?? ""}
            options={options(definitions.data)}
            onChange={(v) => update("characteristicDefinitionId", v)}
          />
        </div>
        <div>
          Состояние
          <AppSelect
            ariaLabel="Состояние примеров"
            value={filters.status}
            options={[
              { value: "active", label: "Активные" },
              { value: "revoked", label: "Отозванные" },
              { value: "sourceExcluded", label: "Источник исключён" },
              { value: "all", label: "Все" },
            ]}
            onChange={(v) => update("status", v)}
          />
        </div>
      </div>
      <div className="grid gap-2">
        <AppButton
          variant="secondary"
          disabled={download.isPending}
          onClick={() => download.mutate()}
        >
          Экспорт подтверждённых примеров
        </AppButton>
        <p className="text-sm text-[var(--app-muted)]">
          Выгружаются ваши активные учебные примеры для выбранного производителя
          и типа товара, кроме назначенных только для контроля правил,
          независимо от страницы и фильтра состояния. Это отдельный формат
          JSONL, без bundle. Уже скачанные файлы не меняются после отзыва.
        </p>
      </div>
      {error && (
        <p role="alert" className="text-red-400">
          {getApiErrorMessage(error, "Не удалось выполнить операцию.")}
        </p>
      )}
      {notice && <p role="status">{notice}</p>}
      {examples.isPending && <p>Загрузка примеров…</p>}
      {examples.data?.items.length === 0 && (
        <p>Примеров по выбранным фильтрам нет.</p>
      )}
      {selectedExample && (
        <LearningProvenancePanel
          key={selectedExample}
          exampleId={selectedExample}
        />
      )}
      {visibleExamples.map((x) => (
        <article
          key={x.id}
          className="grid gap-2 rounded-xl border border-[var(--app-border)] p-4"
        >
          <h3 className="font-medium">{x.productName}</h3>
          <p>
            {x.manufacturerName} · {x.productTypeName} · {x.characteristicName}
          </p>
          <p>
            Назначение:{" "}
            {x.isEvaluationOnly ? "только контроль правил" : "обучение правил"}.
          </p>
          {!x.revokedAtUtc && !x.sourceExcludedAtUtc && x.sourceAvailable && (
            <AppButton
              variant="secondary"
              disabled={purpose.isPending}
              onClick={() =>
                purpose.mutate({
                  id: x.id,
                  evaluationOnly: !x.isEvaluationOnly,
                })
              }
            >
              {x.isEvaluationOnly
                ? "Вернуть в обучение правил"
                : "Использовать только для контроля правил"}
            </AppButton>
          )}
          <p>
            Фрагмент: <strong>{x.rawValue}</strong> → {x.normalizedValue}
          </p>
          <p className="text-sm">
            Подтверждён: {new Date(x.confirmedAtUtc).toLocaleString("ru-RU")} ·{" "}
            {x.revokedAtUtc
              ? "Отозван"
              : !x.sourceAvailable
                ? "Источник недоступен"
                : x.sourceExcludedAtUtc
                  ? "Источник исключён"
                  : "Активен"}
          </p>
          {x.sourceExcludedAtUtc && (
            <p>Источник исключён: {x.sourceExclusionReason}</p>
          )}
          <AppButton
            variant="secondary"
            onClick={() => {
              setSelectedExample(x.id);
              onSelectExample?.(x.id);
            }}
          >
            Использование в обучении
          </AppButton>
          {!x.importBatchId && (
            <p className="text-sm text-[var(--app-muted)]">
              Исходный импорт удалён
            </p>
          )}
          {x.revokedAtUtc && (
            <p>
              Отозван: {new Date(x.revokedAtUtc).toLocaleString("ru-RU")}.
              Причина:{" "}
              {x.revocationReason ?? "Не указана в исторической записи"}
            </p>
          )}
          {!x.revokedAtUtc && revoking !== x.id && (
            <AppButton
              variant="secondary"
              onClick={() => {
                setRevoking(x.id);
                setReason("");
                revoke.reset();
              }}
            >
              Отозвать подтверждение примера
            </AppButton>
          )}
          {revoking === x.id && (
            <div className="grid gap-2">
              <label>
                Причина отзыва
                <textarea
                  className="block w-full rounded border border-[var(--app-border)] bg-transparent p-2"
                  maxLength={1000}
                  value={reason}
                  onChange={(e) => setReason(e.target.value)}
                />
              </label>
              <AppButton
                disabled={!reason.trim() || revoke.isPending}
                onClick={() => revoke.mutate({ id: x.id, text: reason.trim() })}
              >
                Подтвердить отзыв
              </AppButton>
              <AppButton
                variant="secondary"
                disabled={revoke.isPending}
                onClick={() => setRevoking(null)}
              >
                Отмена
              </AppButton>
            </div>
          )}
        </article>
      ))}
      <div className="flex items-center gap-3">
        <AppButton
          variant="secondary"
          disabled={filters.page <= 1 || examples.isFetching}
          onClick={() => setFilters((old) => ({ ...old, page: old.page - 1 }))}
        >
          Назад
        </AppButton>
        <span>Страница {filters.page}</span>
        <AppButton
          variant="secondary"
          disabled={!examples.data?.hasMore || examples.isFetching}
          onClick={() => setFilters((old) => ({ ...old, page: old.page + 1 }))}
        >
          Далее
        </AppButton>
      </div>
    </section>
  );
}
