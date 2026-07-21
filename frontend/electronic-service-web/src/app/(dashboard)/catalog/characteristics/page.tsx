"use client";

import { useQuery } from "@tanstack/react-query";
import axios from "axios";
import type { FormEvent } from "react";
import { useState } from "react";
import { RequireTechnicalUser } from "@/features/auth/ui/RequireTechnicalUser";
import { getCatalogCharacteristicDefinitions } from "@/features/catalogCharacteristicDefinitions/api/getCatalogCharacteristicDefinitions";
import { CharacteristicDefinitionEditorCard } from "@/features/catalogCharacteristicDefinitions/ui/CharacteristicDefinitionEditorCard";
import { CreateCharacteristicDefinitionForm } from "@/features/catalogCharacteristicDefinitions/ui/CreateCharacteristicDefinitionForm";
import { PageHeader } from "@/shared/ui/PageHeader";

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

  return "Не удалось загрузить характеристики.";
}

export default function CatalogCharacteristicsPage() {
  return (
    <RequireTechnicalUser>
      <CatalogCharacteristicsContent />
    </RequireTechnicalUser>
  );
}

function CatalogCharacteristicsContent() {
  const [searchDraft, setSearchDraft] = useState("");

  const [appliedSearch, setAppliedSearch] = useState("");

  const definitionsQuery = useQuery({
    queryKey: ["catalog-characteristic-definitions", appliedSearch],

    queryFn: () => getCatalogCharacteristicDefinitions(appliedSearch),
  });

  const definitions = definitionsQuery.data ?? [];

  function handleSearchSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    setAppliedSearch(searchDraft.trim());
  }

  return (
    <div className="grid gap-6">
      <PageHeader
        title="Характеристики каталога"
        description="Создание и безопасное редактирование глобальных определений характеристик."
      />

      <CreateCharacteristicDefinitionForm />

      <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
        <div>
          <h2 className="text-xl font-semibold text-white">
            Все характеристики
          </h2>

          <p className="mt-2 text-sm text-slate-400">
            Поиск выполняется по названию и стабильному коду характеристики.
          </p>
        </div>

        <form
          onSubmit={handleSearchSubmit}
          className="mt-5 flex flex-col gap-3 sm:flex-row"
        >
          <input
            value={searchDraft}
            onChange={(event) => setSearchDraft(event.target.value)}
            placeholder="Например: номинальный ток или RATED_CURRENT"
            className="min-w-0 flex-1 rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none placeholder:text-slate-600 focus:border-teal-400"
          />

          <button
            type="submit"
            disabled={definitionsQuery.isFetching}
            className="rounded-2xl bg-teal-500 px-5 py-3 text-sm font-medium text-white transition hover:bg-teal-400 disabled:opacity-50"
          >
            Найти
          </button>

          {(searchDraft.length > 0 || appliedSearch.length > 0) && (
            <button
              type="button"
              onClick={() => {
                setSearchDraft("");
                setAppliedSearch("");
              }}
              className="rounded-2xl border border-white/10 bg-white/[0.04] px-5 py-3 text-sm font-medium text-slate-300 transition hover:bg-white/[0.08]"
            >
              Сбросить
            </button>
          )}
        </form>
      </section>

      {definitionsQuery.isError && (
        <section className="rounded-3xl border border-red-500/30 bg-red-500/10 p-6 text-red-200">
          {getErrorMessage(definitionsQuery.error)}
        </section>
      )}

      {definitionsQuery.isLoading ? (
        <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6 text-slate-300">
          Загружаем характеристики...
        </section>
      ) : definitions.length === 0 ? (
        <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
          <h2 className="text-xl font-semibold text-white">
            Характеристики не найдены
          </h2>

          <p className="mt-2 text-sm text-slate-400">
            Измените поисковый запрос или создайте новое определение.
          </p>
        </section>
      ) : (
        <section className="grid gap-4">
          {definitions.map((definition) => (
            <CharacteristicDefinitionEditorCard
              key={definition.id}
              definition={definition}
            />
          ))}
        </section>
      )}
    </div>
  );
}
