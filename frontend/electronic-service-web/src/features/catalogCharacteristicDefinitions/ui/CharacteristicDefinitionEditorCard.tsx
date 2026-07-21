"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import type { FormEvent } from "react";
import { useState } from "react";
import { updateCharacteristicDefinition } from "../api/updateCharacteristicDefinition";
import type { CatalogCharacteristicDefinition } from "../model/types";

interface CharacteristicDefinitionEditorCardProps {
  definition: CatalogCharacteristicDefinition;
}

function getErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const responseData = error.response?.data;

    if (typeof responseData === "string") {
      return responseData;
    }

    if (typeof responseData?.detail === "string") {
      return responseData.detail;
    }

    if (typeof responseData?.message === "string") {
      return responseData.message;
    }
  }

  if (error instanceof Error) {
    return error.message;
  }

  return "Не удалось обновить характеристику.";
}

function formatDataType(dataType: string): string {
  if (dataType === "Text") {
    return "Текст";
  }

  if (dataType === "Number") {
    return "Число";
  }

  if (dataType === "Boolean") {
    return "Да / Нет";
  }

  return dataType;
}

export function CharacteristicDefinitionEditorCard({
  definition,
}: CharacteristicDefinitionEditorCardProps) {
  const queryClient = useQueryClient();

  const [isEditing, setIsEditing] = useState(false);

  const [nameDraft, setNameDraft] = useState<string | null>(null);

  const [unitDraft, setUnitDraft] = useState<string | null>(null);

  const [validationError, setValidationError] = useState<string | null>(null);

  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const name = nameDraft ?? definition.name;

  const unit = unitDraft ?? definition.unit ?? "";

  const normalizedUnit = unit.trim().length ? unit.trim() : null;

  const hasChanges =
    name.trim() !== definition.name ||
    normalizedUnit !== (definition.unit ?? null);

  const mutation = useMutation({
    mutationFn: async () => {
      await updateCharacteristicDefinition(definition.id, {
        name: name.trim(),
        unit: normalizedUnit,
      });
    },

    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: ["catalog-characteristic-definitions"],
        }),

        queryClient.invalidateQueries({
          queryKey: ["catalog-product-type-characteristic-schema"],
        }),

        queryClient.invalidateQueries({
          queryKey: ["catalog-product-type-available-definitions"],
        }),

        queryClient.invalidateQueries({
          queryKey: ["catalog-product-type-characteristics"],
        }),

        queryClient.invalidateQueries({
          queryKey: ["catalog-product-details"],
        }),
      ]);

      setNameDraft(null);
      setUnitDraft(null);
      setValidationError(null);
      setIsEditing(false);

      setSuccessMessage("Определение характеристики обновлено.");
    },
  });

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    mutation.reset();
    setValidationError(null);
    setSuccessMessage(null);

    if (name.trim().length === 0) {
      setValidationError("Название характеристики не может быть пустым.");

      return;
    }

    mutation.mutate();
  }

  function cancelEditing(): void {
    mutation.reset();
    setNameDraft(null);
    setUnitDraft(null);
    setValidationError(null);
    setSuccessMessage(null);
    setIsEditing(false);
  }

  return (
    <article className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
      <div className="flex flex-col justify-between gap-4 lg:flex-row lg:items-start">
        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-2">
            <h3 className="text-lg font-semibold text-white">
              {definition.name}
              {definition.unit ? `, ${definition.unit}` : ""}
            </h3>

            <span className="rounded-full border border-teal-500/30 bg-teal-500/10 px-3 py-1 text-xs font-medium text-teal-300">
              {formatDataType(definition.dataType)}
            </span>
          </div>

          <p className="mt-2 break-all font-mono text-sm text-slate-500">
            {definition.code}
          </p>
        </div>

        {!isEditing && (
          <button
            type="button"
            onClick={() => {
              setIsEditing(true);
              setSuccessMessage(null);
              mutation.reset();
            }}
            className="rounded-xl border border-white/10 bg-white/[0.05] px-4 py-2 text-sm font-medium text-slate-200 transition hover:bg-white/[0.1]"
          >
            Редактировать
          </button>
        )}
      </div>

      <div className="mt-5 grid gap-3 sm:grid-cols-2">
        <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
          <p className="text-xs text-slate-500">Используется типами товаров</p>

          <p className="mt-2 text-xl font-semibold text-white">
            {definition.productTypesCount}
          </p>
        </div>

        <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
          <p className="text-xs text-slate-500">Заполнено у товаров</p>

          <p className="mt-2 text-xl font-semibold text-white">
            {definition.productsWithValueCount}
          </p>
        </div>
      </div>

      {validationError && (
        <div className="mt-5 rounded-2xl border border-amber-500/30 bg-amber-500/10 p-4 text-sm text-amber-200">
          {validationError}
        </div>
      )}

      {mutation.isError && (
        <div className="mt-5 rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-200">
          {getErrorMessage(mutation.error)}
        </div>
      )}

      {successMessage && (
        <div className="mt-5 rounded-2xl border border-green-500/30 bg-green-500/10 p-4 text-sm text-green-200">
          {successMessage}
        </div>
      )}

      {isEditing && (
        <form
          onSubmit={handleSubmit}
          className="mt-5 rounded-2xl border border-white/10 bg-black/20 p-5"
        >
          <div className="grid gap-4 lg:grid-cols-2">
            <label className="grid gap-2">
              <span className="text-sm text-slate-300">Название</span>

              <input
                value={name}
                maxLength={200}
                onChange={(event) => {
                  setNameDraft(event.target.value);
                  setValidationError(null);
                  setSuccessMessage(null);
                }}
                className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none focus:border-teal-400"
              />
            </label>

            <label className="grid gap-2">
              <span className="text-sm text-slate-300">Единица измерения</span>

              <input
                value={unit}
                maxLength={50}
                onChange={(event) => {
                  setUnitDraft(event.target.value);
                  setValidationError(null);
                  setSuccessMessage(null);
                }}
                placeholder="Пустое значение удалит единицу"
                className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none placeholder:text-slate-600 focus:border-teal-400"
              />
            </label>
          </div>

          <div className="mt-4 rounded-2xl border border-white/10 bg-white/[0.03] p-4 text-sm text-slate-400">
            Код{" "}
            <span className="font-mono text-slate-200">{definition.code}</span>{" "}
            и тип данных{" "}
            <span className="text-slate-200">
              {formatDataType(definition.dataType)}
            </span>{" "}
            изменить нельзя.
          </div>

          <div className="mt-5 flex flex-wrap gap-3">
            <button
              type="submit"
              disabled={mutation.isPending || !hasChanges}
              className="rounded-xl bg-teal-500 px-4 py-2 text-sm font-medium text-white transition hover:bg-teal-400 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {mutation.isPending ? "Сохраняем..." : "Сохранить"}
            </button>

            <button
              type="button"
              disabled={mutation.isPending}
              onClick={cancelEditing}
              className="rounded-xl border border-white/10 bg-white/[0.04] px-4 py-2 text-sm font-medium text-slate-300 transition hover:bg-white/[0.08]"
            >
              Отмена
            </button>
          </div>
        </form>
      )}
    </article>
  );
}
