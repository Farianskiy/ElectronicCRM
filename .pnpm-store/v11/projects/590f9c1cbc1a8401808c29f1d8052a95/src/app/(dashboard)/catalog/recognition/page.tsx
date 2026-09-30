"use client";

import Link from "next/link";
import { useMutation, useQuery } from "@tanstack/react-query";
import type { FormEvent } from "react";
import { useState } from "react";
import { RequirePermission } from "@/features/auth/ui/RequirePermission";
import { getCatalogManufacturers } from "@/features/catalogMetadata/api/getCatalogManufacturers";
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
import { EvaluationReportLookup } from "@/features/catalogRecognition/ui/EvaluationReportLookup";
import { RecognitionDatasetExportPanel } from "@/features/catalogRecognition/ui/RecognitionDatasetExportPanel";
import { ProductTypeSuggestionPreviewPanel } from "@/features/catalogProductTypeSuggestions/ui/ProductTypeSuggestionPreviewPanel";

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
    <RequirePermission permission="DictionariesManage">
      <CatalogRecognitionContent />
    </RequirePermission>
  );
}

function CatalogRecognitionContent() {
  const [productName, setProductName] = useState(
    recognitionExamples[0]?.productName ?? "",
  );

  const [productTypeCode, setProductTypeCode] = useState(
    recognitionExamples[0]?.productTypeCode ?? "",
  );

  const [manufacturerId, setManufacturerId] = useState("");
  const [validationError, setValidationError] = useState<string | null>(null);

  const productTypesQuery = useQuery({
    queryKey: ["catalog-product-types"],
    queryFn: getCatalogProductTypes,
    staleTime: 5 * 60 * 1000,
  });

  const manufacturersQuery = useQuery({
    queryKey: ["catalog-manufacturers"],
    queryFn: getCatalogManufacturers,
    staleTime: 5 * 60 * 1000,
  });

  const manufacturerOptions = [
    { value: "", label: "Без производителя — только общая диагностика" },
    ...(manufacturersQuery.data ?? []).map((manufacturer) => ({
      value: manufacturer.id,
      label: manufacturer.name,
    })),
  ];

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
      manufacturerId: manufacturerId || null,
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
      manufacturerId: manufacturerId || null,
    });
  }

  return (
    <div className="grid gap-6">
      <PageHeader
        title="Проверка распознавания"
        description="Проверка действующих правил распознавания, которые использует импорт: значения, источники и конфликты."
      />

      <section
        id="training-examples"
        aria-labelledby="recognition-learning-title"
        className="scroll-mt-24 rounded-3xl border border-teal-500/25 bg-teal-500/[0.06] p-6"
      >
        <h2
          id="recognition-learning-title"
          className="text-xl font-semibold text-teal-100"
        >
          Обучение распознавания
        </h2>

        <p className="mt-2 text-sm text-slate-300">
          Подтверждённые примеры, подготовка правил, проверка и включение
          изменений.
        </p>

        <p className="mt-3 text-sm text-slate-400">
          Для работы с примерами откройте обучение, выберите производителя и тип
          товара, затем вкладку «Мои подтверждения». Сохранение примера само по
          себе не включает новое правило.
        </p>

        <Link
          href="/catalog/recognition/learning"
          className="mt-5 inline-flex items-center justify-center rounded-2xl border border-teal-500/30 bg-teal-500/10 px-5 py-3 text-sm font-medium text-teal-100 transition hover:bg-teal-500/20 focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-teal-400"
        >
          Открыть обучение
        </Link>
      </section>

      <RecognitionExplanation />

      <ProductTypeSuggestionPreviewPanel />

      <EvaluationReportLookup />
      <RecognitionDatasetExportPanel />

      <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
        <div>
          <h2 className="text-xl font-semibold text-white">Исходные данные</h2>

          <p className="mt-2 text-sm text-slate-400">
            Выберите производителя и тип товара, введите наименование и
            запустите распознавание.
          </p>
        </div>

        <form onSubmit={handleSubmit} className="mt-6 grid gap-5">
          <div className="grid gap-2">
            <span className="text-sm font-medium text-slate-300">
              Производитель
            </span>
            <AppSelect
              ariaLabel="Производитель для распознавания"
              value={manufacturerId}
              options={manufacturerOptions}
              disabled={manufacturersQuery.isLoading}
              onChange={(value) => {
                setManufacturerId(value);
                setValidationError(null);
                previewMutation.reset();
              }}
            />
            {manufacturersQuery.isError && (
              <p className="text-sm text-amber-200">
                {getApiErrorMessage(
                  manufacturersQuery.error,
                  "Не удалось загрузить производителей.",
                )}{" "}
                Без производителя доступна только диагностика неполной области.
              </p>
            )}
          </div>
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
              При выбранных производителе и типе используются те же правила, что
              в импорте. Без полной области правила конкретного производителя и
              типа не проверяются.
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
          <p className="font-medium text-white">
            С производителем и типом товара
          </p>

          <p className="mt-2 text-slate-400">
            Используются базовые правила, словарь, профили и активная версия
            правил для выбранного производителя и типа товара. Ручные значения и
            значения Excel импорт обрабатывает отдельно.
          </p>
        </div>

        <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
          <p className="font-medium text-white">Без производителя или типа</p>

          <p className="mt-2 text-slate-400">
            Доступна общая диагностика. Правила конкретной области проверены не
            полностью; такой результат не воспроизводит импорт с выбранным
            производителем и типом.
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
        key={`${preview.manufacturerId ?? "general"}-${preview.productTypeCode ?? "assistant"}-${preview.productName}`}
        preview={preview}
        isRecognitionRefreshing={isRecognitionRefreshing}
        onRecognitionRefresh={onRecognitionRefresh}
      />

      <div id="dictionary-management" />
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
            preview.hasCompleteScope
              ? "Действующая конфигурация импорта"
              : "Общая диагностика — производитель или тип не выбран"
          }
        />

        <ScopeDetail
          label="Производитель"
          value={preview.manufacturerName ?? "Не выбран"}
        />
        <ScopeDetail
          label="Использованная активная версия"
          value={
            preview.hasCompleteScope
              ? (preview.activeRuleSetVersionId ??
                "Нет активной версии — базовые правила и словарь")
              : "Не проверена: выберите производителя и тип"
          }
          monospace={Boolean(preview.activeRuleSetVersionId)}
        />
        {preview.activeRuleSetSequenceNumber != null && (
          <ScopeDetail
            label="Переключение конфигурации"
            value={String(preview.activeRuleSetSequenceNumber)}
          />
        )}

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

      {!preview.hasCompleteScope && (
        <p className="mt-4 rounded-2xl border border-amber-500/20 bg-amber-500/[0.06] p-4 text-sm text-amber-200">
          Производитель и тип товара выбраны не полностью. Правила для
          конкретного сочетания товаров не проверены. Выберите оба значения,
          чтобы использовать ту же конфигурацию распознавания, что и при
          импорте.
        </p>
      )}

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
            {preview.hasProductTypeScope
              ? "Для выбранного типа нет разрешённых характеристик."
              : "Ограничение схемой типа отсутствует. Доступны общие стратегии."}
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
