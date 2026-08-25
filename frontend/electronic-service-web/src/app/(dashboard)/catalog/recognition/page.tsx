"use client";

import { useMutation, useQuery } from "@tanstack/react-query";
import type { FormEvent } from "react";
import { useState } from "react";
import { RequireTechnicalUser } from "@/features/auth/ui/RequireTechnicalUser";
import { getCatalogProductTypes } from "@/features/catalogMetadata/api/getCatalogProductTypes";
import { previewCatalogProductNameRecognition } from "@/features/catalogRecognition/api/previewCatalogProductNameRecognition";
import type {
  CatalogProductNameRecognitionPreview,
  CatalogRecognitionConflict,
} from "@/features/catalogRecognition/model/types";
import { RecognitionCandidateCard } from "@/features/catalogRecognition/ui/RecognitionCandidateCard";
import { RecognitionHighlightedProductName } from "@/features/catalogRecognition/ui/RecognitionHighlightedProductName";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppSelect } from "@/shared/ui/AppSelect";
import { PageHeader } from "@/shared/ui/PageHeader";
import { RecognitionProfilesPanel } from "@/features/catalogRecognition/ui/RecognitionProfilesPanel";
import { RecognitionProfileManagementPanel } from "@/features/catalogRecognition/ui/RecognitionProfileManagementPanel";
import { RecognitionDictionaryTermCreationPanel } from "@/features/catalogDictionaries/ui/RecognitionDictionaryTermCreationPanel";
import { RecognitionDictionaryManagementPanel } from "@/features/catalogDictionaries/ui/RecognitionDictionaryManagementPanel";
import { RecognitionDatasetExportPanel } from "@/features/catalogRecognition/ui/RecognitionDatasetExportPanel";

interface RecognitionExample {
  label: string;
  productTypeCode: string;
  productName: string;
}

const recognitionExamples: RecognitionExample[] = [
  {
    label: "Модульный автомат",
    productTypeCode: "MODULAR_CIRCUIT_BREAKER",
    productName: "NB1 63 1п 10А B 6кА CHINT",
  },
  {
    label: "Дифференциальный автомат",
    productTypeCode: "DIFFERENTIAL_CIRCUIT_BREAKER",
    productName: "АВДТ 2п 16А C 30мА 6кА IEK",
  },
  {
    label: "УЗО",
    productTypeCode: "RCD",
    productName: "УЗО 2п 40А 30мА тип AC IEK",
  },
];

export default function CatalogRecognitionPage() {
  return (
    <RequireTechnicalUser>
      <CatalogRecognitionContent />
    </RequireTechnicalUser>
  );
}

function CatalogRecognitionContent() {
  const [productName, setProductName] = useState(
    recognitionExamples[0]?.productName ?? "",
  );

  const [productTypeCode, setProductTypeCode] = useState(
    recognitionExamples[0]?.productTypeCode ?? "",
  );

  const [validationError, setValidationError] = useState<string | null>(null);

  const productTypesQuery = useQuery({
    queryKey: ["catalog-product-types"],
    queryFn: getCatalogProductTypes,
    staleTime: 5 * 60 * 1000,
  });

  const previewMutation = useMutation({
    mutationFn: previewCatalogProductNameRecognition,
  });

  const productTypes = productTypesQuery.data ?? [];
  const preview = previewMutation.data;

  const productTypeOptions = [
    {
      value: "",
      label: "Без типа товара — режим ассистента",
    },
    ...productTypes.map((productType) => ({
      value: productType.code,
      label: `${productType.name} — ${productType.code}`,
    })),
  ];

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    if (productName.trim().length === 0) {
      setValidationError("Введите наименование товара.");
      return;
    }

    setValidationError(null);

    previewMutation.mutate({
      productName,
      productTypeCode: productTypeCode.length > 0 ? productTypeCode : null,
    });
  }

  function handleProductNameChange(value: string): void {
    setProductName(value);
    setValidationError(null);
    previewMutation.reset();
  }

  function handleProductTypeChange(value: string): void {
    setProductTypeCode(value);
    setValidationError(null);
    previewMutation.reset();
  }

  function applyExample(example: RecognitionExample): void {
    setProductName(example.productName);
    setProductTypeCode(example.productTypeCode);
    setValidationError(null);
    previewMutation.reset();
  }

  async function refreshRecognitionPreview(): Promise<void> {
    await previewMutation.mutateAsync({
      productName,
      productTypeCode: productTypeCode.length > 0 ? productTypeCode : null,
    });
  }

  return (
    <div className="grid gap-6">
      <PageHeader
        title="Проверка распознавания"
        description="Диагностическая страница для проверки Recognition Engine, профилей характеристик, конфликтов, confidence и span."
      />

      <RecognitionExplanation />

      <RecognitionDatasetExportPanel />

      <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
        <div>
          <h2 className="text-xl font-semibold text-white">Исходные данные</h2>

          <p className="mt-2 text-sm text-slate-400">
            Выберите тип товара или оставьте режим ассистента, введите
            наименование и запустите распознавание.
          </p>
        </div>

        <form onSubmit={handleSubmit} className="mt-6 grid gap-5">
          <div className="grid gap-2">
            <span className="text-sm font-medium text-slate-300">
              Контекст типа товара
            </span>

            <AppSelect
              ariaLabel="Контекст типа товара"
              value={productTypeCode}
              options={productTypeOptions}
              disabled={productTypesQuery.isLoading}
              onChange={handleProductTypeChange}
            />

            <p className="text-xs text-slate-500">
              При выборе типа движок использует его ProductTypeId, схему
              характеристик и активные RecognitionProfile. Без типа движок
              работает так же, как ассистент.
            </p>
          </div>

          <label className="grid gap-2">
            <span className="text-sm font-medium text-slate-300">
              Наименование товара
            </span>

            <textarea
              value={productName}
              onChange={(event) => handleProductNameChange(event.target.value)}
              rows={4}
              placeholder="Например: NB1 63 1п 10А B 6кА CHINT"
              className="resize-y rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none placeholder:text-slate-600 focus:border-teal-400 focus:ring-2 focus:ring-teal-400/20"
            />
          </label>

          {validationError && (
            <div className="rounded-2xl border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-200">
              {validationError}
            </div>
          )}

          <div className="flex flex-wrap gap-3">
            <button
              type="submit"
              disabled={previewMutation.isPending}
              className="rounded-2xl bg-teal-500 px-5 py-3 text-sm font-medium text-white transition hover:bg-teal-400 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {previewMutation.isPending
                ? "Распознаём..."
                : "Запустить распознавание"}
            </button>

            {preview && (
              <button
                type="button"
                onClick={() => previewMutation.reset()}
                className="rounded-2xl border border-white/10 bg-white/[0.04] px-5 py-3 text-sm font-medium text-slate-300 transition hover:bg-white/[0.08]"
              >
                Скрыть результат
              </button>
            )}
          </div>
        </form>

        <div className="mt-6 border-t border-white/10 pt-5">
          <p className="text-sm font-medium text-slate-300">
            Примеры для быстрой проверки
          </p>

          <div className="mt-3 flex flex-wrap gap-3">
            {recognitionExamples.map((example) => (
              <button
                key={example.productTypeCode}
                type="button"
                onClick={() => applyExample(example)}
                className="rounded-xl border border-white/10 bg-white/[0.03] px-4 py-2 text-sm text-slate-300 transition hover:border-teal-500/30 hover:bg-teal-500/10 hover:text-teal-200"
              >
                {example.label}
              </button>
            ))}
          </div>
        </div>
      </section>

      {productTypesQuery.isError && (
        <section className="rounded-3xl border border-amber-500/30 bg-amber-500/10 p-6 text-amber-200">
          <h2 className="font-semibold">Не удалось загрузить типы товаров</h2>

          <p className="mt-2 text-sm">
            {getApiErrorMessage(
              productTypesQuery.error,
              "Не удалось загрузить типы товаров.",
            )}
          </p>

          <p className="mt-2 text-sm text-amber-200/80">
            Проверка без типа товара всё ещё доступна.
          </p>
        </section>
      )}

      {previewMutation.isError && (
        <section className="rounded-3xl border border-red-500/30 bg-red-500/10 p-6 text-red-200">
          <h2 className="font-semibold">Распознавание не выполнено</h2>

          <p className="mt-2 text-sm">
            {getApiErrorMessage(
              previewMutation.error,
              "Recognition Engine не смог обработать наименование.",
            )}
          </p>
        </section>
      )}

      {preview && (
        <RecognitionResult
          preview={preview}
          isRecognitionRefreshing={previewMutation.isPending}
          onRecognitionRefresh={refreshRecognitionPreview}
        />
      )}
    </div>
  );
}

function RecognitionExplanation() {
  return (
    <section className="rounded-3xl border border-blue-500/20 bg-blue-500/[0.06] p-6">
      <h2 className="text-lg font-semibold text-blue-100">
        Что именно проверяет эта страница
      </h2>

      <div className="mt-4 grid gap-4 text-sm text-slate-300 lg:grid-cols-2">
        <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
          <p className="font-medium text-white">С выбранным типом товара</p>

          <p className="mt-2 text-slate-400">
            Используются характеристики схемы типа, его ProductTypeId, активные
            профили, единицы, диапазоны и MinimumConfidence.
          </p>
        </div>

        <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
          <p className="font-medium text-white">Без выбранного типа товара</p>

          <p className="mt-2 text-slate-400">
            Профили типа недоступны. Стратегии работают с базовыми настройками.
            Это показывает текущее поведение ассистента.
          </p>
        </div>
      </div>

      <p className="mt-4 text-sm text-blue-100/80">
        Сам запуск Preview является безопасным диагностическим чтением.
        Отдельные Technical-панели ниже могут изменять профили и словарные
        термины только после явного подтверждения пользователя.
      </p>
    </section>
  );
}

function RecognitionResult({
  preview,
  isRecognitionRefreshing,
  onRecognitionRefresh,
}: {
  preview: CatalogProductNameRecognitionPreview;
  isRecognitionRefreshing: boolean;
  onRecognitionRefresh: () => Promise<void>;
}) {
  return (
    <>
      <RecognitionSummary preview={preview} />

      <RecognitionScope preview={preview} />

      <RecognitionDictionaryTermCreationPanel
        key={`${preview.productTypeCode ?? "assistant"}-${preview.productName}`}
        preview={preview}
        isRecognitionRefreshing={isRecognitionRefreshing}
        onRecognitionRefresh={onRecognitionRefresh}
      />

      <RecognitionDictionaryManagementPanel
        currentProductTypeId={preview.productTypeId}
        isRecognitionRefreshing={isRecognitionRefreshing}
        onRecognitionRefresh={onRecognitionRefresh}
      />

      <RecognitionProfileManagementPanel
        preview={preview}
        isRecognitionRefreshing={isRecognitionRefreshing}
        onRecognitionRefresh={onRecognitionRefresh}
      />

      <RecognitionProfilesPanel preview={preview} />

      <RecognitionHighlightedProductName
        productName={preview.productName}
        characteristics={preview.characteristics}
        conflicts={preview.conflicts}
      />

      <AcceptedCharacteristics preview={preview} />

      <RecognitionConflicts
        conflicts={preview.conflicts}
        productName={preview.productName}
      />

      <AllRecognitionCandidates preview={preview} />
    </>
  );
}

function RecognitionSummary({
  preview,
}: {
  preview: CatalogProductNameRecognitionPreview;
}) {
  return (
    <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
      <div>
        <h2 className="text-xl font-semibold text-white">Результат запуска</h2>

        <p className="mt-2 text-sm text-slate-400">
          Краткая сводка того, что вернул Recognition Engine.
        </p>
      </div>

      <div className="mt-5 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <SummaryCard
          label="Принято характеристик"
          value={preview.characteristics.length.toString()}
          colorClassName="text-teal-300"
        />

        <SummaryCard
          label="Конфликтов"
          value={preview.conflicts.length.toString()}
          colorClassName={
            preview.conflicts.length > 0 ? "text-red-300" : "text-slate-200"
          }
        />

        <SummaryCard
          label="Всего кандидатов"
          value={preview.candidates.length.toString()}
          colorClassName="text-blue-300"
        />

        <SummaryCard
          label="Разрешено характеристик"
          value={preview.allowedCharacteristicCodes?.length.toString() ?? "Все"}
          colorClassName="text-violet-300"
        />
      </div>

      <div className="mt-5 rounded-2xl border border-white/10 bg-black/20 p-4">
        <p className="text-xs text-slate-500">Нормализованное наименование</p>

        <p className="mt-2 break-words font-mono text-sm text-slate-200">
          {preview.normalizedProductName}
        </p>
      </div>
    </section>
  );
}

function RecognitionScope({
  preview,
}: {
  preview: CatalogProductNameRecognitionPreview;
}) {
  return (
    <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
      <div>
        <h2 className="text-xl font-semibold text-white">
          Контекст распознавания
        </h2>

        <p className="mt-2 text-sm text-slate-400">
          Эти данные определяют, какие стратегии и профили мог использовать
          движок.
        </p>
      </div>

      <div className="mt-5 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <ScopeDetail
          label="Режим"
          value={
            preview.hasProductTypeScope
              ? "Контекст типа товара"
              : "Без типа товара — ассистент"
          }
        />

        <ScopeDetail
          label="Название типа"
          value={preview.productTypeName ?? "Не выбрано"}
        />

        <ScopeDetail
          label="Код типа"
          value={preview.productTypeCode ?? "null"}
          monospace
        />

        <ScopeDetail
          label="ProductTypeId"
          value={preview.productTypeId ?? "null"}
          monospace
        />
      </div>

      <div className="mt-5">
        <p className="text-sm font-medium text-slate-300">
          Разрешённые характеристики
        </p>

        {preview.allowedCharacteristicCodes &&
        preview.allowedCharacteristicCodes.length > 0 ? (
          <div className="mt-3 flex flex-wrap gap-2">
            {preview.allowedCharacteristicCodes.map((code) => (
              <span
                key={code}
                className="rounded-full border border-violet-500/25 bg-violet-500/10 px-3 py-1 font-mono text-xs text-violet-200"
              >
                {code}
              </span>
            ))}
          </div>
        ) : (
          <p className="mt-3 rounded-2xl border border-amber-500/20 bg-amber-500/[0.06] p-4 text-sm text-amber-200">
            Ограничение схемой типа отсутствует. Движок может запускать все
            зарегистрированные стратегии.
          </p>
        )}
      </div>
    </section>
  );
}

function AcceptedCharacteristics({
  preview,
}: {
  preview: CatalogProductNameRecognitionPreview;
}) {
  return (
    <section className="grid gap-4">
      <div>
        <h2 className="text-2xl font-semibold text-white">
          Итоговые характеристики
        </h2>

        <p className="mt-2 text-sm text-slate-400">
          Это победившие значения, которые Recognition Engine считает
          результатом распознавания.
        </p>
      </div>

      {preview.characteristics.length === 0 ? (
        <EmptyResult
          title="Итоговых характеристик нет"
          description="Стратегии не нашли подходящих значений либо все найденные значения попали в конфликты."
        />
      ) : (
        <div className="grid gap-4 xl:grid-cols-2">
          {preview.characteristics.map((characteristic) => (
            <RecognitionCandidateCard
              key={`${characteristic.characteristicCode}-${characteristic.startIndex}-${characteristic.recognizerKey}`}
              candidate={characteristic}
              productName={preview.productName}
              tone="Accepted"
            />
          ))}
        </div>
      )}
    </section>
  );
}

function RecognitionConflicts({
  conflicts,
  productName,
}: {
  conflicts: CatalogRecognitionConflict[];
  productName: string;
}) {
  return (
    <section className="grid gap-4">
      <div>
        <h2 className="text-2xl font-semibold text-white">Конфликты</h2>

        <p className="mt-2 text-sm text-slate-400">
          Конфликт возникает, когда кандидаты одинакового уровня надёжности
          предлагают разные нормализованные значения.
        </p>
      </div>

      {conflicts.length === 0 ? (
        <EmptyResult
          title="Конфликтов нет"
          description="Для каждой распознанной характеристики движок смог выбрать одно итоговое значение."
          success
        />
      ) : (
        <div className="grid gap-5">
          {conflicts.map((conflict) => (
            <article
              key={conflict.characteristicCode}
              className="rounded-3xl border border-red-500/30 bg-red-500/[0.06] p-6"
            >
              <h3 className="font-mono text-lg font-semibold text-red-100">
                {conflict.characteristicCode}
              </h3>

              <p className="mt-2 text-sm text-red-200/80">
                Движок не применил характеристику, потому что обнаружил
                несколько разных значений одного уровня источника.
              </p>

              <div className="mt-5 grid gap-4 xl:grid-cols-2">
                {conflict.candidates.map((candidate) => (
                  <RecognitionCandidateCard
                    key={`${candidate.characteristicCode}-${candidate.normalizedValue}-${candidate.startIndex}-${candidate.recognizerKey}`}
                    candidate={candidate}
                    productName={productName}
                    tone="Conflict"
                  />
                ))}
              </div>
            </article>
          ))}
        </div>
      )}
    </section>
  );
}

function AllRecognitionCandidates({
  preview,
}: {
  preview: CatalogProductNameRecognitionPreview;
}) {
  return (
    <section className="grid gap-4">
      <div>
        <h2 className="text-2xl font-semibold text-white">
          Все исходные кандидаты
        </h2>

        <p className="mt-2 text-sm text-slate-400">
          Здесь показаны все доказательства до выбора победителя. Один кандидат
          может проиграть более надёжному источнику, но останется в этом списке
          для диагностики.
        </p>
      </div>

      {preview.candidates.length === 0 ? (
        <EmptyResult
          title="Кандидатов нет"
          description="Ни одна зарегистрированная стратегия и ни один подтверждённый словарный термин не распознали значение."
        />
      ) : (
        <div className="grid gap-4 xl:grid-cols-2">
          {preview.candidates.map((candidate, index) => (
            <RecognitionCandidateCard
              key={`${candidate.characteristicCode}-${candidate.normalizedValue}-${candidate.startIndex}-${candidate.recognizerKey}-${index}`}
              candidate={candidate}
              productName={preview.productName}
              tone="Evidence"
            />
          ))}
        </div>
      )}
    </section>
  );
}

function SummaryCard({
  label,
  value,
  colorClassName,
}: {
  label: string;
  value: string;
  colorClassName: string;
}) {
  return (
    <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
      <p className="text-sm text-slate-400">{label}</p>

      <p className={`mt-2 text-3xl font-semibold ${colorClassName}`}>{value}</p>
    </div>
  );
}

function ScopeDetail({
  label,
  value,
  monospace = false,
}: {
  label: string;
  value: string;
  monospace?: boolean;
}) {
  return (
    <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
      <p className="text-xs text-slate-500">{label}</p>

      <p
        className={`mt-2 break-all text-sm text-slate-200 ${
          monospace ? "font-mono" : ""
        }`}
      >
        {value}
      </p>
    </div>
  );
}

function EmptyResult({
  title,
  description,
  success = false,
}: {
  title: string;
  description: string;
  success?: boolean;
}) {
  return (
    <div
      className={
        success
          ? "rounded-3xl border border-teal-500/25 bg-teal-500/[0.06] p-6"
          : "rounded-3xl border border-white/10 bg-white/[0.04] p-6"
      }
    >
      <h3
        className={
          success
            ? "text-lg font-semibold text-teal-200"
            : "text-lg font-semibold text-white"
        }
      >
        {title}
      </h3>

      <p className="mt-2 text-sm text-slate-400">{description}</p>
    </div>
  );
}
