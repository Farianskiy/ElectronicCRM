"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import type { FormEvent } from "react";
import { useState } from "react";
import { addOptionalCharacteristicToProductType } from "../api/addOptionalCharacteristicToProductType";
import { getAvailableCharacteristicDefinitions } from "../api/getAvailableCharacteristicDefinitions";
import type { AvailableCharacteristicDefinition } from "../model/types";
import { AppSelect } from "@/shared/ui/AppSelect";

interface AddOptionalCharacteristicToProductTypeProps {
  productTypeCode: string;
  productTypeName: string;
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

  return "Не удалось добавить характеристику к типу.";
}

function formatDefinitionLabel(
  definition: AvailableCharacteristicDefinition,
): string {
  const unit = definition.unit ? `, ${definition.unit}` : "";

  return `${definition.name}${unit} — ${definition.code}`;
}

export function AddOptionalCharacteristicToProductType({
  productTypeCode,
  productTypeName,
}: AddOptionalCharacteristicToProductTypeProps) {
  const queryClient = useQueryClient();

  const [searchDraft, setSearchDraft] = useState("");

  const [appliedSearch, setAppliedSearch] = useState("");

  const [selectedDefinitionId, setSelectedDefinitionId] = useState("");

  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const availableDefinitionsQuery = useQuery({
    queryKey: [
      "catalog-product-type-available-definitions",
      productTypeCode,
      appliedSearch,
    ],

    queryFn: () =>
      getAvailableCharacteristicDefinitions(productTypeCode, appliedSearch),

    enabled: productTypeCode.length > 0,
  });

  const availableDefinitions = availableDefinitionsQuery.data ?? [];

  const selectedDefinition =
    availableDefinitions.find(
      (definition) => definition.id === selectedDefinitionId,
    ) ?? null;

  const addMutation = useMutation({
    mutationFn: async (definition: AvailableCharacteristicDefinition) => {
      await addOptionalCharacteristicToProductType(productTypeCode, {
        characteristicDefinitionId: definition.id,
      });
    },

    onSuccess: async (_data, definition) => {
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: [
            "catalog-product-type-characteristic-schema",
            productTypeCode,
          ],
        }),

        queryClient.invalidateQueries({
          queryKey: [
            "catalog-product-type-available-definitions",
            productTypeCode,
          ],
        }),

        /*
         * Этот query используется редактором товара
         * и фильтрами каталога.
         */
        queryClient.invalidateQueries({
          queryKey: ["catalog-product-type-characteristics", productTypeCode],
        }),
      ]);

      setSelectedDefinitionId("");

      setSuccessMessage(
        `Характеристика «${definition.name}» ` +
          `добавлена к типу «${productTypeName}».`,
      );
    },
  });

  function handleSearchSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    setAppliedSearch(searchDraft.trim());
    setSelectedDefinitionId("");
    setSuccessMessage(null);
    addMutation.reset();
  }

  function handleAdd(): void {
    if (!selectedDefinition) {
      return;
    }

    setSuccessMessage(null);
    addMutation.reset();
    addMutation.mutate(selectedDefinition);
  }

  return (
    <section className="rounded-3xl border border-teal-500/20 bg-teal-500/[0.04] p-6">
      <div>
        <h2 className="text-xl font-semibold text-white">
          Добавить характеристику к типу
        </h2>

        <p className="mt-2 text-sm text-slate-400">
          Выберите существующее определение характеристики. Оно будет добавлено
          как необязательное и пока не будет участвовать в фильтрации или
          подборе аналогов.
        </p>
      </div>

      <div className="mt-5 rounded-2xl border border-amber-500/20 bg-amber-500/[0.06] p-4">
        <p className="text-sm font-medium text-amber-200">
          Безопасные настройки
        </p>

        <div className="mt-2 flex flex-wrap gap-2 text-xs">
          <span className="rounded-full bg-white/[0.05] px-3 py-1 text-slate-300">
            Необязательная
          </span>

          <span className="rounded-full bg-white/[0.05] px-3 py-1 text-slate-300">
            Не участвует в фильтрах
          </span>

          <span className="rounded-full bg-white/[0.05] px-3 py-1 text-slate-300">
            Не участвует в аналогах
          </span>
        </div>
      </div>

      <form
        onSubmit={handleSearchSubmit}
        className="mt-5 flex flex-col gap-3 sm:flex-row"
      >
        <input
          value={searchDraft}
          onChange={(event) => setSearchDraft(event.target.value)}
          placeholder="Поиск по названию или коду"
          className="min-w-0 flex-1 rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none placeholder:text-slate-600 focus:border-teal-400 focus:ring-2 focus:ring-teal-400/20"
        />

        <button
          type="submit"
          disabled={availableDefinitionsQuery.isFetching}
          className="rounded-2xl border border-white/10 bg-white/[0.06] px-5 py-3 text-sm font-medium text-slate-200 transition hover:bg-white/[0.1] disabled:opacity-50"
        >
          Найти
        </button>

        {(searchDraft.length > 0 || appliedSearch.length > 0) && (
          <button
            type="button"
            onClick={() => {
              setSearchDraft("");
              setAppliedSearch("");
              setSelectedDefinitionId("");
              setSuccessMessage(null);
              addMutation.reset();
            }}
            className="rounded-2xl border border-white/10 bg-white/[0.03] px-5 py-3 text-sm font-medium text-slate-400 transition hover:bg-white/[0.08]"
          >
            Сбросить
          </button>
        )}
      </form>

      {availableDefinitionsQuery.isError && (
        <div className="mt-5 rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-200">
          {getErrorMessage(availableDefinitionsQuery.error)}
        </div>
      )}

      {addMutation.isError && (
        <div className="mt-5 rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-200">
          {getErrorMessage(addMutation.error)}
        </div>
      )}

      {successMessage && (
        <div className="mt-5 rounded-2xl border border-green-500/30 bg-green-500/10 p-4 text-sm text-green-200">
          {successMessage}
        </div>
      )}

      {availableDefinitionsQuery.isLoading ? (
        <p className="mt-5 text-sm text-slate-400">
          Загружаем доступные характеристики...
        </p>
      ) : availableDefinitions.length === 0 ? (
        <div className="mt-5 rounded-2xl border border-white/10 bg-black/20 p-5">
          <p className="font-medium text-white">Доступных характеристик нет</p>

          <p className="mt-2 text-sm text-slate-400">
            Все существующие определения уже подключены к выбранному типу либо
            ничего не найдено по указанному запросу.
          </p>
        </div>
      ) : (
        <div className="mt-5 grid gap-4 lg:grid-cols-[1fr_auto] lg:items-end">
          <div className="grid gap-2">
            <span className="text-sm font-medium text-slate-300">
              Характеристика
            </span>

            <AppSelect
              ariaLabel="Доступная характеристика"
              value={selectedDefinitionId}
              disabled={
                availableDefinitionsQuery.isFetching || addMutation.isPending
              }
              onChange={(value) => {
                setSelectedDefinitionId(value);
                setSuccessMessage(null);
                addMutation.reset();
              }}
              options={[
                {
                  value: "",
                  label: "Выберите характеристику",
                },

                ...availableDefinitions.map((definition) => ({
                  value: definition.id,
                  label: formatDefinitionLabel(definition),
                })),
              ]}
            />
          </div>

          <button
            type="button"
            disabled={!selectedDefinition || addMutation.isPending}
            onClick={handleAdd}
            className="rounded-2xl bg-teal-500 px-5 py-3 text-sm font-medium text-white transition hover:bg-teal-400 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {addMutation.isPending ? "Добавляем..." : "Добавить к типу"}
          </button>
        </div>
      )}
    </section>
  );
}
