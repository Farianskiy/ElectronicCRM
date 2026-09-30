"use client";

import { useMutation } from "@tanstack/react-query";
import type { FormEvent } from "react";
import { useState } from "react";
import { setCatalogCharacteristicRecognitionProfileActive } from "../api/setCatalogCharacteristicRecognitionProfileActive";
import { updateCatalogCharacteristicRecognitionProfile } from "../api/updateCatalogCharacteristicRecognitionProfile";
import {
  formatRecognitionProfileConfiguration,
  validateRecognitionProfileForm,
} from "../model/recognitionProfileFormSupport";
import type { CatalogCharacteristicRecognitionProfile } from "../model/types";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";

interface RecognitionProfileEditorProps {
  profile: CatalogCharacteristicRecognitionProfile;
  isRecognitionRefreshing: boolean;
  onCompleted: (message: string) => Promise<void>;
}

export function RecognitionProfileEditor({
  profile,
  isRecognitionRefreshing,
  onCompleted,
}: RecognitionProfileEditorProps) {
  const [isEditing, setIsEditing] = useState(false);
  const [priorityText, setPriorityText] = useState(profile.priority.toString());

  const [minimumConfidenceText, setMinimumConfidenceText] = useState(
    profile.minimumConfidence.toString(),
  );

  const [configurationJson, setConfigurationJson] = useState(
    formatRecognitionProfileConfiguration(profile.configurationJson),
  );

  const [validationError, setValidationError] = useState<string | null>(null);

  const updateMutation = useMutation({
    mutationFn: updateCatalogCharacteristicRecognitionProfile,

    onSuccess: async () => {
      setIsEditing(false);

      await onCompleted(
        `Профиль '${profile.id}' обновлён. Preview повторно запущен.`,
      );
    },
  });

  const activeMutation = useMutation({
    mutationFn: setCatalogCharacteristicRecognitionProfileActive,

    onSuccess: async (_, parameters) => {
      await onCompleted(
        parameters.isActive
          ? `Профиль '${profile.id}' включён. Preview повторно запущен.`
          : `Профиль '${profile.id}' отключён. Preview повторно запущен.`,
      );
    },
  });

  const requestIsPending =
    updateMutation.isPending ||
    activeMutation.isPending ||
    isRecognitionRefreshing;

  const requestError = updateMutation.error ?? activeMutation.error;

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    const formError = validateRecognitionProfileForm(
      profile.strategyKind,
      priorityText,
      minimumConfidenceText,
      configurationJson,
    );

    if (formError) {
      setValidationError(formError);
      return;
    }

    setValidationError(null);

    updateMutation.mutate({
      profileId: profile.id,
      strategyKind: profile.strategyKind,
      priority: Number(priorityText),
      minimumConfidence: Number(minimumConfidenceText),
      configurationJson,
    });
  }

  function handleCancel(): void {
    setPriorityText(profile.priority.toString());
    setMinimumConfidenceText(profile.minimumConfidence.toString());

    setConfigurationJson(
      formatRecognitionProfileConfiguration(profile.configurationJson),
    );

    setValidationError(null);
    updateMutation.reset();
    setIsEditing(false);
  }

  function handleActiveToggle(): void {
    if (profile.isActive) {
      const confirmed = window.confirm(
        `Отключить профиль ${profile.characteristicCode}? Recognition Engine перестанет применять его для выбранного типа товара.`,
      );

      if (!confirmed) {
        return;
      }
    }

    setValidationError(null);
    activeMutation.reset();

    activeMutation.mutate({
      profileId: profile.id,
      isActive: !profile.isActive,
    });
  }

  return (
    <article className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
      <div className="flex flex-col justify-between gap-5 lg:flex-row lg:items-start">
        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-2">
            <h3 className="text-lg font-semibold text-white">
              {profile.characteristicName}
            </h3>

            <span
              className={
                profile.isActive
                  ? "rounded-full border border-green-500/30 bg-green-500/10 px-3 py-1 text-xs text-green-300"
                  : "rounded-full border border-slate-500/30 bg-slate-500/10 px-3 py-1 text-xs text-slate-300"
              }
            >
              {profile.isActive ? "Активен" : "Отключён"}
            </span>
          </div>

          <p className="mt-2 break-all font-mono text-sm text-teal-300">
            {profile.characteristicCode}
          </p>

          <p className="mt-2 text-sm text-slate-400">
            StrategyKind:
            <span className="ml-2 font-mono text-slate-200">
              {profile.strategyKind}
            </span>
          </p>
        </div>

        <div className="flex flex-wrap gap-3">
          <button
            type="button"
            disabled={requestIsPending}
            onClick={() => {
              setValidationError(null);
              updateMutation.reset();
              setIsEditing((currentValue) => !currentValue);
            }}
            className="rounded-xl border border-blue-500/30 bg-blue-500/10 px-4 py-2 text-sm font-medium text-blue-200 transition hover:bg-blue-500/20 disabled:opacity-50"
          >
            {isEditing ? "Скрыть редактор" : "Редактировать"}
          </button>

          <button
            type="button"
            disabled={requestIsPending}
            onClick={handleActiveToggle}
            className={
              profile.isActive
                ? "rounded-xl border border-red-500/30 bg-red-500/10 px-4 py-2 text-sm font-medium text-red-200 transition hover:bg-red-500/20 disabled:opacity-50"
                : "rounded-xl border border-green-500/30 bg-green-500/10 px-4 py-2 text-sm font-medium text-green-200 transition hover:bg-green-500/20 disabled:opacity-50"
            }
          >
            {activeMutation.isPending
              ? "Сохраняем..."
              : profile.isActive
                ? "Отключить"
                : "Включить"}
          </button>
        </div>
      </div>

      {isEditing && (
        <form
          onSubmit={handleSubmit}
          className="mt-6 grid gap-5 border-t border-white/10 pt-6"
        >
          <div className="rounded-2xl border border-amber-500/20 bg-amber-500/[0.06] p-4 text-sm text-amber-100/80">
            Тип товара, характеристика и StrategyKind не изменяются. Здесь
            редактируется только поведение существующего профиля.
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <label className="grid gap-2">
              <span className="text-sm font-medium text-slate-300">
                Priority
              </span>

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
                  updateMutation.reset();
                }}
                className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none focus:border-teal-400"
              />
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
                  updateMutation.reset();
                }}
                className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none focus:border-teal-400"
              />
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
                updateMutation.reset();
              }}
              spellCheck={false}
              className="resize-y rounded-2xl border border-white/10 bg-black/40 px-4 py-3 font-mono text-sm leading-6 text-slate-200 outline-none focus:border-teal-400"
            />
          </label>

          {profile.strategyKind === "BooleanAlias" && (
            <div className="grid gap-3 rounded-2xl border border-violet-500/20 bg-violet-500/[0.05] p-5 text-sm text-slate-300">
              <p className="font-medium text-violet-200">
                Настройки BooleanAlias
              </p>

              <p>
                <span className="font-mono text-green-300">trueAliases</span>
                <span className="mx-2 text-slate-600">—</span>
                фразы, явно подтверждающие наличие характеристики.
              </p>

              <p>
                <span className="font-mono text-red-300">falseAliases</span>
                <span className="mx-2 text-slate-600">—</span>
                фразы, явно подтверждающие отсутствие характеристики.
              </p>

              <p className="text-slate-400">
                Регистр букв не учитывается. Несколько пробелов между словами
                допускаются. Латинские и кириллические буквы считаются разными
                символами, поэтому необходимые варианты нужно указывать явно.
              </p>
            </div>
          )}

          {validationError && (
            <div className="rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-200">
              {validationError}
            </div>
          )}

          {requestError && (
            <div className="rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-200">
              {getApiErrorMessage(
                requestError,
                "Не удалось изменить профиль распознавания.",
              )}
            </div>
          )}

          <div className="flex flex-wrap gap-3">
            <button
              type="submit"
              disabled={requestIsPending}
              className="rounded-xl bg-teal-500 px-5 py-3 text-sm font-medium text-white transition hover:bg-teal-400 disabled:opacity-50"
            >
              {updateMutation.isPending
                ? "Сохраняем..."
                : isRecognitionRefreshing
                  ? "Обновляем Preview..."
                  : "Сохранить изменения"}
            </button>

            <button
              type="button"
              disabled={requestIsPending}
              onClick={handleCancel}
              className="rounded-xl border border-white/10 bg-white/[0.04] px-5 py-3 text-sm font-medium text-slate-300 transition hover:bg-white/[0.08] disabled:opacity-50"
            >
              Отмена
            </button>
          </div>
        </form>
      )}

      {!isEditing && requestError && (
        <div className="mt-5 rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-200">
          {getApiErrorMessage(
            requestError,
            "Не удалось изменить состояние профиля.",
          )}
        </div>
      )}
    </article>
  );
}
