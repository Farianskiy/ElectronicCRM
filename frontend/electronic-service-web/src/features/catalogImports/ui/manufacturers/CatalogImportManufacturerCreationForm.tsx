"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useState, type FormEvent } from "react";
import { useAuthSession } from "@/features/auth/model/useAuthSession";
import { createManufacturerFromUnresolvedPhrase } from "@/features/catalogManufacturers/api/createManufacturerFromUnresolvedPhrase";
import type { CreateManufacturerFromUnresolvedPhraseResponse } from "@/features/catalogManufacturers/model/types";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { isTechnicalUser } from "@/shared/api/authToken";
import { analyzeCatalogImportBatch } from "../../api/analyzeCatalogImportBatch";
import { catalogImportQueryKeys } from "../../model/queryKeys";
import type {
  AnalyzeCatalogImportBatchResponse,
  CatalogImportManufacturerResolutionGroup,
} from "../../model/types";

interface CatalogImportManufacturerCreationFormProps {
  batchId: string;
  productTypeId?: string | null;
  group: CatalogImportManufacturerResolutionGroup;
  onAnalysisChange: (analysis: AnalyzeCatalogImportBatchResponse) => void;
}

export function CatalogImportManufacturerCreationForm({
  batchId,
  productTypeId,
  group,
  onAnalysisChange,
}: CatalogImportManufacturerCreationFormProps) {
  const queryClient = useQueryClient();
  const session = useAuthSession();
  const canManageManufacturers = isTechnicalUser(session);

  const [canonicalName, setCanonicalName] = useState(group.sourceValue);
  const [changeConfirmed, setChangeConfirmed] = useState(false);
  const [createdManufacturer, setCreatedManufacturer] =
    useState<CreateManufacturerFromUnresolvedPhraseResponse | null>(null);

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

  const createManufacturerMutation = useMutation({
    mutationFn: () =>
      createManufacturerFromUnresolvedPhrase({
        canonicalName: canonicalName.trim(),
        sourcePhrase: group.sourceValue,
      }),

    onSuccess: async (manufacturer) => {
      setCreatedManufacturer(manufacturer);
      setChangeConfirmed(false);

      await queryClient.invalidateQueries({
        queryKey: ["catalog-manufacturers"],
      });

      repeatAnalysisMutation.mutate();
    },
  });

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    if (!canonicalName.trim() || !changeConfirmed) {
      return;
    }

    createManufacturerMutation.reset();
    repeatAnalysisMutation.reset();
    setCreatedManufacturer(null);
    createManufacturerMutation.mutate();
  }

  if (!canManageManufacturers) {
    return null;
  }

  return (
    <form
      onSubmit={handleSubmit}
      className="mt-4 rounded-2xl border border-violet-500/30 bg-violet-500/[0.06] p-5"
    >
      <div>
        <h4 className="font-semibold text-violet-100">
          Создать нового производителя
        </h4>

        <p className="mt-2 text-sm leading-6 text-violet-200/80">
          Используйте этот вариант только тогда, когда исходная фраза обозначает
          новый реальный бренд, которого ещё нет в справочнике Manufacturer.
        </p>
      </div>

      <div className="mt-4 rounded-xl border border-sky-500/25 bg-sky-500/[0.06] p-4">
        <p className="text-sm font-medium text-sky-100">
          Каноническое имя и исходная фраза имеют разное назначение
        </p>

        <p className="mt-2 text-sm leading-6 text-sky-200/80">
          Исходная фраза — это значение, которое пришло из Excel. Каноническое
          имя — постоянное правильное название производителя, которое будет
          отображаться в каталоге и использоваться во всех последующих
          операциях.
        </p>
      </div>

      <dl className="mt-4 grid gap-4 lg:grid-cols-2">
        <div className="rounded-xl border border-white/10 bg-black/20 p-4">
          <dt className="text-xs text-slate-500">
            Исходная неизвестная фраза из Excel
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
            Количество строк с этой фразой
          </dt>

          <dd className="mt-2 text-2xl font-semibold text-white">
            {group.occurrenceCount}
          </dd>

          <dd className="mt-2 text-xs leading-5 text-slate-500">
            После создания производителя пакет будет проанализирован повторно.
          </dd>
        </div>
      </dl>

      <div className="mt-4">
        <label
          htmlFor={`manufacturer-canonical-name-${group.normalizedSourceValue}`}
          className="text-sm font-medium text-slate-200"
        >
          Каноническое имя нового производителя
        </label>

        <input
          id={`manufacturer-canonical-name-${group.normalizedSourceValue}`}
          type="text"
          value={canonicalName}
          maxLength={200}
          disabled={
            createManufacturerMutation.isPending ||
            repeatAnalysisMutation.isPending ||
            createdManufacturer !== null
          }
          onChange={(event) => {
            setCanonicalName(event.target.value);
            setChangeConfirmed(false);
            createManufacturerMutation.reset();
            repeatAnalysisMutation.reset();
          }}
          placeholder="Например: Schneider Electric"
          className="mt-2 w-full rounded-xl border border-white/10 bg-black/30 px-4 py-3 text-sm text-white outline-none transition placeholder:text-slate-600 focus:border-violet-500/60 disabled:cursor-not-allowed disabled:opacity-60"
        />

        <p className="mt-2 text-xs leading-5 text-slate-500">
          Если имя после нормализации совпадёт с исходной фразой, будет создан
          только Manufacturer. Если значения различаются, backend атомарно
          создаст Manufacturer и Approved ManufacturerAlias.
        </p>
      </div>

      {!createdManufacturer && canonicalName.trim() && (
        <div className="mt-4 rounded-xl border border-violet-500/25 bg-black/20 p-4">
          <p className="text-xs text-violet-300">
            Операция, которая будет отправлена backend
          </p>

          <dl className="mt-3 grid gap-4 lg:grid-cols-2">
            <div>
              <dt className="text-xs text-slate-500">CanonicalName</dt>

              <dd className="mt-1 break-words font-mono text-sm text-white">
                {canonicalName.trim()}
              </dd>
            </div>

            <div>
              <dt className="text-xs text-slate-500">SourcePhrase</dt>

              <dd className="mt-1 break-words font-mono text-sm text-white">
                {group.sourceValue}
              </dd>
            </div>
          </dl>
        </div>
      )}

      <div className="mt-4 rounded-xl border border-red-500/30 bg-red-500/[0.08] p-4">
        <p className="text-sm font-medium text-red-100">
          Это постоянное изменение справочника
        </p>

        <p className="mt-2 text-sm leading-6 text-red-200/80">
          Новый Manufacturer останется в PostgreSQL после завершения текущего
          импорта и будет доступен во всём каталоге. Если каноническое имя
          отличается от фразы «{group.sourceValue}», вместе с производителем
          будет создан постоянный Approved alias.
        </p>
      </div>

      {!createdManufacturer && canonicalName.trim() && (
        <label className="mt-4 flex cursor-pointer items-start gap-3 rounded-xl border border-white/10 bg-black/20 p-4">
          <input
            type="checkbox"
            checked={changeConfirmed}
            disabled={
              createManufacturerMutation.isPending ||
              repeatAnalysisMutation.isPending
            }
            onChange={(event) => setChangeConfirmed(event.target.checked)}
            className="mt-1 size-4 shrink-0 accent-violet-500"
          />

          <span>
            <span className="block text-sm font-medium text-white">
              Я проверил данные и подтверждаю создание нового Manufacturer
            </span>

            <span className="mt-1 block text-xs leading-5 text-slate-400">
              Каноническое имя: «{canonicalName.trim()}». Исходная фраза: «
              {group.sourceValue}».
            </span>
          </span>
        </label>
      )}

      {createManufacturerMutation.isError && (
        <div className="mt-4 rounded-xl border border-red-500/30 bg-red-500/10 p-4">
          <p className="text-sm font-medium text-red-100">
            Производитель не создан
          </p>

          <p className="mt-2 text-sm leading-6 text-red-200">
            {getApiErrorMessage(
              createManufacturerMutation.error,
              "Не удалось создать нового производителя.",
            )}
          </p>

          <p className="mt-2 text-xs leading-5 text-red-200/70">
            Ошибка 409 означает, что каноническое имя или исходная фраза уже
            используются существующим Manufacturer или ManufacturerAlias.
          </p>
        </div>
      )}

      {createdManufacturer && (
        <div className="mt-4 rounded-xl border border-green-500/30 bg-green-500/10 p-4">
          <p className="text-sm font-medium text-green-100">
            Производитель создан
          </p>

          <p className="mt-2 text-sm leading-6 text-green-200/80">
            Создан Manufacturer «{createdManufacturer.manufacturerName}».
            Результат последующего разрешения:{" "}
            {createdManufacturer.resolutionSource}.
          </p>

          <dl className="mt-3 grid gap-3 md:grid-cols-2">
            <div>
              <dt className="text-xs text-green-300/70">ManufacturerId</dt>

              <dd className="mt-1 break-all font-mono text-xs text-green-100">
                {createdManufacturer.manufacturerId}
              </dd>
            </div>

            <div>
              <dt className="text-xs text-green-300/70">Нормализованное имя</dt>

              <dd className="mt-1 break-words font-mono text-xs text-green-100">
                {createdManufacturer.normalizedManufacturerName}
              </dd>
            </div>

            <div>
              <dt className="text-xs text-green-300/70">ManufacturerAliasId</dt>

              <dd className="mt-1 break-all font-mono text-xs text-green-100">
                {createdManufacturer.manufacturerAliasId ??
                  "Alias не потребовался"}
              </dd>
            </div>

            <div>
              <dt className="text-xs text-green-300/70">
                Источник следующего разрешения
              </dt>

              <dd className="mt-1 font-mono text-xs text-green-100">
                {createdManufacturer.resolutionSource}
              </dd>
            </div>
          </dl>
        </div>
      )}

      {repeatAnalysisMutation.isPending && (
        <div className="mt-4 rounded-xl border border-sky-500/30 bg-sky-500/10 p-4 text-sm leading-6 text-sky-200">
          Производитель уже сохранён. Повторно анализируем Excel, обновляем
          строки пакета и сводку производителей...
        </div>
      )}

      {createdManufacturer && repeatAnalysisMutation.isError && (
        <div className="mt-4 rounded-xl border border-red-500/30 bg-red-500/10 p-4">
          <p className="text-sm font-medium text-red-100">
            Производитель создан, но повторный анализ не завершён
          </p>

          <p className="mt-2 text-sm leading-6 text-red-200">
            {getApiErrorMessage(
              repeatAnalysisMutation.error,
              "Не удалось повторно проанализировать пакет.",
            )}
          </p>

          <p className="mt-2 text-xs leading-5 text-red-200/70">
            Не нажимайте создание производителя повторно: запись уже находится в
            PostgreSQL. Нужно повторить только анализ пакета.
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

      {!createdManufacturer && (
        <div className="mt-4 flex justify-end">
          <button
            type="submit"
            disabled={
              !canonicalName.trim() ||
              !changeConfirmed ||
              createManufacturerMutation.isPending ||
              repeatAnalysisMutation.isPending
            }
            className="rounded-xl bg-violet-500 px-5 py-3 text-sm font-semibold text-white transition hover:bg-violet-400 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {createManufacturerMutation.isPending
              ? "Создаём производителя..."
              : "Создать нового производителя"}
          </button>
        </div>
      )}
    </form>
  );
}
