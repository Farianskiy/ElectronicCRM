"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState, type FormEvent } from "react";
import { useCurrentUserAccess } from "@/features/auth/model/CurrentUserAccessContext";
import { getCatalogManufacturers } from "@/features/catalogMetadata/api/getCatalogManufacturers";
import { getCatalogProductTypeCharacteristics } from "@/features/catalogMetadata/api/getCatalogProductTypeCharacteristics";
import { getCatalogProductTypes } from "@/features/catalogMetadata/api/getCatalogProductTypes";
import type {
  CatalogCharacteristicDataType,
} from "@/features/catalogMetadata/model/types";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppSelect } from "@/shared/ui/AppSelect";
import { AppButton } from "@/shared/ui/AppButton";
import { AppInput } from "@/shared/ui/AppInput";
import { CatalogImportProductTypePicker } from "../productTypes/CatalogImportProductTypePicker";
import { updateCatalogImportRow } from "../../api/updateCatalogImportRow";
import { confirmCatalogImportTrainingExample } from "../../api/confirmCatalogImportTrainingExample";
import { catalogImportConfirmedSpansQueryKey } from "../../api/getCatalogImportRowConfirmedSpans";
import { CatalogImportConfirmedSpanEditor } from "./CatalogImportConfirmedSpanEditor";
import { CatalogImportCharacteristicGrid } from "./CatalogImportCharacteristicGrid";
import { ProductAnalogiesPreviewPanel } from "@/features/catalogProductAnalogies/ui/ProductAnalogiesPreviewPanel";
import type {
  CatalogProductAnalogyCharacteristicProposal,
} from "@/features/catalogProductAnalogies/model/types";
import type { CatalogImportConfirmedSpan } from "../../model/types";
import { catalogImportQueryKeys } from "../../model/queryKeys";
import type {
  CatalogImportRow,
  UpdateCatalogImportRowRequest,
  UpdateCatalogImportRowResponse,
} from "../../model/types";

interface CatalogImportRowEditorProps {
  batchId: string;
  defaultProductTypeId?: string | null;
  row: CatalogImportRow;
  onCancel: () => void;
  onSaved: (result: UpdateCatalogImportRowResponse) => void;
}

interface ParsedNumberResult {
  value: number | null;
  error?: string;
}

interface SaveRowVariables {
  request: UpdateCatalogImportRowRequest;
  confirmTrainingExamples: boolean;
}

interface TrainingConfirmationFailure {
  confirmedCount: number;
  error: unknown;
}

interface SaveRowResult {
  row: UpdateCatalogImportRowResponse;
  confirmedExamplesCount: number;
  trainingFailure: TrainingConfirmationFailure | null;
}

const textareaClassName = [
  "min-h-28 w-full min-w-0 resize-y rounded-xl border px-4 py-3",
  "border-[var(--app-border)] bg-[var(--app-surface)]",
  "text-sm text-[var(--app-text)]",
  "placeholder:text-[var(--app-muted)]",
  "transition-colors motion-reduce:transition-none",
  "enabled:hover:border-[var(--app-border-strong)]",
  "focus:outline-none focus:border-[var(--app-accent)]",
  "focus:ring-2 focus:ring-[var(--app-accent)]",
  "disabled:cursor-not-allowed disabled:opacity-60",
].join(" ");

function parseNullableDecimal(
  rawValue: string,
  fieldName: string,
): ParsedNumberResult {
  const normalizedValue = rawValue.trim().replace(",", ".");

  if (!normalizedValue) {
    return {
      value: null,
    };
  }

  const value = Number(normalizedValue);

  if (!Number.isFinite(value)) {
    return {
      value: null,
      error: `Поле «${fieldName}» должно содержать число.`,
    };
  }

  return {
    value,
  };
}

function parseNullableInteger(
  rawValue: string,
  fieldName: string,
): ParsedNumberResult {
  const normalizedValue = rawValue.trim();

  if (!normalizedValue) {
    return {
      value: null,
    };
  }

  const value = Number(normalizedValue);

  if (!Number.isSafeInteger(value)) {
    return {
      value: null,
      error: `Поле «${fieldName}» должно содержать целое число.`,
    };
  }

  return {
    value,
  };
}

function normalizeCharacteristicValue(
  value: string,
  dataType: CatalogCharacteristicDataType,
): string {
  const trimmedValue = value.trim();

  if (dataType === "Number") {
    return trimmedValue.replace(",", ".");
  }

  return trimmedValue;
}

export function CatalogImportRowEditor({
  batchId,
  defaultProductTypeId,
  row,
  onCancel,
  onSaved,
}: CatalogImportRowEditorProps) {
  const queryClient = useQueryClient();
  const access = useCurrentUserAccess();

  const [name, setName] = useState(row.data.name ?? "");

  const [article, setArticle] = useState(row.data.article ?? "");

  const [productTypeId, setProductTypeId] = useState(
    row.data.productTypeId ?? defaultProductTypeId ?? "",
  );

  const [manufacturerId, setManufacturerId] = useState(
    row.data.manufacturerId ?? "",
  );

  const [price, setPrice] = useState(row.data.price?.toString() ?? "");

  const [stockQuantity, setStockQuantity] = useState(
    row.data.stockQuantity?.toString() ?? "",
  );

  const [characteristicValues, setCharacteristicValues] = useState<
    Record<string, string>
  >({
    ...row.data.characteristics,
  });

  const [formErrors, setFormErrors] = useState<string[]>([]);
  const [trainingConfirmationFailure, setTrainingConfirmationFailure] =
    useState<TrainingConfirmationFailure | null>(null);
  const [confirmedSpans, setConfirmedSpans] = useState<
    Record<string, CatalogImportConfirmedSpan>
  >({});
  const [confirmTrainingOnSave, setConfirmTrainingOnSave] = useState(false);
  const [analogyDecisions, setAnalogyDecisions] = useState<
    Record<
      string,
      {
        decision: "Accepted" | "Rejected";
        reason?: string;
        feedbackId?: string | null;
      }
    >
  >({});

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

  const selectedProductType = productTypesQuery.data?.find(
    (productType) => productType.id === productTypeId,
  );

  const characteristicsQuery = useQuery({
    queryKey: [
      "catalog-product-type-characteristics",
      selectedProductType?.code ?? "",
    ],
    queryFn: () =>
      getCatalogProductTypeCharacteristics(selectedProductType?.code ?? ""),
    enabled: Boolean(selectedProductType?.code),
    staleTime: 5 * 60 * 1000,
  });

  const characteristics = characteristicsQuery.data ?? [];
  const manufacturers = manufacturersQuery.data ?? [];

  const metadataError =
    productTypesQuery.error ??
    manufacturersQuery.error ??
    characteristicsQuery.error;

  const saveMutation = useMutation({
    mutationKey: catalogImportQueryKeys.saveRows(batchId),
    mutationFn: async ({
      request,
      confirmTrainingExamples,
    }: SaveRowVariables): Promise<SaveRowResult> => {
      const savedRow = await updateCatalogImportRow(
        batchId,
        row.rowId,
        request,
      );

      if (!confirmTrainingExamples || !request.confirmedSpans) {
        return {
          row: savedRow,
          confirmedExamplesCount: 0,
          trainingFailure: null,
        };
      }

      let confirmedExamplesCount = 0;

      try {
        for (const [characteristicId, span] of Object.entries(
          request.confirmedSpans,
        )) {
          const normalizedValue =
            savedRow.data.characteristics[characteristicId]?.trim();
          const savedName = savedRow.data.name?.trim();
          const savedManufacturerId = savedRow.data.manufacturerId;
          const savedProductTypeId = savedRow.data.productTypeId;
          const rawValue = span.productName.slice(
            span.start,
            span.start + span.length,
          );

          if (
            !normalizedValue ||
            !savedName ||
            !savedManufacturerId ||
            !savedProductTypeId ||
            !rawValue
          ) {
            throw new Error(
              "Сохранённая строка не содержит всех данных учебного примера.",
            );
          }

          await confirmCatalogImportTrainingExample(
            batchId,
            row.rowId,
            characteristicId,
            {
              productName: savedName,
              manufacturerId: savedManufacturerId,
              productTypeId: savedProductTypeId,
              normalizedValue,
              rawValue,
              spanStart: span.start,
              spanLength: span.length,
            },
          );

          confirmedExamplesCount++;
        }
      } catch (error) {
        return {
          row: savedRow,
          confirmedExamplesCount,
          trainingFailure: {
            confirmedCount: confirmedExamplesCount,
            error,
          },
        };
      }

      return {
        row: savedRow,
        confirmedExamplesCount,
        trainingFailure: null,
      };
    },

    onSuccess: async (result) => {
      setConfirmedSpans({});
      setAnalogyDecisions({});
      setTrainingConfirmationFailure(result.trainingFailure);

      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: catalogImportConfirmedSpansQueryKey(batchId, row.rowId),
        }),
        queryClient.invalidateQueries({
          queryKey: catalogImportQueryKeys.details(batchId),
        }),

        queryClient.invalidateQueries({
          queryKey: catalogImportQueryKeys.rowsRoot(batchId),
        }),

        queryClient.invalidateQueries({
          queryKey: catalogImportQueryKeys.rowProblemCodesRoot(batchId),
          refetchType: "none",
        }),

        queryClient.invalidateQueries({
          queryKey: catalogImportQueryKeys.myRoot,
        }),
        queryClient.invalidateQueries({
          queryKey: ["training-examples"],
        }),
      ]);

      if (!result.trainingFailure) {
        onSaved(result.row);
      }
    },
  });

  const isMetadataLoading =
    productTypesQuery.isLoading ||
    manufacturersQuery.isLoading ||
    characteristicsQuery.isLoading;

  const isBusy = isMetadataLoading || saveMutation.isPending;

  function handleCharacteristicChange(
    characteristicId: string,
    value: string,
  ): void {
    setCharacteristicValues((currentValues) => ({
      ...currentValues,
      [characteristicId]: value,
    }));

    setConfirmedSpans((currentSpans) => {
      const nextSpans = { ...currentSpans };
      delete nextSpans[characteristicId];
      return nextSpans;
    });

    setFormErrors([]);
    setTrainingConfirmationFailure(null);
    saveMutation.reset();
  }

  function applyAnalogyCharacteristicProposal(
    proposal: CatalogProductAnalogyCharacteristicProposal,
  ): void {
    const characteristic = characteristics.find(
      (item) => item.code === proposal.characteristicCode,
    );

    if (!characteristic) {
      setFormErrors([
        `Характеристика ${proposal.characteristicCode} отсутствует в выбранном типе товара.`,
      ]);
      return;
    }

    handleCharacteristicChange(characteristic.id, proposal.proposedValue);
    setAnalogyDecisions((current) => ({
      ...current,
      [characteristic.id]: {
        decision: "Accepted",
        feedbackId: proposal.feedbackId,
      },
    }));
  }

  function rejectAnalogyCharacteristicProposal(
    proposal: CatalogProductAnalogyCharacteristicProposal,
    reason: string,
  ): void {
    const characteristic = characteristics.find(
      (item) => item.code === proposal.characteristicCode,
    );

    if (!characteristic) {
      setFormErrors([
        `Характеристика ${proposal.characteristicCode} отсутствует в выбранном типе товара.`,
      ]);
      return;
    }

    setAnalogyDecisions((current) => ({
      ...current,
      [characteristic.id]: {
        decision: "Rejected",
        reason,
        feedbackId: proposal.feedbackId,
      },
    }));
  }

  function saveRow(confirmTrainingExamples: boolean): void {
    const errors: string[] = [];

    const parsedPrice = parseNullableDecimal(price, "Цена");

    if (parsedPrice.error) {
      errors.push(parsedPrice.error);
    }

    const parsedStockQuantity = parseNullableInteger(stockQuantity, "Остаток");

    if (parsedStockQuantity.error) {
      errors.push(parsedStockQuantity.error);
    }

    if (!productTypeId) {
      errors.push("Выберите тип товара.");
    }

    if (errors.length > 0) {
      setFormErrors(errors);

      return;
    }

    const requestCharacteristics: Record<string, string> = {};

    for (const characteristic of characteristics) {
      const rawValue = characteristicValues[characteristic.id] ?? "";

      const normalizedValue = normalizeCharacteristicValue(
        rawValue,
        characteristic.dataType,
      );

      /*
       * Пустые необязательные характеристики не отправляем.
       * Для обязательных backend самостоятельно сформирует issue.
       */
      if (!normalizedValue) {
        continue;
      }

      requestCharacteristics[characteristic.id] = normalizedValue;
    }

    const request: UpdateCatalogImportRowRequest = {
      name: name.trim() || null,
      article: article.trim() || null,
      productTypeId: productTypeId || null,
      manufacturerId: manufacturerId || null,
      price: parsedPrice.value,
      stockQuantity: parsedStockQuantity.value,
      characteristics: requestCharacteristics,
      confirmedSpans,
      analogyDecisions: Object.entries(analogyDecisions).map(
        ([characteristicDefinitionId, decision]) => ({
          characteristicDefinitionId,
          decision: decision.decision,
          reason: decision.reason ?? null,
          feedbackId: decision.feedbackId ?? null,
        }),
      ),
    };

    setFormErrors([]);
    setTrainingConfirmationFailure(null);
    saveMutation.mutate({ request, confirmTrainingExamples });
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();
    saveRow(
      confirmTrainingOnSave &&
        trainingExamplesCount > 0 &&
        canConfirmTrainingExamples,
    );
  }

  const trainingExamplesCount = Object.keys(confirmedSpans).length;
  const canConfirmTrainingExamples =
    !access.isLoading &&
    !access.isError &&
    access.hasPermission("DictionariesManage");

  const analogyKnownCharacteristics = Object.fromEntries(
    characteristics
      .map((characteristic) => [
        characteristic.code,
        characteristicValues[characteristic.id]?.trim() ?? "",
      ])
      .filter(([, value]) => value.length > 0),
  );

  return (
    <form
      onSubmit={handleSubmit}
      className="grid min-w-0 gap-5 rounded-2xl border border-[var(--app-accent-border)] bg-[var(--app-panel)] p-4 text-[var(--app-text)] sm:p-5"
    >
      <div>
        <div>
          <h3 className="font-semibold text-[var(--app-text)]">
            Основные данные товара
          </h3>

          <p className="mt-1 text-sm text-[var(--app-muted)]">
            Измените только неверные значения. После сохранения строка будет проверена повторно.
          </p>
        </div>
      </div>

      {metadataError && (
        <div className="rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm text-[var(--app-danger)]">
          {getApiErrorMessage(
            metadataError,
            "Не удалось загрузить справочники для редактирования строки.",
          )}
        </div>
      )}

      {isMetadataLoading && (
        <div className="rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4 text-sm text-[var(--app-text)]">
          Загружаем производителей и характеристики...
        </div>
      )}

      <div className="grid gap-4 xl:grid-cols-2">
        <label className="grid gap-2">
          <span className="text-sm font-medium text-[var(--app-text)]">
            Наименование
          </span>

          <textarea
            value={name}
            disabled={isBusy}
            rows={3}
            onChange={(event) => {
              setName(event.target.value);
              setConfirmedSpans({});
              setAnalogyDecisions({});
              setFormErrors([]);
              setTrainingConfirmationFailure(null);
              saveMutation.reset();
            }}
            className={textareaClassName}
            placeholder="Наименование товара"
          />
        </label>

        <label className="grid content-start gap-2">
          <span className="text-sm font-medium text-[var(--app-text)]">
            Артикул
          </span>

          <AppInput
            type="text"
            value={article}
            disabled={isBusy}
            onChange={(event) => {
              setArticle(event.target.value);
              setAnalogyDecisions({});
              setFormErrors([]);
              saveMutation.reset();
            }}
            placeholder="Артикул товара"
          />
        </label>

        <div className="grid content-start gap-2">
          <span className="text-sm font-medium text-[var(--app-text)]">
            Производитель
          </span>

          <AppSelect
            ariaLabel={`Производитель строки ${row.rowNumber}`}
            value={manufacturerId}
            disabled={isBusy}
            onChange={(value) => {
              setManufacturerId(value);
              setConfirmedSpans({});
              setAnalogyDecisions({});
              setFormErrors([]);
              setTrainingConfirmationFailure(null);
              saveMutation.reset();
            }}
            options={[
              {
                value: "",
                label: "Производитель не выбран",
              },
              ...manufacturers.map((manufacturer) => ({
                value: manufacturer.id,
                label: manufacturer.name,
              })),
            ]}
          />

          {row.data.manufacturer && (
            <p className="text-xs text-[var(--app-muted)]">
              Значение после анализа: {row.data.manufacturer}
            </p>
          )}
        </div>

        <div className="grid content-start gap-2">
          <span className="text-sm font-medium text-[var(--app-text)]">
            Тип товара
          </span>

          <CatalogImportProductTypePicker
            ariaLabel={`Тип товара строки ${row.rowNumber}`}
            value={productTypeId}
            productTypes={productTypesQuery.data ?? []}
            disabled={isBusy}
            emptyLabel="Тип товара не выбран"
            onChange={(value) => {
              setProductTypeId(value);
              setCharacteristicValues({});
              setConfirmedSpans({});
              setAnalogyDecisions({});
              setFormErrors([]);
              setTrainingConfirmationFailure(null);
              saveMutation.reset();
            }}
          />
        </div>

        <div className="grid gap-4 sm:grid-cols-2">
          <label className="grid content-start gap-2">
            <span className="text-sm font-medium text-[var(--app-text)]">
              Цена
            </span>

            <AppInput
              type="text"
              inputMode="decimal"
              value={price}
              disabled={isBusy}
              onChange={(event) => {
                setPrice(event.target.value);
                setFormErrors([]);
                saveMutation.reset();
              }}
              placeholder="0.00"
            />
          </label>

          <label className="grid content-start gap-2">
            <span className="text-sm font-medium text-[var(--app-text)]">
              Остаток
            </span>

            <AppInput
              type="text"
              inputMode="numeric"
              value={stockQuantity}
              disabled={isBusy}
              onChange={(event) => {
                setStockQuantity(event.target.value);
                setFormErrors([]);
                saveMutation.reset();
              }}
              placeholder="0"
            />
          </label>
        </div>
      </div>

      <section className="rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel-strong)] p-5">
        <div>
          <h3 className="font-semibold text-[var(--app-text)]">
            Характеристики товара
          </h3>

          <p className="mt-1 text-sm text-[var(--app-muted)]">
            Тип товара:{" "}
            <span className="text-[var(--app-text)]">
              {selectedProductType?.name ?? "не выбран"}
            </span>
          </p>
        </div>

        {characteristics.length === 0 ? (
          <p className="mt-4 text-sm text-[var(--app-muted)]">
            Для выбранного типа характеристики не определены.
          </p>
        ) : (
          <CatalogImportCharacteristicGrid
            characteristics={characteristics}
            values={characteristicValues}
            origins={row.data.characteristicOrigins ?? {}}
            disabled={isBusy}
            onChange={handleCharacteristicChange}
          />
        )}
      </section>

      {canConfirmTrainingExamples && (
        <>
          {Object.keys(analogyDecisions).length > 0 && (
            <section className="rounded-2xl border border-teal-500/25 bg-teal-500/[0.06] p-5">
              <h3 className="font-semibold text-teal-100">
                Решения по предложениям аналогов
              </h3>
              <p className="mt-2 text-sm text-slate-400">
                Эти решения ещё не записаны. Они сохранятся вместе со строкой.
              </p>
              <div className="mt-4 grid gap-3">
                {Object.entries(analogyDecisions).map(
                  ([characteristicId, decision]) => {
                    const characteristic = characteristics.find(
                      (item) => item.id === characteristicId,
                    );

                    return (
                      <div
                        key={characteristicId}
                        className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-white/10 bg-black/20 p-3"
                      >
                        <div>
                          <p className="text-sm font-medium text-white">
                            {characteristic?.name ?? characteristicId}
                          </p>
                          <p className="mt-1 text-xs text-slate-400">
                            {decision.decision === "Accepted"
                              ? "Предложение принято; изменённое значение будет сохранено как исправление."
                              : `Предложение отклонено: ${decision.reason}`}
                          </p>
                        </div>
                        <AppButton
                          size="sm"
                          variant="ghost"
                          onClick={() =>
                            setAnalogyDecisions((current) => {
                              const next = { ...current };
                              delete next[characteristicId];
                              return next;
                            })
                          }
                        >
                          Отменить решение
                        </AppButton>
                      </div>
                    );
                  },
                )}
              </div>
            </section>
          )}

          <details className="rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel-strong)]">
            <summary className="cursor-pointer list-none px-5 py-4 marker:hidden">
              <h3 className="font-semibold text-[var(--app-text)]">
                Предложения похожих товаров
              </h3>
              <p className="mt-1 text-sm text-[var(--app-muted)]">
                Откройте, чтобы сравнить строку с товарами каталога и принять отдельные предложения.
              </p>
            </summary>
            <div className="border-t border-[var(--app-border)] p-4">
              <ProductAnalogiesPreviewPanel
                key={JSON.stringify({
                  name,
                  article,
                  manufacturerId,
                  productTypeId,
                  analogyKnownCharacteristics,
                })}
                productName={name}
                article={article}
                manufacturerId={manufacturerId || null}
                productTypeId={productTypeId || null}
                knownCharacteristics={analogyKnownCharacteristics}
                importBatchId={batchId}
                importRowId={row.rowId}
                onApplyCharacteristicProposal={applyAnalogyCharacteristicProposal}
                onRejectCharacteristicProposal={rejectAnalogyCharacteristicProposal}
              />
            </div>
          </details>
        </>
      )}

      <CatalogImportConfirmedSpanEditor
        batchId={batchId}
        rowId={row.rowId}
        productName={name}
        manufacturerId={manufacturerId}
        productTypeId={productTypeId}
        characteristics={characteristics}
        characteristicValues={characteristicValues}
        drafts={confirmedSpans}
        disabled={isBusy}
        onChange={setConfirmedSpans}
      />

      {formErrors.length > 0 && (
        <div className="rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-5">
          <h3 className="font-semibold text-[var(--app-danger)]">
            Проверьте введённые значения
          </h3>

          <ul className="mt-3 grid gap-2 text-sm text-[var(--app-danger)]">
            {formErrors.map((error) => (
              <li key={error}>• {error}</li>
            ))}
          </ul>
        </div>
      )}

      {saveMutation.isError && (
        <div className="rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-5 text-sm text-[var(--app-danger)]">
          {getApiErrorMessage(
            saveMutation.error,
            "Не удалось сохранить строку импорта.",
          )}
        </div>
      )}

      {trainingConfirmationFailure && (
        <div className="rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-5 text-sm text-[var(--app-danger)]">
          <p className="font-semibold">
            Строка сохранена, но не все учебные примеры подтверждены
          </p>
          <p className="mt-2">
            Успешно подтверждено: {trainingConfirmationFailure.confirmedCount}.
            {" "}
            {getApiErrorMessage(
              trainingConfirmationFailure.error,
              "Проверьте сохранённую разметку и повторите подтверждение.",
            )}
          </p>
          <p className="mt-2 text-[var(--app-muted)]">
            Сохранённые данные строки не потеряны. Ниже можно проверить разметку
            и повторить подтверждение отдельно.
          </p>
        </div>
      )}

      <div className="grid gap-3 border-t border-[var(--app-border)] pt-4 sm:grid-cols-[minmax(0,1fr)_auto_auto] sm:items-center">
        {canConfirmTrainingExamples ? (
          <label className="flex items-start gap-3 rounded-xl bg-[var(--app-surface)] p-3 text-sm text-[var(--app-text)]">
            <input
              type="checkbox"
              checked={confirmTrainingOnSave}
              disabled={isBusy || trainingExamplesCount === 0}
              onChange={(event) => setConfirmTrainingOnSave(event.target.checked)}
              className="mt-0.5 h-4 w-4 accent-[var(--app-accent-strong)]"
            />
            <span>
              Использовать размеченные исправления для обучения
              <span className="mt-1 block text-xs text-[var(--app-muted)]">
                {trainingExamplesCount > 0
                  ? `Будет подтверждено примеров: ${trainingExamplesCount}`
                  : "Сначала добавьте разметку в блоке обучения выше."}
              </span>
            </span>
          </label>
        ) : (
          <span />
        )}

        <AppButton
          type="button"
          variant="secondary"
          disabled={saveMutation.isPending}
          onClick={onCancel}
        >
          Отмена
        </AppButton>

        <AppButton
          type="submit"
          variant="primary"
          loading={saveMutation.isPending}
          disabled={isBusy || Boolean(metadataError)}
        >
          {saveMutation.isPending
            ? "Сохраняем и проверяем..."
            : "Сохранить и перейти дальше"}
        </AppButton>
      </div>
    </form>
  );
}
