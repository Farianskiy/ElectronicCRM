"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import type { FormEvent } from "react";
import { useState } from "react";
import {
  createComponentNeed,
  getComponentNeeds,
  updateComponentNeed,
} from "../api/componentCompatibilityManagement";
import type {
  ComponentNeed,
  ComponentNeedRequirementMode,
} from "../model/types";

function getErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error) && typeof error.response?.data === "string") {
    return error.response.data;
  }

  return "Не удалось сохранить группу комплектующих.";
}

function ComponentNeedSettings({
  need,
  isSaving,
  onSave,
}: {
  need: ComponentNeed;
  isSaving: boolean;
  onSave: (
    needDefinitionId: string,
    requirementMode: ComponentNeedRequirementMode,
    defaultQuantityPerUnit: number,
  ) => void;
}) {
  const [requirementMode, setRequirementMode] =
    useState<ComponentNeedRequirementMode>(need.requirementMode);
  const [defaultQuantityPerUnit, setDefaultQuantityPerUnit] = useState(
    String(need.defaultQuantityPerUnit),
  );
  const quantity = Number(defaultQuantityPerUnit);
  const isQuantityValid =
    Number.isInteger(quantity) && quantity >= 1 && quantity <= 1000000;
  const isChanged =
    requirementMode !== need.requirementMode ||
    quantity !== need.defaultQuantityPerUnit;

  return (
    <article className="rounded-2xl border border-sky-500/20 bg-black/20 p-4">
      <div>
        <p className="font-medium text-slate-100">{need.name}</p>
      </div>

      <div className="mt-3 grid gap-3 md:grid-cols-[1fr_0.55fr_auto]">
        <select
          value={requirementMode}
          onChange={(event) =>
            setRequirementMode(
              event.target.value as ComponentNeedRequirementMode,
            )
          }
          className="rounded-xl border border-white/10 bg-black/30 px-3 py-2 text-sm text-slate-100 outline-none focus:border-sky-400"
        >
          <option value="Optional">Дополнительное, покупается по выбору</option>
          <option value="RequiredForAssembly">
            Обязательно для сборки или монтажа
          </option>
        </select>

        <label className="grid grid-cols-[1fr_auto] items-center gap-2 rounded-xl border border-white/10 bg-black/30 px-3">
          <input
            type="number"
            min={1}
            max={1000000}
            step={1}
            value={defaultQuantityPerUnit}
            onChange={(event) =>
              setDefaultQuantityPerUnit(event.target.value)
            }
            aria-label={`Количество для группы ${need.name}`}
            className="min-w-0 bg-transparent py-2 text-sm text-slate-100 outline-none"
          />
          <span className="text-xs text-slate-500">шт./ед.</span>
        </label>

        <button
          type="button"
          disabled={isSaving || !isChanged || !isQuantityValid}
          onClick={() => onSave(need.id, requirementMode, quantity)}
          className="rounded-xl border border-sky-400/30 px-4 py-2 text-sm text-sky-200 transition hover:bg-sky-500/10 disabled:opacity-40"
        >
          Сохранить
        </button>
      </div>
    </article>
  );
}

export function ComponentNeedsManager({
  productTypeCode,
  productTypeName,
}: {
  productTypeCode: string;
  productTypeName: string;
}) {
  const queryClient = useQueryClient();
  const queryKey = ["component-needs", productTypeCode];
  const [name, setName] = useState("");
  const [requirementMode, setRequirementMode] =
    useState<ComponentNeedRequirementMode>("Optional");
  const [defaultQuantityPerUnit, setDefaultQuantityPerUnit] = useState("1");
  const needsQuery = useQuery({
    queryKey,
    queryFn: () => getComponentNeeds(productTypeCode),
  });
  const createMutation = useMutation({
    mutationFn: () =>
      createComponentNeed(
        productTypeCode,
        name,
        requirementMode,
        Number(defaultQuantityPerUnit),
      ),
    onSuccess: async () => {
      setName("");
      setRequirementMode("Optional");
      setDefaultQuantityPerUnit("1");
      await queryClient.invalidateQueries({ queryKey });
    },
  });
  const updateMutation = useMutation({
    mutationFn: ({
      needDefinitionId,
      nextRequirementMode,
      nextDefaultQuantityPerUnit,
    }: {
      needDefinitionId: string;
      nextRequirementMode: ComponentNeedRequirementMode;
      nextDefaultQuantityPerUnit: number;
    }) =>
      updateComponentNeed(
        needDefinitionId,
        nextRequirementMode,
        nextDefaultQuantityPerUnit,
      ),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey });
    },
  });

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    if (name.trim()) {
      createMutation.mutate();
    }
  }

  return (
    <section className="rounded-3xl border border-sky-500/20 bg-sky-500/[0.04] p-6">
      <h2 className="text-xl font-semibold text-white">
        Группы комплектующих
      </h2>

      <p className="mt-2 text-sm text-slate-400">
        Создайте группы комплектующих для товаров типа «{productTypeName}» и
        укажите, обязательны ли они для монтажа.
      </p>

      <form
        onSubmit={handleSubmit}
        className="mt-5 grid gap-3 lg:grid-cols-[1.4fr_1fr_0.6fr_auto]"
      >
        <input
          value={name}
          onChange={(event) => setName(event.target.value)}
          placeholder="Монтажная панель"
          maxLength={200}
          className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none focus:border-sky-400"
        />

        <select
          value={requirementMode}
          onChange={(event) =>
            setRequirementMode(
              event.target.value as ComponentNeedRequirementMode,
            )
          }
          className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none focus:border-sky-400"
        >
          <option value="Optional">Необязательное</option>
          <option value="RequiredForAssembly">Требуется для монтажа</option>
        </select>

        <input
          type="number"
          min={1}
          max={1000000}
          step={1}
          value={defaultQuantityPerUnit}
          onChange={(event) => setDefaultQuantityPerUnit(event.target.value)}
          aria-label="Количество комплектующих на единицу товара по умолчанию"
          className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none focus:border-sky-400"
        />

        <button
          type="submit"
          disabled={
            createMutation.isPending ||
            !name.trim() ||
            !Number.isInteger(Number(defaultQuantityPerUnit)) ||
            Number(defaultQuantityPerUnit) < 1 ||
            Number(defaultQuantityPerUnit) > 1000000
          }
          className="rounded-2xl bg-sky-500 px-5 py-3 text-sm font-medium text-white transition hover:bg-sky-400 disabled:opacity-50"
        >
          Добавить
        </button>
      </form>

      {createMutation.isError && (
        <p className="mt-3 text-sm text-red-300">
          {getErrorMessage(createMutation.error)}
        </p>
      )}

      {updateMutation.isError && (
        <p className="mt-3 text-sm text-red-300">
          {getErrorMessage(updateMutation.error)}
        </p>
      )}

      {needsQuery.isLoading ? (
        <p className="mt-5 text-sm text-slate-400">Загружаем группы...</p>
      ) : (
        <div className="mt-5 grid gap-3">
          {(needsQuery.data ?? []).map((need) => (
            <ComponentNeedSettings
              key={need.id}
              need={need}
              isSaving={
                updateMutation.isPending &&
                updateMutation.variables?.needDefinitionId === need.id
              }
              onSave={(
                needDefinitionId,
                nextRequirementMode,
                nextDefaultQuantityPerUnit,
              ) =>
                updateMutation.mutate({
                  needDefinitionId,
                  nextRequirementMode,
                  nextDefaultQuantityPerUnit,
                })
              }
            />
          ))}

          {needsQuery.data?.length === 0 && (
            <p className="text-sm text-slate-500">
              Группы пока не настроены.
            </p>
          )}
        </div>
      )}
    </section>
  );
}
