"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import type { FormEvent } from "react";
import { useState } from "react";
import { getCatalogManufacturers } from "@/features/catalogMetadata/api/getCatalogManufacturers";
import { AppSelect } from "@/shared/ui/AppSelect";
import { AppButton } from "@/shared/ui/AppButton";
import { AppInput } from "@/shared/ui/AppInput";
import { updateCatalogProductGeneralInformation } from "../api/updateCatalogProductGeneralInformation";
import type { CatalogProductDetails } from "../model/types";
import { catalogProductAuditHistoryQueryKey } from "@/features/catalogProductAuditHistory/model/queryKeys";

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

        queryClient.invalidateQueries({
          queryKey: catalogProductAuditHistoryQueryKey(product.id),
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
      className="min-w-0 rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel-strong)] p-5"
    >
      <div>
        <h3 className="text-base font-semibold text-[var(--app-text)]">
          Основная информация
        </h3>

        <p className="mt-2 text-sm leading-6 text-[var(--app-muted)]">
          Здесь изменяются название, артикул и производитель. Тип товара
          изменяется отдельной безопасной операцией.
        </p>
      </div>

      {manufacturersQuery.isError && (
        <div
          role="alert"
          className="mt-5 whitespace-pre-wrap rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm leading-6 text-[var(--app-danger)] [overflow-wrap:anywhere]"
        >
          {getErrorMessage(manufacturersQuery.error)}
        </div>
      )}

      {validationError && (
        <div
          role="alert"
          className="mt-5 rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm leading-6 text-[var(--app-danger)] [overflow-wrap:anywhere]"
        >
          {validationError}
        </div>
      )}

      {mutation.isError && (
        <div
          role="alert"
          className="mt-5 whitespace-pre-wrap rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm leading-6 text-[var(--app-danger)] [overflow-wrap:anywhere]"
        >
          {getErrorMessage(mutation.error)}
        </div>
      )}

      {successMessage && (
        <div
          role="status"
          className="mt-5 rounded-2xl border border-[var(--app-success-border)] bg-[var(--app-success-soft)] p-4 text-sm leading-6 text-[var(--app-success)] [overflow-wrap:anywhere]"
        >
          {successMessage}
        </div>
      )}

      <div className="mt-5 grid min-w-0 gap-4 lg:grid-cols-2">
        <label className="grid min-w-0 content-start gap-2 lg:col-span-2">
          <span className="text-sm font-medium text-[var(--app-text)]">
            Название товара
          </span>

          <AppInput
            value={name}
            maxLength={500}
            required
            onChange={(event) => {
              setNameDraft(event.target.value);
              setValidationError(null);
              setSuccessMessage(null);
            }}
          />
        </label>

        <label className="grid min-w-0 content-start gap-2">
          <span className="text-sm font-medium text-[var(--app-text)]">
            Артикул
          </span>

          <AppInput
            value={article}
            maxLength={100}
            required
            onChange={(event) => {
              setArticleDraft(event.target.value);
              setValidationError(null);
              setSuccessMessage(null);
            }}
          />
        </label>

        <div className="grid min-w-0 content-start gap-2">
          <span className="text-sm font-medium text-[var(--app-text)]">
            Производитель
          </span>

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

      <div className="mt-5 flex flex-col gap-3 sm:flex-row sm:flex-wrap">
        <AppButton
          type="submit"
          variant="primary"
          loading={mutation.isPending}
          disabled={manufacturersQuery.isError}
          className="w-full sm:w-auto"
        >
          {mutation.isPending ? "Сохраняем..." : "Сохранить информацию"}
        </AppButton>

        {(nameDraft !== null ||
          articleDraft !== null ||
          manufacturerIdDraft !== null) && (
          <AppButton
            type="button"
            variant="secondary"
            disabled={mutation.isPending}
            onClick={() => {
              setNameDraft(null);
              setArticleDraft(null);
              setManufacturerIdDraft(null);
              setValidationError(null);
              setSuccessMessage(null);
              mutation.reset();
            }}
            className="w-full sm:w-auto"
          >
            Отменить изменения
          </AppButton>
        )}
      </div>
    </form>
  );
}
