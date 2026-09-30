"use client";

import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { getCatalogProductTypeCharacteristicSchema } from "@/features/catalogProductTypes/api/getCatalogProductTypeCharacteristicSchema";
import type { CatalogProductTypeCharacteristicSchemaItem } from "@/features/catalogProductTypes/model/types";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { getRecognitionStrategyDescriptor } from "../model/recognitionProfileFormSupport";
import type { CatalogProductNameRecognitionPreview } from "../model/types";
import { CreateRecognitionProfileForm } from "./CreateRecognitionProfileForm";
import { RecognitionProfileEditor } from "./RecognitionProfileEditor";

interface RecognitionProfileManagementPanelProps {
  preview: CatalogProductNameRecognitionPreview;
  isRecognitionRefreshing: boolean;
  onRecognitionRefresh: () => Promise<void>;
}

export function RecognitionProfileManagementPanel({
  preview,
  isRecognitionRefreshing,
  onRecognitionRefresh,
}: RecognitionProfileManagementPanelProps) {
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const [refreshError, setRefreshError] = useState<string | null>(null);

  const productTypeCode = preview.productTypeCode ?? "";

  const schemaQuery = useQuery({
    queryKey: ["catalog-product-type-characteristic-schema", productTypeCode],

    queryFn: () => getCatalogProductTypeCharacteristicSchema(productTypeCode),

    enabled: preview.hasProductTypeScope && productTypeCode.length > 0,
    staleTime: 60 * 1000,
  });

  if (!preview.hasProductTypeScope || !preview.productTypeCode) {
    return null;
  }

  const schema = schemaQuery.data;

  const supportedCharacteristics =
    schema?.characteristics.filter(
      (characteristic) =>
        getRecognitionStrategyDescriptor(characteristic.code) !== null,
    ) ?? [];

  const availableCharacteristics = supportedCharacteristics.filter(
    (characteristic) => !profileAlreadyExists(characteristic, preview),
  );

  const unsupportedCharacteristicsCount =
    (schema?.characteristics.length ?? 0) - supportedCharacteristics.length;

  async function handleCompleted(message: string): Promise<void> {
    setSuccessMessage(message);
    setRefreshError(null);

    try {
      await onRecognitionRefresh();
    } catch (error) {
      setRefreshError(
        getApiErrorMessage(
          error,
          "Изменение сохранено, но повторный запуск Preview завершился ошибкой.",
        ),
      );
    }
  }

  return (
    <section className="grid gap-5 rounded-3xl border border-violet-500/20 bg-violet-500/[0.04] p-6">
      <div className="flex flex-col justify-between gap-5 lg:flex-row lg:items-start">
        <div>
          <h2 className="text-xl font-semibold text-white">
            Управление профилями
          </h2>

          <p className="mt-2 max-w-3xl text-sm text-slate-400">
            Создание и изменение технической политики распознавания для типа
            товара
            <span className="mx-1 font-mono text-violet-200">
              {preview.productTypeCode}
            </span>
            .
          </p>
        </div>

        <button
          type="button"
          disabled={
            schemaQuery.isLoading ||
            schemaQuery.isError ||
            availableCharacteristics.length === 0
          }
          onClick={() => {
            setShowCreateForm((currentValue) => !currentValue);
            setSuccessMessage(null);
            setRefreshError(null);
          }}
          className="rounded-xl bg-violet-500 px-5 py-3 text-sm font-medium text-white transition hover:bg-violet-400 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {showCreateForm ? "Закрыть форму" : "Создать профиль"}
        </button>
      </div>

      <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        <ManagementMetric
          label="Характеристик в схеме"
          value={schema?.characteristics.length.toString() ?? "—"}
        />

        <ManagementMetric
          label="Поддержано стратегиями"
          value={supportedCharacteristics.length.toString()}
        />

        <ManagementMetric
          label="Можно создать"
          value={availableCharacteristics.length.toString()}
        />

        <ManagementMetric
          label="Пока не поддержано"
          value={unsupportedCharacteristicsCount.toString()}
        />
      </div>

      {schemaQuery.isLoading && (
        <div className="rounded-2xl border border-white/10 bg-black/20 p-5 text-sm text-slate-300">
          Загружаем схему характеристик выбранного типа...
        </div>
      )}

      {schemaQuery.isError && (
        <div className="rounded-2xl border border-red-500/30 bg-red-500/10 p-5 text-sm text-red-200">
          {getApiErrorMessage(
            schemaQuery.error,
            "Не удалось загрузить схему типа товара.",
          )}
        </div>
      )}

      {successMessage && (
        <div className="rounded-2xl border border-green-500/30 bg-green-500/10 p-5 text-sm text-green-200">
          {successMessage}
        </div>
      )}

      {refreshError && (
        <div className="rounded-2xl border border-amber-500/30 bg-amber-500/10 p-5 text-sm text-amber-200">
          {refreshError}
        </div>
      )}

      {!schemaQuery.isLoading &&
        !schemaQuery.isError &&
        availableCharacteristics.length === 0 && (
          <div className="rounded-2xl border border-blue-500/20 bg-blue-500/[0.05] p-5 text-sm text-blue-100/80">
            Для всех поддерживаемых характеристик этого типа профили уже
            созданы. Вы можете редактировать, включать и отключать их ниже.
          </div>
        )}

      {unsupportedCharacteristicsCount > 0 && (
        <div className="rounded-2xl border border-amber-500/20 bg-amber-500/[0.05] p-5 text-sm text-amber-100/80">
          Характеристик без зарегистрированной RecognitionStrategy:
          <span className="ml-2 font-semibold">
            {unsupportedCharacteristicsCount}
          </span>
          . Профили для них пока не создаются, потому что движок не сможет их
          использовать.
        </div>
      )}

      {showCreateForm && availableCharacteristics.length > 0 && (
        <CreateRecognitionProfileForm
          key={`${preview.productTypeCode}-${preview.recognitionProfiles
            .map((profile) => profile.id)
            .sort()
            .join("-")}`}
          productTypeCode={preview.productTypeCode}
          characteristics={availableCharacteristics}
          isRecognitionRefreshing={isRecognitionRefreshing}
          onCompleted={handleCompleted}
        />
      )}

      <div className="grid gap-4 border-t border-white/10 pt-5">
        <div>
          <h3 className="text-lg font-semibold text-white">
            Существующие профили
          </h3>

          <p className="mt-2 text-sm text-slate-400">
            Изменения применяются к PostgreSQL, после чего Preview запускается
            повторно.
          </p>
        </div>

        {preview.recognitionProfiles.length === 0 ? (
          <div className="rounded-2xl border border-white/10 bg-black/20 p-5 text-sm text-slate-400">
            Для выбранного типа пока нет профилей.
          </div>
        ) : (
          <div className="grid gap-4">
            {preview.recognitionProfiles.map((profile) => (
              <RecognitionProfileEditor
                key={`${profile.id}-${profile.updatedAtUtc}`}
                profile={profile}
                isRecognitionRefreshing={isRecognitionRefreshing}
                onCompleted={handleCompleted}
              />
            ))}
          </div>
        )}
      </div>
    </section>
  );
}

function profileAlreadyExists(
  characteristic: CatalogProductTypeCharacteristicSchemaItem,
  preview: CatalogProductNameRecognitionPreview,
): boolean {
  const descriptor = getRecognitionStrategyDescriptor(characteristic.code);

  if (!descriptor) {
    return true;
  }

  return preview.recognitionProfiles.some(
    (profile) =>
      profile.characteristicDefinitionId === characteristic.definitionId &&
      profile.strategyKind === descriptor.strategyKind,
  );
}

function ManagementMetric({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
      <p className="text-xs text-slate-500">{label}</p>

      <p className="mt-2 text-2xl font-semibold text-white">{value}</p>
    </div>
  );
}
