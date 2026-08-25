"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState, type FormEvent } from "react";
import { useAuthSession } from "@/features/auth/model/useAuthSession";
import { createApprovedManufacturerAlias } from "@/features/catalogManufacturers/api/createApprovedManufacturerAlias";
import type { CreateApprovedManufacturerAliasResponse } from "@/features/catalogManufacturers/model/types";
import { getCatalogManufacturers } from "@/features/catalogMetadata/api/getCatalogManufacturers";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { isTechnicalUser } from "@/shared/api/authToken";
import { AppSelect } from "@/shared/ui/AppSelect";
import { analyzeCatalogImportBatch } from "../../api/analyzeCatalogImportBatch";
import { catalogImportQueryKeys } from "../../model/queryKeys";
import type {
  AnalyzeCatalogImportBatchResponse,
  CatalogImportManufacturerResolutionGroup,
} from "../../model/types";

interface CatalogImportManufacturerAliasCreationFormProps {
  batchId: string;
  productTypeId?: string | null;
  group: CatalogImportManufacturerResolutionGroup;
  onAnalysisChange: (analysis: AnalyzeCatalogImportBatchResponse) => void;
}

export function CatalogImportManufacturerAliasCreationForm({
  batchId,
  productTypeId,
  group,
  onAnalysisChange,
}: CatalogImportManufacturerAliasCreationFormProps) {
  const queryClient = useQueryClient();
  const session = useAuthSession();
  const canManageManufacturers = isTechnicalUser(session);

  const [selectedManufacturerId, setSelectedManufacturerId] = useState("");
  const [changeConfirmed, setChangeConfirmed] = useState(false);
  const [createdAlias, setCreatedAlias] =
    useState<CreateApprovedManufacturerAliasResponse | null>(null);

  const manufacturersQuery = useQuery({
    queryKey: ["catalog-manufacturers"],
    queryFn: getCatalogManufacturers,
    enabled: canManageManufacturers,
    staleTime: 5 * 60 * 1000,
  });

  const manufacturers = manufacturersQuery.data ?? [];

  const selectedManufacturer =
    manufacturers.find(
      (manufacturer) => manufacturer.id === selectedManufacturerId,
    ) ?? null;

  const repeatAnalysisMutation = useMutation({
    mutationFn: () => analyzeCatalogImportBatch(batchId, productTypeId),

    onSuccess: async (analysis) => {
      onAnalysisChange(analysis);

      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: catalogImportQueryKeys.mapping(batchId),
        }),
        queryClient.invalidateQueries({
          queryKey: catalogImportQueryKeys.details(batchId),
        }),
        queryClient.invalidateQueries({
          queryKey: catalogImportQueryKeys.rowsRoot(batchId),
        }),
        queryClient.invalidateQueries({
          queryKey: catalogImportQueryKeys.myRoot,
        }),
      ]);
    },
  });

  const createAliasMutation = useMutation({
    mutationFn: () =>
      createApprovedManufacturerAlias({
        manufacturerId: selectedManufacturerId,
        phrase: group.sourceValue,
      }),

    onSuccess: (alias) => {
      setCreatedAlias(alias);
      setChangeConfirmed(false);
      repeatAnalysisMutation.mutate();
    },
  });

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    if (!selectedManufacturerId || !changeConfirmed) {
      return;
    }

    createAliasMutation.reset();
    repeatAnalysisMutation.reset();
    setCreatedAlias(null);
    createAliasMutation.mutate();
  }

  if (!canManageManufacturers) {
    return (
      <div className="mt-4 rounded-xl border border-slate-500/25 bg-slate-500/[0.06] p-4">
        <p className="text-sm font-medium text-slate-200">
          Требуется решение Technical-пользователя
        </p>

        <p className="mt-2 text-sm leading-6 text-slate-400">
          Вы можете видеть неизвестное значение, но изменять справочник
          Manufacturer и создавать ManufacturerAlias может только
          Technical-пользователь.
        </p>
      </div>
    );
  }

  return (
    <form
      onSubmit={handleSubmit}
      className="mt-4 rounded-2xl border border-amber-500/30 bg-amber-500/[0.06] p-5"
    >
      <div>
        <h4 className="font-semibold text-amber-100">
          Привязать к существующему производителю
        </h4>

        <p className="mt-2 text-sm leading-6 text-amber-200/80">
          Выберите канонического производителя, которому соответствует исходная
          фраза из Excel. После подтверждения будет создан постоянный Approved
          ManufacturerAlias.
        </p>
      </div>

      <div className="mt-4 rounded-xl border border-red-500/30 bg-red-500/[0.08] p-4">
        <p className="text-sm font-medium text-red-100">
          Это реальное изменение справочника
        </p>

        <p className="mt-2 text-sm leading-6 text-red-200/80">
          Созданный alias будет использоваться не только в текущем пакете. Все
          последующие импорты смогут автоматически интерпретировать фразу «
          {group.sourceValue}» как выбранного производителя.
        </p>
      </div>

      <dl className="mt-4 grid gap-4 lg:grid-cols-2">
        <div className="rounded-xl border border-white/10 bg-black/20 p-4">
          <dt className="text-xs text-slate-500">
            Фраза, которая станет alias
          </dt>

          <dd className="mt-2 break-words font-mono text-sm text-white">
            {group.sourceValue}
          </dd>

          <dd className="mt-2 break-words font-mono text-xs text-slate-500">
            Нормализовано: {group.normalizedSourceValue}
          </dd>
        </div>

        <div className="rounded-xl border border-white/10 bg-black/20 p-4">
          <dt className="text-xs text-slate-500">
            Сколько строк исправит это решение
          </dt>

          <dd className="mt-2 text-2xl font-semibold text-white">
            {group.occurrenceCount}
          </dd>

          <dd className="mt-2 text-xs text-slate-500">
            После повторного анализа текущего Excel-файла.
          </dd>
        </div>
      </dl>

      {manufacturersQuery.isLoading && (
        <div className="mt-4 rounded-xl border border-sky-500/25 bg-sky-500/[0.06] p-4 text-sm text-sky-200">
          Загружаем существующих производителей...
        </div>
      )}

      {manufacturersQuery.isError && (
        <div className="mt-4 rounded-xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-200">
          {getApiErrorMessage(
            manufacturersQuery.error,
            "Не удалось загрузить справочник производителей.",
          )}
        </div>
      )}

      {!manufacturersQuery.isLoading && !manufacturersQuery.isError && (
        <div className="mt-4">
          <label className="text-sm font-medium text-slate-200">
            Канонический производитель
          </label>

          <div className="mt-2">
            <AppSelect
              ariaLabel={`Производитель для alias ${group.sourceValue}`}
              value={selectedManufacturerId}
              disabled={
                createAliasMutation.isPending ||
                repeatAnalysisMutation.isPending ||
                createdAlias !== null
              }
              onChange={(manufacturerId) => {
                setSelectedManufacturerId(manufacturerId);
                setChangeConfirmed(false);
                createAliasMutation.reset();
                repeatAnalysisMutation.reset();
              }}
              options={[
                {
                  value: "",
                  label: "Выберите производителя",
                },
                ...manufacturers.map((manufacturer) => ({
                  value: manufacturer.id,
                  label: manufacturer.name,
                })),
              ]}
            />
          </div>
        </div>
      )}

      {selectedManufacturer && !createdAlias && (
        <div className="mt-4 rounded-xl border border-violet-500/25 bg-violet-500/[0.06] p-4">
          <p className="text-xs text-violet-300">Будет создано соответствие</p>

          <div className="mt-3 flex flex-wrap items-center gap-3">
            <span className="break-words font-mono text-sm font-semibold text-white">
              {group.sourceValue}
            </span>

            <span className="text-slate-500">→</span>

            <span className="text-sm font-semibold text-teal-300">
              {selectedManufacturer.name}
            </span>
          </div>

          <p className="mt-3 break-all font-mono text-xs text-slate-500">
            ManufacturerId: {selectedManufacturer.id}
          </p>
        </div>
      )}

      {selectedManufacturer && !createdAlias && (
        <label className="mt-4 flex cursor-pointer items-start gap-3 rounded-xl border border-white/10 bg-black/20 p-4">
          <input
            type="checkbox"
            checked={changeConfirmed}
            disabled={
              createAliasMutation.isPending || repeatAnalysisMutation.isPending
            }
            onChange={(event) => setChangeConfirmed(event.target.checked)}
            className="mt-1 size-4 shrink-0 accent-teal-500"
          />

          <span>
            <span className="block text-sm font-medium text-white">
              Я проверил соответствие и подтверждаю создание Approved alias
            </span>

            <span className="mt-1 block text-xs leading-5 text-slate-400">
              Фраза «{group.sourceValue}» будет постоянно связана с
              производителем «{selectedManufacturer.name}».
            </span>
          </span>
        </label>
      )}

      {createAliasMutation.isError && (
        <div className="mt-4 rounded-xl border border-red-500/30 bg-red-500/10 p-4">
          <p className="text-sm font-medium text-red-100">Alias не создан</p>

          <p className="mt-2 text-sm leading-6 text-red-200">
            {getApiErrorMessage(
              createAliasMutation.error,
              "Не удалось создать псевдоним производителя.",
            )}
          </p>
        </div>
      )}

      {createdAlias && (
        <div className="mt-4 rounded-xl border border-green-500/30 bg-green-500/10 p-4">
          <p className="text-sm font-medium text-green-100">
            Approved alias создан
          </p>

          <p className="mt-2 text-sm leading-6 text-green-200/80">
            Фраза «{createdAlias.phrase}» сохранена как alias производителя «
            {createdAlias.manufacturerName}».
          </p>

          <dl className="mt-3 grid gap-3 md:grid-cols-2">
            <div>
              <dt className="text-xs text-green-300/70">ManufacturerAliasId</dt>

              <dd className="mt-1 break-all font-mono text-xs text-green-100">
                {createdAlias.manufacturerAliasId}
              </dd>
            </div>

            <div>
              <dt className="text-xs text-green-300/70">Статус и источник</dt>

              <dd className="mt-1 font-mono text-xs text-green-100">
                {createdAlias.status} / {createdAlias.source}
              </dd>
            </div>
          </dl>
        </div>
      )}

      {repeatAnalysisMutation.isPending && (
        <div className="mt-4 rounded-xl border border-sky-500/30 bg-sky-500/10 p-4 text-sm leading-6 text-sky-200">
          Alias уже сохранён. Повторно анализируем Excel, чтобы обновить строки
          и сводку производителей...
        </div>
      )}

      {createdAlias && repeatAnalysisMutation.isError && (
        <div className="mt-4 rounded-xl border border-red-500/30 bg-red-500/10 p-4">
          <p className="text-sm font-medium text-red-100">
            Alias создан, но повторный анализ не завершён
          </p>

          <p className="mt-2 text-sm leading-6 text-red-200">
            {getApiErrorMessage(
              repeatAnalysisMutation.error,
              "Не удалось повторно проанализировать пакет.",
            )}
          </p>

          <p className="mt-2 text-xs leading-5 text-red-200/70">
            Не нажимайте создание alias повторно: запись уже находится в
            PostgreSQL. Повторите только анализ.
          </p>

          <button
            type="button"
            onClick={() => repeatAnalysisMutation.mutate()}
            className="mt-4 rounded-xl border border-sky-500/30 bg-sky-500/10 px-4 py-2 text-sm font-medium text-sky-200 transition hover:bg-sky-500/20"
          >
            Повторить анализ пакета
          </button>
        </div>
      )}

      {!createdAlias && (
        <div className="mt-4 flex justify-end">
          <button
            type="submit"
            disabled={
              !selectedManufacturerId ||
              !changeConfirmed ||
              manufacturersQuery.isLoading ||
              manufacturersQuery.isError ||
              createAliasMutation.isPending ||
              repeatAnalysisMutation.isPending
            }
            className="rounded-xl bg-teal-500 px-5 py-3 text-sm font-semibold text-white transition hover:bg-teal-400 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {createAliasMutation.isPending
              ? "Создаём Approved alias..."
              : "Создать Approved alias"}
          </button>
        </div>
      )}
    </form>
  );
}
