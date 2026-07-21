"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import type { FormEvent } from "react";
import { useState } from "react";
import { getCatalogManufacturers } from "@/features/catalogMetadata/api/getCatalogManufacturers";
import { AppSelect } from "@/shared/ui/AppSelect";
import { updateCatalogProductGeneralInformation } from "../api/updateCatalogProductGeneralInformation";
import type { CatalogProductDetails } from "../model/types";

interface TechnicalProductGeneralInformationEditorProps {
  product: CatalogProductDetails;
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

  return "Не удалось изменить общую информацию.";
}

export function TechnicalProductGeneralInformationEditor({
  product,
}: TechnicalProductGeneralInformationEditorProps) {
  const queryClient = useQueryClient();

  // В state храним только несохранённый черновик.
  // null означает: использовать актуальное значение product.
  const [nameDraft, setNameDraft] = useState<string | null>(null);

  const [articleDraft, setArticleDraft] = useState<string | null>(null);

  const [manufacturerIdDraft, setManufacturerIdDraft] = useState<string | null>(
    null,
  );

  const [validationError, setValidationError] = useState<string | null>(null);

  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const name = nameDraft ?? product.name;
  const article = articleDraft ?? product.article;

  const manufacturerId = manufacturerIdDraft ?? product.manufacturerId;

  const manufacturersQuery = useQuery({
    queryKey: ["catalog-manufacturers"],
    queryFn: getCatalogManufacturers,
    staleTime: 5 * 60 * 1000,
  });

  /*
   * Текущий производитель добавляется первым.
   * Благодаря этому AppSelect корректно показывает его
   * даже во время загрузки metadata.
   */
  const manufacturerOptions = [
    {
      value: product.manufacturerId,
      label: product.manufacturerName,
    },

    ...(manufacturersQuery.data ?? [])
      .filter((manufacturer) => manufacturer.id !== product.manufacturerId)
      .map((manufacturer) => ({
        value: manufacturer.id,
        label: manufacturer.name,
      })),
  ];

  const mutation = useMutation({
    mutationFn: async (request: {
      name: string;
      article: string;
      manufacturerId: string;
    }) => {
      await updateCatalogProductGeneralInformation(product.id, request);
    },

    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: ["catalog-product-details", product.id],
        }),

        queryClient.invalidateQueries({
          queryKey: ["catalog-products"],
        }),
      ]);

      setNameDraft(null);
      setArticleDraft(null);
      setManufacturerIdDraft(null);
      setValidationError(null);
      setSuccessMessage("Общая информация успешно обновлена.");
    },
  });

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    mutation.reset();
    setValidationError(null);
    setSuccessMessage(null);

    const normalizedName = name.trim();
    const normalizedArticle = article.trim();

    if (normalizedName.length === 0) {
      setValidationError("Название товара не может быть пустым.");

      return;
    }

    if (normalizedArticle.length === 0) {
      setValidationError("Артикул товара не может быть пустым.");

      return;
    }

    if (manufacturerId.length === 0) {
      setValidationError("Выберите производителя товара.");

      return;
    }

    mutation.mutate({
      name: normalizedName,
      article: normalizedArticle,
      manufacturerId,
    });
  }

  return (
    <form
      onSubmit={handleSubmit}
      className="rounded-2xl border border-white/10 bg-black/20 p-5"
    >
      <div>
        <h3 className="font-semibold text-white">Основная информация</h3>

        <p className="mt-2 text-sm text-slate-400">
          Здесь изменяются название, артикул и производитель. Тип товара
          изменяется отдельной безопасной операцией.
        </p>
      </div>

      {manufacturersQuery.isError && (
        <div className="mt-5 rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-200">
          {getErrorMessage(manufacturersQuery.error)}
        </div>
      )}

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
        <label className="grid gap-2 lg:col-span-2">
          <span className="text-sm text-slate-300">Название товара</span>

          <input
            value={name}
            maxLength={500}
            required
            onChange={(event) => {
              setNameDraft(event.target.value);
              setValidationError(null);
              setSuccessMessage(null);
            }}
            className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none focus:border-teal-400 focus:ring-2 focus:ring-teal-400/20"
          />
        </label>

        <label className="grid gap-2">
          <span className="text-sm text-slate-300">Артикул</span>

          <input
            value={article}
            maxLength={100}
            required
            onChange={(event) => {
              setArticleDraft(event.target.value);
              setValidationError(null);
              setSuccessMessage(null);
            }}
            className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none focus:border-teal-400 focus:ring-2 focus:ring-teal-400/20"
          />
        </label>

        <div className="grid gap-2">
          <span className="text-sm text-slate-300">Производитель</span>

          <AppSelect
            ariaLabel="Производитель товара"
            value={manufacturerId}
            disabled={
              manufacturersQuery.isLoading || manufacturersQuery.isError
            }
            onChange={(value) => {
              setManufacturerIdDraft(value);
              setValidationError(null);
              setSuccessMessage(null);
            }}
            options={manufacturerOptions}
          />
        </div>
      </div>

      <div className="mt-5 flex flex-wrap gap-3">
        <button
          type="submit"
          disabled={mutation.isPending || manufacturersQuery.isError}
          className="rounded-2xl bg-teal-500 px-5 py-3 text-sm font-medium text-white transition hover:bg-teal-400 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {mutation.isPending ? "Сохраняем..." : "Сохранить информацию"}
        </button>

        {(nameDraft !== null ||
          articleDraft !== null ||
          manufacturerIdDraft !== null) && (
          <button
            type="button"
            disabled={mutation.isPending}
            onClick={() => {
              setNameDraft(null);
              setArticleDraft(null);
              setManufacturerIdDraft(null);
              setValidationError(null);
              setSuccessMessage(null);
              mutation.reset();
            }}
            className="rounded-2xl border border-white/10 bg-white/[0.04] px-5 py-3 text-sm font-medium text-slate-300 transition hover:bg-white/[0.08]"
          >
            Отменить изменения
          </button>
        )}
      </div>
    </form>
  );
}
