"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useState, type FormEvent } from "react";
import { useAuthSession } from "@/features/auth/model/useAuthSession";
import { markManufacturerPhraseAsNoise } from "@/features/catalogManufacturers/api/markManufacturerPhraseAsNoise";
import type { MarkManufacturerPhraseAsNoiseResponse } from "@/features/catalogManufacturers/model/types";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { isTechnicalUser } from "@/shared/api/authToken";
import { analyzeCatalogImportBatch } from "../../api/analyzeCatalogImportBatch";
import { catalogImportQueryKeys } from "../../model/queryKeys";
import type {
  AnalyzeCatalogImportBatchResponse,
  CatalogImportManufacturerResolutionGroup,
} from "../../model/types";

interface CatalogImportManufacturerNoiseFormProps {
  batchId: string;
  productTypeId?: string | null;
  group: CatalogImportManufacturerResolutionGroup;
  onAnalysisChange: (analysis: AnalyzeCatalogImportBatchResponse) => void;
}

export function CatalogImportManufacturerNoiseForm({
  batchId,
  productTypeId,
  group,
  onAnalysisChange,
}: CatalogImportManufacturerNoiseFormProps) {
  const queryClient = useQueryClient();
  const session = useAuthSession();
  const canManageManufacturers = isTechnicalUser(session);

  const [reason, setReason] = useState("");
  const [changeConfirmed, setChangeConfirmed] = useState(false);
  const [markedNoisePhrase, setMarkedNoisePhrase] =
    useState<MarkManufacturerPhraseAsNoiseResponse | null>(null);

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

  const markAsNoiseMutation = useMutation({
    mutationFn: () =>
      markManufacturerPhraseAsNoise({
        phrase: group.sourceValue,
        reason: reason.trim() || null,
      }),

    onSuccess: (noisePhrase) => {
      setMarkedNoisePhrase(noisePhrase);
      setChangeConfirmed(false);
      repeatAnalysisMutation.mutate();
    },
  });

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    if (!changeConfirmed) {
      return;
    }

    markAsNoiseMutation.reset();
    repeatAnalysisMutation.reset();
    setMarkedNoisePhrase(null);
    markAsNoiseMutation.mutate();
  }

  if (!canManageManufacturers) {
    return null;
  }

  return (
    <form
      onSubmit={handleSubmit}
      className="mt-4 rounded-2xl border border-orange-500/30 bg-orange-500/[0.06] p-5"
    >
      <div>
        <h4 className="font-semibold text-orange-100">Пометить как шум</h4>

        <p className="mt-2 text-sm leading-6 text-orange-200/80">
          Используйте этот вариант только тогда, когда значение из Excel вообще
          не является названием производителя. Например: «НЕ УКАЗАН», «НЕТ»,
          «N/A» или служебный текст.
        </p>
      </div>

      <div className="mt-4 rounded-xl border border-red-500/30 bg-red-500/[0.08] p-4">
        <p className="text-sm font-medium text-red-100">
          Это постоянное решение Technical-пользователя
        </p>

        <p className="mt-2 text-sm leading-6 text-red-200/80">
          После сохранения фраза «{group.sourceValue}» перестанет считаться
          неизвестным производителем во всех последующих импортах. Создать
          Manufacturer или ManufacturerAlias с такой активной фразой будет
          запрещено.
        </p>
      </div>

      <dl className="mt-4 grid gap-4 lg:grid-cols-2">
        <div className="rounded-xl border border-white/10 bg-black/20 p-4">
          <dt className="text-xs text-slate-500">Исходная фраза из Excel</dt>

          <dd className="mt-2 break-words font-mono text-sm text-white">
            {group.sourceValue}
          </dd>

          <dd className="mt-2 break-words font-mono text-xs text-slate-500">
            Нормализовано: {group.normalizedSourceValue}
          </dd>
        </div>

        <div className="rounded-xl border border-white/10 bg-black/20 p-4">
          <dt className="text-xs text-slate-500">
            Сколько строк затронет решение
          </dt>

          <dd className="mt-2 text-2xl font-semibold text-white">
            {group.occurrenceCount}
          </dd>

          <dd className="mt-2 text-xs leading-5 text-slate-500">
            После повторного анализа эти строки перейдут из Unresolved в
            IgnoredNoise.
          </dd>
        </div>
      </dl>

      {!markedNoisePhrase && (
        <div className="mt-4">
          <label
            htmlFor={`manufacturer-noise-reason-${group.normalizedSourceValue}`}
            className="text-sm font-medium text-slate-200"
          >
            Причина решения
          </label>

          <textarea
            id={`manufacturer-noise-reason-${group.normalizedSourceValue}`}
            value={reason}
            maxLength={500}
            disabled={
              markAsNoiseMutation.isPending || repeatAnalysisMutation.isPending
            }
            onChange={(event) => {
              setReason(event.target.value);
              setChangeConfirmed(false);
              markAsNoiseMutation.reset();
              repeatAnalysisMutation.reset();
            }}
            placeholder="Например: служебное значение поставщика, не является названием бренда."
            className="mt-2 min-h-28 w-full resize-y rounded-xl border border-white/10 bg-black/30 px-4 py-3 text-sm text-white outline-none transition placeholder:text-slate-600 focus:border-orange-500/50"
          />

          <p className="mt-2 text-xs text-slate-500">
            Причина необязательна, но она поможет другому Technical-пользователю
            понять, почему было принято это решение. Символов: {reason.length}{" "}
            из 500.
          </p>
        </div>
      )}

      {!markedNoisePhrase && (
        <label className="mt-4 flex cursor-pointer items-start gap-3 rounded-xl border border-white/10 bg-black/20 p-4">
          <input
            type="checkbox"
            checked={changeConfirmed}
            disabled={
              markAsNoiseMutation.isPending || repeatAnalysisMutation.isPending
            }
            onChange={(event) => setChangeConfirmed(event.target.checked)}
            className="mt-1 size-4 shrink-0 accent-orange-500"
          />

          <span>
            <span className="block text-sm font-medium text-white">
              Я проверил значение и подтверждаю, что это не производитель
            </span>

            <span className="mt-1 block text-xs leading-5 text-slate-400">
              Фраза «{group.sourceValue}» будет сохранена как активная
              ManufacturerNoisePhrase.
            </span>
          </span>
        </label>
      )}

      {markAsNoiseMutation.isError && (
        <div className="mt-4 rounded-xl border border-red-500/30 bg-red-500/10 p-4">
          <p className="text-sm font-medium text-red-100">
            Фраза не помечена как шум
          </p>

          <p className="mt-2 text-sm leading-6 text-red-200">
            {getApiErrorMessage(
              markAsNoiseMutation.error,
              "Не удалось сохранить решение о шумовой фразе.",
            )}
          </p>

          <p className="mt-2 text-xs leading-5 text-red-200/70">
            Ошибка 409 означает, что фраза уже используется как каноническое имя
            Manufacturer или активный ManufacturerAlias.
          </p>
        </div>
      )}

      {markedNoisePhrase && (
        <div className="mt-4 rounded-xl border border-green-500/30 bg-green-500/10 p-4">
          <p className="text-sm font-medium text-green-100">
            Решение сохранено
          </p>

          <p className="mt-2 text-sm leading-6 text-green-200/80">
            Фраза «{markedNoisePhrase.phrase}» сохранена как активный шум.
            Действие backend: {markedNoisePhrase.action}.
          </p>

          <dl className="mt-3 grid gap-3 md:grid-cols-2">
            <div>
              <dt className="text-xs text-green-300/70">
                ManufacturerNoisePhraseId
              </dt>

              <dd className="mt-1 break-all font-mono text-xs text-green-100">
                {markedNoisePhrase.manufacturerNoisePhraseId}
              </dd>
            </div>

            <div>
              <dt className="text-xs text-green-300/70">
                Нормализованная фраза
              </dt>

              <dd className="mt-1 break-words font-mono text-xs text-green-100">
                {markedNoisePhrase.normalizedPhrase}
              </dd>
            </div>
          </dl>
        </div>
      )}

      {repeatAnalysisMutation.isPending && (
        <div className="mt-4 rounded-xl border border-sky-500/30 bg-sky-500/10 p-4 text-sm leading-6 text-sky-200">
          Решение уже сохранено. Повторно анализируем Excel и обновляем сводку
          производителей...
        </div>
      )}

      {markedNoisePhrase && repeatAnalysisMutation.isError && (
        <div className="mt-4 rounded-xl border border-red-500/30 bg-red-500/10 p-4">
          <p className="text-sm font-medium text-red-100">
            Шум сохранён, но повторный анализ не завершён
          </p>

          <p className="mt-2 text-sm leading-6 text-red-200">
            {getApiErrorMessage(
              repeatAnalysisMutation.error,
              "Не удалось повторно проанализировать пакет.",
            )}
          </p>

          <p className="mt-2 text-xs leading-5 text-red-200/70">
            Не сохраняйте решение повторно: запись уже находится в PostgreSQL.
            Нужно повторить только анализ пакета.
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

      {!markedNoisePhrase && (
        <div className="mt-4 flex justify-end">
          <button
            type="submit"
            disabled={
              !changeConfirmed ||
              markAsNoiseMutation.isPending ||
              repeatAnalysisMutation.isPending
            }
            className="rounded-xl bg-orange-500 px-5 py-3 text-sm font-semibold text-white transition hover:bg-orange-400 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {markAsNoiseMutation.isPending
              ? "Сохраняем решение..."
              : "Пометить как шум"}
          </button>
        </div>
      )}
    </form>
  );
}
