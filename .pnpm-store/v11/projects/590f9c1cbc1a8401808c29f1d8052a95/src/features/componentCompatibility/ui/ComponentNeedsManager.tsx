"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import type { FormEvent } from "react";
import { useState } from "react";
import {
  createComponentNeed,
  getComponentNeeds,
} from "../api/componentCompatibilityManagement";

function getErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error) && typeof error.response?.data === "string") {
    return error.response.data;
  }

  return "Не удалось сохранить потребность.";
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
  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const needsQuery = useQuery({
    queryKey,
    queryFn: () => getComponentNeeds(productTypeCode),
  });
  const createMutation = useMutation({
    mutationFn: () => createComponentNeed(productTypeCode, code, name),
    onSuccess: async () => {
      setCode("");
      setName("");
      await queryClient.invalidateQueries({ queryKey });
    },
  });

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    if (code.trim() && name.trim()) {
      createMutation.mutate();
    }
  }

  return (
    <section className="rounded-3xl border border-sky-500/20 bg-sky-500/[0.04] p-6">
      <h2 className="text-xl font-semibold text-white">
        Потребности комплектности
      </h2>

      <p className="mt-2 text-sm text-slate-400">
        Опишите, чего может не хватать товарам типа «{productTypeName}».
      </p>

      <form
        onSubmit={handleSubmit}
        className="mt-5 grid gap-3 lg:grid-cols-[1fr_1.5fr_auto]"
      >
        <input
          value={code}
          onChange={(event) => setCode(event.target.value)}
          placeholder="MOUNTING_PANEL"
          maxLength={100}
          className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 font-mono text-slate-100 outline-none focus:border-sky-400"
        />

        <input
          value={name}
          onChange={(event) => setName(event.target.value)}
          placeholder="Монтажная панель"
          maxLength={200}
          className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none focus:border-sky-400"
        />

        <button
          type="submit"
          disabled={createMutation.isPending || !code.trim() || !name.trim()}
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

      {needsQuery.isLoading ? (
        <p className="mt-5 text-sm text-slate-400">Загружаем потребности...</p>
      ) : (
        <div className="mt-5 flex flex-wrap gap-2">
          {(needsQuery.data ?? []).map((need) => (
            <span
              key={need.id}
              className="rounded-full border border-sky-500/30 bg-sky-500/10 px-3 py-2 text-sm text-sky-200"
            >
              {need.name} · <span className="font-mono">{need.code}</span>
            </span>
          ))}

          {needsQuery.data?.length === 0 && (
            <p className="text-sm text-slate-500">
              Потребности пока не настроены.
            </p>
          )}
        </div>
      )}
    </section>
  );
}
