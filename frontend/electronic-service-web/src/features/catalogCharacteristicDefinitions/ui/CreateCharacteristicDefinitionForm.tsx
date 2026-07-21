"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import type { FormEvent } from "react";
import { useState } from "react";
import { AppSelect } from "@/shared/ui/AppSelect";
import { createCharacteristicDefinition } from "../api/createCharacteristicDefinition";
import type { CatalogCharacteristicDataType } from "../model/types";

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

  return "Не удалось создать характеристику.";
}

export function CreateCharacteristicDefinitionForm() {
  const queryClient = useQueryClient();

  const [code, setCode] = useState("");
  const [name, setName] = useState("");

  const [dataType, setDataType] =
    useState<CatalogCharacteristicDataType>("Text");

  const [unit, setUnit] = useState("");

  const [validationError, setValidationError] = useState<string | null>(null);

  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const mutation = useMutation({
    mutationFn: createCharacteristicDefinition,

    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: ["catalog-characteristic-definitions"],
        }),

        /*
         * Новая definition должна появиться
         * в формах добавления характеристики
         * к ProductType.
         */
        queryClient.invalidateQueries({
          queryKey: ["catalog-product-type-available-definitions"],
        }),
      ]);

      setCode("");
      setName("");
      setDataType("Text");
      setUnit("");
      setValidationError(null);

      setSuccessMessage("Определение характеристики создано.");
    },
  });

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    mutation.reset();
    setValidationError(null);
    setSuccessMessage(null);

    const normalizedCode = code.trim();
    const normalizedName = name.trim();

    if (normalizedCode.length === 0) {
      setValidationError("Введите код характеристики.");

      return;
    }

    if (normalizedName.length === 0) {
      setValidationError("Введите название характеристики.");

      return;
    }

    mutation.mutate({
      code: normalizedCode,
      name: normalizedName,
      dataType,
      unit: unit.trim().length ? unit.trim() : null,
    });
  }

  return (
    <form
      onSubmit={handleSubmit}
      className="rounded-3xl border border-teal-500/20 bg-teal-500/[0.04] p-6"
    >
      <div>
        <h2 className="text-xl font-semibold text-white">
          Создать характеристику
        </h2>

        <p className="mt-2 text-sm text-slate-400">
          Код и тип данных после создания становятся неизменяемыми. Название и
          единицу измерения можно будет отредактировать.
        </p>
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

      <div className="mt-5 grid gap-4 lg:grid-cols-2">
        <label className="grid gap-2">
          <span className="text-sm text-slate-300">Код</span>

          <input
            value={code}
            maxLength={100}
            onChange={(event) => {
              setCode(event.target.value);
              setValidationError(null);
              setSuccessMessage(null);
            }}
            placeholder="INSTALLATION_METHOD"
            className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 font-mono text-slate-100 outline-none placeholder:text-slate-600 focus:border-teal-400"
          />

          <span className="text-xs text-slate-500">
            Пробелы и дефисы backend преобразует в символ подчёркивания.
          </span>
        </label>

        <label className="grid gap-2">
          <span className="text-sm text-slate-300">Название</span>

          <input
            value={name}
            maxLength={200}
            onChange={(event) => {
              setName(event.target.value);
              setValidationError(null);
              setSuccessMessage(null);
            }}
            placeholder="Способ монтажа"
            className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none placeholder:text-slate-600 focus:border-teal-400"
          />
        </label>

        <div className="grid gap-2">
          <span className="text-sm text-slate-300">Тип данных</span>

          <AppSelect
            ariaLabel="Тип данных характеристики"
            value={dataType}
            onChange={(value) => {
              setDataType(value as CatalogCharacteristicDataType);
              setValidationError(null);
              setSuccessMessage(null);
            }}
            options={[
              {
                value: "Text",
                label: "Текст",
              },
              {
                value: "Number",
                label: "Число",
              },
              {
                value: "Boolean",
                label: "Да / Нет",
              },
            ]}
          />
        </div>

        <label className="grid gap-2">
          <span className="text-sm text-slate-300">Единица измерения</span>

          <input
            value={unit}
            maxLength={50}
            onChange={(event) => {
              setUnit(event.target.value);
              setValidationError(null);
              setSuccessMessage(null);
            }}
            placeholder="A, V, мм или пусто"
            className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none placeholder:text-slate-600 focus:border-teal-400"
          />
        </label>
      </div>

      <button
        type="submit"
        disabled={mutation.isPending}
        className="mt-5 rounded-2xl bg-teal-500 px-5 py-3 text-sm font-medium text-white transition hover:bg-teal-400 disabled:cursor-not-allowed disabled:opacity-50"
      >
        {mutation.isPending ? "Создаём..." : "Создать характеристику"}
      </button>
    </form>
  );
}
