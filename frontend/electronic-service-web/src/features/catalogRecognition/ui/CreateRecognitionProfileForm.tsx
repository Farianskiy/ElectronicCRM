"use client";

import { useMutation } from "@tanstack/react-query";
import type { FormEvent } from "react";
import { useState } from "react";
import { createCatalogCharacteristicRecognitionProfile } from "../api/createCatalogCharacteristicRecognitionProfile";
import {
  getRecognitionStrategyDescriptor,
  validateRecognitionProfileForm,
} from "../model/recognitionProfileFormSupport";
import type { CatalogProductTypeCharacteristicSchemaItem } from "@/features/catalogProductTypes/model/types";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppSelect } from "@/shared/ui/AppSelect";

interface CreateRecognitionProfileFormProps {
  productTypeCode: string;
  characteristics: CatalogProductTypeCharacteristicSchemaItem[];
  isRecognitionRefreshing: boolean;
  onCompleted: (message: string) => Promise<void>;
}

export function CreateRecognitionProfileForm({
  productTypeCode,
  characteristics,
  isRecognitionRefreshing,
  onCompleted,
}: CreateRecognitionProfileFormProps) {
  const initialCharacteristic = characteristics[0] ?? null;

  const [selectedDefinitionId, setSelectedDefinitionId] = useState(
    initialCharacteristic?.definitionId ?? "",
  );

  const [priorityText, setPriorityText] = useState("1000");
  const [minimumConfidenceText, setMinimumConfidenceText] = useState("0.98");

  const [configurationJson, setConfigurationJson] = useState(
    initialCharacteristic
      ? (getRecognitionStrategyDescriptor(initialCharacteristic.code)
          ?.defaultConfigurationJson ?? "{}")
      : "{}",
  );

  const [validationError, setValidationError] = useState<string | null>(null);

  const selectedCharacteristic =
    characteristics.find(
      (characteristic) => characteristic.definitionId === selectedDefinitionId,
    ) ?? initialCharacteristic;

  const strategyDescriptor = selectedCharacteristic
    ? getRecognitionStrategyDescriptor(selectedCharacteristic.code)
    : null;

  const createMutation = useMutation({
    mutationFn: createCatalogCharacteristicRecognitionProfile,

    onSuccess: async (result) => {
      await onCompleted(
        `Профиль '${result.profileId}' создан. Preview повторно запущен.`,
      );
    },
  });

  function handleCharacteristicChange(definitionId: string): void {
    const characteristic = characteristics.find(
      (item) => item.definitionId === definitionId,
    );

    setSelectedDefinitionId(definitionId);
    setValidationError(null);
    createMutation.reset();

    if (!characteristic) {
      return;
    }

    const descriptor = getRecognitionStrategyDescriptor(characteristic.code);

    setConfigurationJson(descriptor?.defaultConfigurationJson ?? "{}");
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    if (!selectedCharacteristic || !strategyDescriptor) {
      setValidationError(
        "Для выбранной характеристики не определена стратегия распознавания.",
      );
      return;
    }

    const formError = validateRecognitionProfileForm(
      strategyDescriptor.strategyKind,
      priorityText,
      minimumConfidenceText,
      configurationJson,
    );

    if (formError) {
      setValidationError(formError);
      return;
    }

    setValidationError(null);

    createMutation.mutate({
      productTypeCode,
      characteristicDefinitionId: selectedCharacteristic.definitionId,
      strategyKind: strategyDescriptor.strategyKind,
      priority: Number(priorityText),
      minimumConfidence: Number(minimumConfidenceText),
      configurationJson,
    });
  }

  if (!selectedCharacteristic || !strategyDescriptor) {
    return (
      <div className="rounded-2xl border border-amber-500/25 bg-amber-500/[0.06] p-5 text-sm text-amber-200">
        Нет характеристик, для которых сейчас можно создать профиль.
      </div>
    );
  }

  const requestIsPending = createMutation.isPending || isRecognitionRefreshing;

  return (
    <form
      onSubmit={handleSubmit}
      className="grid gap-5 rounded-3xl border border-teal-500/25 bg-teal-500/[0.05] p-6"
    >
      <div>
        <h3 className="text-lg font-semibold text-white">Новый профиль</h3>

        <p className="mt-2 text-sm text-slate-400">
          Профиль будет создан для типа
          <span className="mx-1 font-mono text-teal-200">
            {productTypeCode}
          </span>
          .
        </p>
      </div>

      <div className="grid gap-2">
        <span className="text-sm font-medium text-slate-300">
          Характеристика
        </span>

        <AppSelect
          ariaLabel="Характеристика нового профиля"
          value={selectedCharacteristic.definitionId}
          options={characteristics.map((characteristic) => ({
            value: characteristic.definitionId,
            label: `${characteristic.name} — ${characteristic.code}`,
          }))}
          disabled={requestIsPending}
          onChange={handleCharacteristicChange}
        />
      </div>

      <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
        <p className="text-xs text-slate-500">StrategyKind</p>

        <p className="mt-2 font-mono text-sm text-teal-200">
          {strategyDescriptor.strategyKind}
        </p>

        <p className="mt-2 text-sm text-slate-400">
          {strategyDescriptor.description}
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <label className="grid gap-2">
          <span className="text-sm font-medium text-slate-300">Priority</span>

          <input
            type="number"
            min={1}
            max={10000}
            step={1}
            value={priorityText}
            disabled={requestIsPending}
            onChange={(event) => {
              setPriorityText(event.target.value);
              setValidationError(null);
              createMutation.reset();
            }}
            className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none focus:border-teal-400"
          />

          <span className="text-xs text-slate-500">
            Допустимый диапазон: 1–10000.
          </span>
        </label>

        <label className="grid gap-2">
          <span className="text-sm font-medium text-slate-300">
            MinimumConfidence
          </span>

          <input
            type="number"
            min="0.0001"
            max="1"
            step="0.0001"
            value={minimumConfidenceText}
            disabled={requestIsPending}
            onChange={(event) => {
              setMinimumConfidenceText(event.target.value);
              setValidationError(null);
              createMutation.reset();
            }}
            className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none focus:border-teal-400"
          />

          <span className="text-xs text-slate-500">
            Например, 0.98 означает 98%.
          </span>
        </label>
      </div>

      <label className="grid gap-2">
        <span className="text-sm font-medium text-slate-300">
          ConfigurationJson
        </span>

        <textarea
          rows={12}
          value={configurationJson}
          disabled={requestIsPending}
          onChange={(event) => {
            setConfigurationJson(event.target.value);
            setValidationError(null);
            createMutation.reset();
          }}
          spellCheck={false}
          className="resize-y rounded-2xl border border-white/10 bg-black/40 px-4 py-3 font-mono text-sm leading-6 text-slate-200 outline-none focus:border-teal-400"
        />
      </label>

      {strategyDescriptor.strategyKind === "BooleanAlias" && (
        <div className="grid gap-3 rounded-2xl border border-violet-500/20 bg-violet-500/[0.05] p-5 text-sm text-slate-300">
          <p className="font-medium text-violet-200">
            Как заполнить BooleanAlias
          </p>

          <p>
            В массиве
            <span className="mx-1 font-mono text-green-300">trueAliases</span>
            перечисляются фразы, означающие наличие характеристики.
          </p>

          <p>
            В массиве
            <span className="mx-1 font-mono text-red-300">falseAliases</span>
            перечисляются фразы, означающие отсутствие характеристики.
          </p>
        </div>
      )}

      {validationError && (
        <div className="rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-200">
          {validationError}
        </div>
      )}

      {createMutation.isError && (
        <div className="rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-200">
          {getApiErrorMessage(
            createMutation.error,
            "Не удалось создать профиль распознавания.",
          )}
        </div>
      )}

      <div>
        <button
          type="submit"
          disabled={requestIsPending}
          className="rounded-2xl bg-teal-500 px-5 py-3 text-sm font-medium text-white transition hover:bg-teal-400 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {createMutation.isPending
            ? "Создаём профиль..."
            : isRecognitionRefreshing
              ? "Обновляем Preview..."
              : "Создать профиль"}
        </button>
      </div>
    </form>
  );
}
