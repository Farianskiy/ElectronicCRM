"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import type { FormEvent } from "react";
import { useState } from "react";
import { addCatalogDictionaryTerm } from "@/features/catalogDictionaries/api/addCatalogDictionaryTerm";
import { catalogDictionaryTermsQueryKey } from "@/features/catalogDictionaries/api/getCatalogDictionaryTerms";
import type { CatalogProductNameRecognitionPreview } from "@/features/catalogRecognition/model/types";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppSelect } from "@/shared/ui/AppSelect";

type DictionaryTermScope = "Global" | "CurrentProductType";

interface RecognitionDictionaryTermCreationPanelProps {
  preview: CatalogProductNameRecognitionPreview;
  isRecognitionRefreshing: boolean;
  onRecognitionRefresh: () => Promise<void>;
}

export function RecognitionDictionaryTermCreationPanel({
  preview,
  isRecognitionRefreshing,
  onRecognitionRefresh,
}: RecognitionDictionaryTermCreationPanelProps) {
  const queryClient = useQueryClient();

  const availableCharacteristicCodes = preview.allowedCharacteristicCodes ?? [];

  const initialTargetCode = availableCharacteristicCodes[0] ?? "";

  const [scope, setScope] = useState<DictionaryTermScope>("CurrentProductType");

  const [phrase, setPhrase] = useState("ТЕСТТОК");

  const [targetCode, setTargetCode] = useState(initialTargetCode);

  const [targetValue, setTargetValue] = useState("");

  const [priorityText, setPriorityText] = useState("100");

  const [validationError, setValidationError] = useState<string | null>(null);

  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const [refreshError, setRefreshError] = useState<string | null>(null);

  const addTermMutation = useMutation({
    mutationFn: addCatalogDictionaryTerm,

    onSuccess: async (result) => {
      await queryClient.invalidateQueries({
        queryKey: catalogDictionaryTermsQueryKey,
      });

      const scopeDescription = result.productTypeCode
        ? `тип товара ${result.productTypeCode}`
        : "глобальная область";

      setSuccessMessage(
        `Термин '${result.phrase}' создан. Область: ${scopeDescription}.`,
      );

      setRefreshError(null);

      try {
        await onRecognitionRefresh();
      } catch (error) {
        setRefreshError(
          getApiErrorMessage(
            error,
            "Термин сохранён, но повторный запуск Preview завершился ошибкой.",
          ),
        );
      }
    },
  });

  const currentProductTypeIsAvailable =
    preview.hasProductTypeScope && preview.productTypeCode !== null;

  const requestIsPending = addTermMutation.isPending || isRecognitionRefreshing;

  const scopeOptions = [
    {
      value: "Global",
      label: "Глобальный — используется для всех типов",
    },
    {
      value: "CurrentProductType",
      label: currentProductTypeIsAvailable
        ? `Текущий тип — ${preview.productTypeCode}`
        : "Текущий тип недоступен в режиме ассистента",
      disabled: !currentProductTypeIsAvailable,
    },
  ];

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    const normalizedPhrase = phrase.trim();
    const normalizedTargetValue = targetValue.trim();
    const priority = Number(priorityText);

    if (normalizedPhrase.length === 0) {
      setValidationError("Введите распознаваемую фразу Phrase.");

      return;
    }

    if (targetCode.length === 0) {
      setValidationError("Выберите характеристику TargetCode.");

      return;
    }

    if (normalizedTargetValue.length === 0) {
      setValidationError("Введите итоговое значение TargetValue.");

      return;
    }

    if (!Number.isInteger(priority) || priority <= 0) {
      setValidationError("Priority должен быть положительным целым числом.");

      return;
    }

    if (scope === "CurrentProductType" && !preview.productTypeCode) {
      setValidationError(
        "Для scoped-термина сначала выберите тип товара и запустите Preview.",
      );

      return;
    }

    setValidationError(null);
    setSuccessMessage(null);
    setRefreshError(null);

    addTermMutation.mutate({
      productTypeCode:
        scope === "CurrentProductType" ? preview.productTypeCode : null,

      phrase: normalizedPhrase,
      kind: "Characteristic",
      targetCode,
      targetValue: normalizedTargetValue,
      priority,
    });
  }

  function resetMessages(): void {
    setValidationError(null);
    setSuccessMessage(null);
    setRefreshError(null);
    addTermMutation.reset();
  }

  if (availableCharacteristicCodes.length === 0) {
    return (
      <section className="rounded-3xl border border-amber-500/25 bg-amber-500/[0.06] p-6">
        <h2 className="text-xl font-semibold text-amber-100">
          Создание тестового словарного термина недоступно
        </h2>

        <p className="mt-2 text-sm text-amber-200/80">
          В текущем Preview отсутствует список разрешённых характеристик.
          Выберите конкретный тип товара и повторно запустите распознавание.
        </p>
      </section>
    );
  }

  return (
    <section className="rounded-3xl border border-fuchsia-500/25 bg-fuchsia-500/[0.04] p-6">
      <div>
        <h2 className="text-xl font-semibold text-white">
          Тестовый словарный термин
        </h2>

        <p className="mt-2 max-w-3xl text-sm text-slate-400">
          Создаёт подтверждённый CatalogDictionaryTerm и затем повторно
          запускает Recognition Preview.
        </p>
      </div>

      <div className="mt-5 rounded-2xl border border-red-500/25 bg-red-500/[0.06] p-4">
        <p className="text-sm font-medium text-red-100">
          Это реальное изменение данных
        </p>

        <p className="mt-2 text-sm text-red-200/80">
          Термин будет сохранён в PostgreSQL со статусом Approved. При ошибке
          его можно безопасно отключить в секции «Словарь распознавания». Запись
          останется в истории и сможет быть возвращена в работу.
        </p>
      </div>

      <form onSubmit={handleSubmit} className="mt-6 grid gap-5">
        <div className="grid gap-2">
          <span className="text-sm font-medium text-slate-300">
            Область действия
          </span>

          <AppSelect
            ariaLabel="Область действия словарного термина"
            value={scope}
            options={scopeOptions}
            disabled={requestIsPending}
            onChange={(value) => {
              setScope(value as DictionaryTermScope);
              resetMessages();
            }}
          />

          <p className="text-xs text-slate-500">
            Scoped-термин имеет приоритет над глобальным термином только внутри
            выбранного типа товара.
          </p>
        </div>

        <div className="grid gap-4 lg:grid-cols-2">
          <label className="grid gap-2">
            <span className="text-sm font-medium text-slate-300">
              Phrase — фраза в наименовании
            </span>

            <input
              value={phrase}
              disabled={requestIsPending}
              onChange={(event) => {
                setPhrase(event.target.value);
                resetMessages();
              }}
              placeholder="Например: ТЕСТТОК"
              className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none placeholder:text-slate-600 focus:border-fuchsia-400"
            />

            <span className="text-xs text-slate-500">
              Эта фраза должна буквально присутствовать в проверяемом
              наименовании.
            </span>
          </label>

          <div className="grid gap-2">
            <span className="text-sm font-medium text-slate-300">
              TargetCode — характеристика
            </span>

            <AppSelect
              ariaLabel="Характеристика словарного термина"
              value={targetCode}
              options={availableCharacteristicCodes.map(
                (characteristicCode) => ({
                  value: characteristicCode,
                  label: characteristicCode,
                }),
              )}
              disabled={requestIsPending}
              onChange={(value) => {
                setTargetCode(value);
                resetMessages();
              }}
            />

            <span className="text-xs text-slate-500">
              Список ограничен схемой текущего типа товара.
            </span>
          </div>
        </div>

        <div className="grid gap-4 lg:grid-cols-2">
          <label className="grid gap-2">
            <span className="text-sm font-medium text-slate-300">
              TargetValue — итоговое значение
            </span>

            <input
              value={targetValue}
              disabled={requestIsPending}
              onChange={(event) => {
                setTargetValue(event.target.value);
                resetMessages();
              }}
              placeholder="Например: 111 или C"
              className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none placeholder:text-slate-600 focus:border-fuchsia-400"
            />
          </label>

          <label className="grid gap-2">
            <span className="text-sm font-medium text-slate-300">Priority</span>

            <input
              type="number"
              min={1}
              step={1}
              value={priorityText}
              disabled={requestIsPending}
              onChange={(event) => {
                setPriorityText(event.target.value);
                resetMessages();
              }}
              className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none focus:border-fuchsia-400"
            />

            <span className="text-xs text-slate-500">
              При одинаковой длине фразы движок выбирает термин с большим
              Priority.
            </span>
          </label>
        </div>

        <div className="rounded-2xl border border-white/10 bg-black/20 p-5">
          <p className="text-xs text-slate-500">Будет отправлено</p>

          <dl className="mt-3 grid gap-3 text-sm sm:grid-cols-2 xl:grid-cols-5">
            <PreviewValue
              label="ProductTypeCode"
              value={
                scope === "CurrentProductType"
                  ? (preview.productTypeCode ?? "null")
                  : "null"
              }
            />

            <PreviewValue label="Phrase" value={phrase.trim() || "—"} />

            <PreviewValue label="Kind" value="Characteristic" />

            <PreviewValue label="TargetCode" value={targetCode || "—"} />

            <PreviewValue
              label="TargetValue"
              value={targetValue.trim() || "—"}
            />
          </dl>
        </div>

        {validationError && (
          <div className="rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-200">
            {validationError}
          </div>
        )}

        {addTermMutation.isError && (
          <div className="rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-200">
            {getApiErrorMessage(
              addTermMutation.error,
              "Не удалось создать словарный термин.",
            )}
          </div>
        )}

        {successMessage && (
          <div className="rounded-2xl border border-green-500/30 bg-green-500/10 p-4 text-sm text-green-200">
            {successMessage}
          </div>
        )}

        {refreshError && (
          <div className="rounded-2xl border border-amber-500/30 bg-amber-500/10 p-4 text-sm text-amber-200">
            {refreshError}
          </div>
        )}

        <div>
          <button
            type="submit"
            disabled={requestIsPending}
            className="rounded-2xl bg-fuchsia-500 px-5 py-3 text-sm font-medium text-white transition hover:bg-fuchsia-400 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {addTermMutation.isPending
              ? "Создаём термин..."
              : isRecognitionRefreshing
                ? "Обновляем Preview..."
                : "Создать Approved-термин"}
          </button>
        </div>
      </form>
    </section>
  );
}

function PreviewValue({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-xs text-slate-500">{label}</dt>

      <dd className="mt-1 break-all font-mono text-slate-200">{value}</dd>
    </div>
  );
}
