"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useMemo, useState, type FormEvent } from "react";
import {
  addCatalogPriceCalculationLine,
  addCatalogPriceCalculationLineComponent,
  applyCatalogPriceCalculationImport,
  changeCatalogPriceCalculationLineComponentQuantity,
  changeCatalogPriceCalculationLineQuantity,
  completeCatalogPriceCalculation,
  getCatalogPriceCalculation,
  previewCatalogPriceCalculationImport,
  removeCatalogPriceCalculationLine,
  removeCatalogPriceCalculationLineComponent,
  removeCatalogPriceCalculationManufacturerDiscount,
  searchCatalogPriceCalculationProducts,
  setCatalogPriceCalculationManufacturerDiscount,
  updateCatalogPriceCalculationCard,
} from "@/features/catalogPriceCalculations/api/catalogPriceCalculationEditorApi";
import { exportCatalogPriceCalculation } from "@/features/catalogPriceCalculations/api/exportCatalogPriceCalculation";
import { getProductComponentCompatibility } from "@/features/componentCompatibility/api/getProductComponentCompatibility";
import { getCatalogProductTypes } from "@/features/catalogMetadata/api/getCatalogProductTypes";
import { CreateCatalogProductDialog } from "@/features/catalogProducts/ui/CreateCatalogProductDialog";
import { useCurrentUserAccess } from "@/features/auth/model/CurrentUserAccessContext";
import { catalogPriceCalculationQueryKeys } from "@/features/catalogPriceCalculations/model/queryKeys";
import type {
  CatalogPriceCalculationDetails,
  CatalogPriceCalculationCharacteristicImportPreviewRow,
  CatalogPriceCalculationComponentImportRowStatus,
  CatalogPriceCalculationCharacteristicImportRowStatus,
  CatalogPriceCalculationImportRowStatus,
  CatalogPriceCalculationLine,
  CatalogPriceCalculationLineComponent,
  CatalogPriceCalculationManufacturerDiscount,
  CatalogPriceCalculationProductSearchItem,
  CatalogPriceCalculationStatus,
} from "@/features/catalogPriceCalculations/model/types";
import type { CreateCatalogProductResponse } from "@/features/catalogProducts/model/types";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { formatDate, formatPrice } from "@/shared/lib/formatters";
import { AppButton } from "@/shared/ui/AppButton";
import { AppInput } from "@/shared/ui/AppInput";
import { PageWorkspace } from "@/shared/ui/PageWorkspace";

const productSearchPageSize = 20;

interface MissingCatalogProductDraft {
  article: string;
  name: string;
  manufacturerName: string | null;
  kind: "MainProduct" | "Component";
  priceAmount: number | null;
  projectQuantity: number | null;
  source: "projectImport" | "search" | "componentSearch";
}

function getStatusLabel(status: CatalogPriceCalculationStatus): string {
  switch (status) {
    case "Draft":
      return "Черновик";
    case "Completed":
      return "Завершён";
    case "Archived":
      return "В архиве";
  }
}

function formatQuantity(value: number): string {
  return new Intl.NumberFormat("ru-RU", {
    maximumFractionDigits: 3,
  }).format(value);
}

function getImportStatusLabel(
  status: CatalogPriceCalculationImportRowStatus,
): string {
  switch (status) {
    case "New":
      return "Будет добавлена";
    case "QuantityChanged":
      return "Изменится количество";
    case "Removed":
      return "Будет удалена";
    case "Unchanged":
      return "Без изменений";
    case "Invalid":
      return "Некорректная";
    case "ProductNotFound":
      return "Товар не найден";
    case "ProductAmbiguous":
      return "Несколько товаров";
    case "ActivePriceNotFound":
      return "Нет активной цены";
    case "ActivePriceAmbiguous":
      return "Несколько цен";
  }
}

function getImportStatusClassName(
  status: CatalogPriceCalculationImportRowStatus,
): string {
  if (status === "New") {
    return "border-[var(--app-success-border)] bg-[var(--app-success-soft)] text-[var(--app-success)]";
  }

  if (status === "QuantityChanged" || status === "Removed") {
    return "border-[var(--app-warning-border)] bg-[var(--app-warning-soft)] text-[var(--app-warning)]";
  }

  if (status === "Unchanged") {
    return "border-[var(--app-border)] bg-[var(--app-surface)] text-[var(--app-muted)]";
  }

  return "border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] text-[var(--app-danger)]";
}

function getComponentImportStatusLabel(
  status: CatalogPriceCalculationComponentImportRowStatus,
): string {
  switch (status) {
    case "New":
      return "Будет добавлено";
    case "QuantityChanged":
      return "Изменится количество";
    case "Removed":
      return "Будет удалено";
    case "Unchanged":
      return "Без изменений";
    case "Invalid":
      return "Некорректная строка";
    case "MainLineNotFound":
      return "Основной товар не найден";
    case "ComponentNotFound":
      return "Комплектующее не найдено";
    case "NeedNotFound":
      return "Потребность не найдена";
    case "ActivePriceNotFound":
      return "Нет активной цены";
    case "ActivePriceAmbiguous":
      return "Несколько цен";
  }
}

function getComponentImportStatusClassName(
  status: CatalogPriceCalculationComponentImportRowStatus,
): string {
  if (status === "New") {
    return "border-[var(--app-success-border)] bg-[var(--app-success-soft)] text-[var(--app-success)]";
  }

  if (status === "QuantityChanged" || status === "Removed") {
    return "border-[var(--app-warning-border)] bg-[var(--app-warning-soft)] text-[var(--app-warning)]";
  }

  if (status === "Unchanged") {
    return "border-[var(--app-border)] bg-[var(--app-surface)] text-[var(--app-muted)]";
  }

  return "border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] text-[var(--app-danger)]";
}

function getCharacteristicImportStatusLabel(
  status: CatalogPriceCalculationCharacteristicImportRowStatus,
): string {
  switch (status) {
    case "Changed":
      return "Будет изменена";
    case "Removed":
      return "Будет очищена";
    case "Unchanged":
      return "Без изменений";
    case "Invalid":
      return "Некорректное значение";
    case "ProductNotFound":
      return "Товар не найден";
    case "CharacteristicNotFound":
      return "Характеристика не найдена";
  }
}

function getCharacteristicImportStatusClassName(
  status: CatalogPriceCalculationCharacteristicImportRowStatus,
): string {
  if (status === "Changed" || status === "Removed") {
    return "border-[var(--app-warning-border)] bg-[var(--app-warning-soft)] text-[var(--app-warning)]";
  }

  if (status === "Unchanged") {
    return "border-[var(--app-border)] bg-[var(--app-surface)] text-[var(--app-muted)]";
  }

  return "border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] text-[var(--app-danger)]";
}

function formatCharacteristicImportValue(
  value: string | null,
  dataType: string,
): string {
  if (value === null || value.trim().length === 0) {
    return "Не задано";
  }

  if (dataType === "Boolean") {
    if (value.toLowerCase() === "true") {
      return "Да";
    }

    if (value.toLowerCase() === "false") {
      return "Нет";
    }
  }

  return value;
}

function StatusBadge({ status }: { status: CatalogPriceCalculationStatus }) {
  const className =
    status === "Draft"
      ? "border-[var(--app-warning-border)] bg-[var(--app-warning-soft)] text-[var(--app-warning)]"
      : status === "Completed"
        ? "border-[var(--app-success-border)] bg-[var(--app-success-soft)] text-[var(--app-success)]"
        : "border-[var(--app-border)] bg-[var(--app-surface)] text-[var(--app-muted)]";

  return (
    <span
      className={`inline-flex rounded-full border px-3 py-1 text-xs font-semibold ${className}`}
    >
      {getStatusLabel(status)}
    </span>
  );
}

function ProductSearchRow({
  product,
  currency,
  isAdding,
  onAdd,
}: {
  product: CatalogPriceCalculationProductSearchItem;
  currency: string;
  isAdding: boolean;
  onAdd: (productId: string, quantity: number) => void;
}) {
  const [quantity, setQuantity] = useState("1");
  const isAvailable = product.priceStatus === "Available";
  const parsedQuantity = Number(quantity.replace(",", "."));
  const unavailableReason =
    product.priceStatus === "ActivePriceAmbiguous"
      ? "Найдено несколько активных цен"
      : "Нет активной цены";

  return (
    <tr className="bg-[var(--app-panel)] align-top">
      <td className="px-4 py-4">
        <p className="font-medium text-[var(--app-text)]">{product.name}</p>

        <p className="mt-1 text-xs text-[var(--app-muted)]">
          {product.article}
        </p>
      </td>

      <td className="px-4 py-4 text-[var(--app-muted)]">
        {product.manufacturerName}
      </td>

      <td className="whitespace-nowrap px-4 py-4 tabular-nums text-[var(--app-text)]">
        {product.basePriceAmount === null
          ? "—"
          : formatPrice(product.basePriceAmount, currency)}
        <p className="mt-1 text-xs font-normal text-[var(--app-muted)]">
          {product.priceListRowId ? "Из активного прайса" : "Из карточки каталога"}
        </p>
      </td>

      <td className="whitespace-nowrap px-4 py-4 tabular-nums text-[var(--app-muted)]">
        {product.mrcPriceAmount === null || product.mrcPriceAmount === undefined
          ? "—"
          : formatPrice(product.mrcPriceAmount, currency)}
      </td>

      <td className="px-4 py-4">
        {isAvailable ? (
          <div className="flex min-w-[190px] items-center gap-2">
            <AppInput
              type="number"
              min="0.001"
              step="0.001"
              value={quantity}
              aria-label={`Количество ${product.name} в проекте`}
              disabled={isAdding}
              onChange={(event) => setQuantity(event.target.value)}
              className="w-24"
            />
            <AppButton
              type="button"
              variant="primary"
              size="sm"
              loading={isAdding}
              disabled={!Number.isFinite(parsedQuantity) || parsedQuantity <= 0}
              onClick={() => onAdd(product.productId, parsedQuantity)}
            >
              Добавить
            </AppButton>
          </div>
        ) : (
          <span className="inline-flex max-w-56 rounded-xl border border-[var(--app-warning-border)] bg-[var(--app-warning-soft)] px-3 py-2 text-xs font-medium leading-5 text-[var(--app-warning)]">
            {unavailableReason}
          </span>
        )}
      </td>
    </tr>
  );
}

function CalculationComponentRow({
  component,
  currency,
  editable,
  isChanging,
  isRemoving,
  onChangeQuantity,
  onRemove,
}: {
  component: CatalogPriceCalculationLineComponent;
  currency: string;
  editable: boolean;
  isChanging: boolean;
  isRemoving: boolean;
  onChangeQuantity: (componentLineId: string, quantityPerUnit: number) => void;
  onRemove: (component: CatalogPriceCalculationLineComponent) => void;
}) {
  const [quantityPerUnit, setQuantityPerUnit] = useState(
    component.quantityPerUnit.toString(),
  );

  const parsedQuantity = Number(quantityPerUnit);

  return (
    <tr className="bg-[var(--app-surface)] align-top">
      <td className="border-l-4 border-l-[var(--app-accent)] px-4 py-4 pl-8">
        <span className="inline-flex rounded-full border border-[var(--app-accent-border)] bg-[var(--app-accent-soft)] px-2 py-0.5 text-[11px] font-semibold text-[var(--app-accent)]">
          Комплектующее
        </span>
        <span
          className={`ml-2 inline-flex rounded-full border px-2 py-0.5 text-[11px] font-semibold ${
            component.selectionSource === "Manual"
              ? "border-[var(--app-warning-border)] bg-[var(--app-warning-soft)] text-[var(--app-warning)]"
              : "border-[var(--app-success-border)] bg-[var(--app-success-soft)] text-[var(--app-success)]"
          }`}
        >
          {component.selectionSource === "Manual"
            ? "Выбрано вручную"
            : "Совместимость подтверждена"}
        </span>
        <p className="mt-2 font-medium text-[var(--app-text)]">
          {component.name}
        </p>
        <p className="mt-1 text-xs text-[var(--app-muted)]">
          {component.article} · Закрывает: {component.needName}
        </p>
      </td>

      <td className="px-4 py-4 text-[var(--app-muted)]">
        {component.manufacturerName}
      </td>

      <td className="px-4 py-4">
        {editable ? (
          <div className="flex min-w-[190px] items-center gap-2">
            <AppInput
              type="number"
              min="1"
              max="1000000"
              step="1"
              value={quantityPerUnit}
              disabled={isChanging || isRemoving}
              onChange={(event) => setQuantityPerUnit(event.target.value)}
              className="max-w-24"
            />
            <AppButton
              type="button"
              variant="secondary"
              size="sm"
              loading={isChanging}
              disabled={
                !Number.isInteger(parsedQuantity) ||
                parsedQuantity < 1 ||
                parsedQuantity === component.quantityPerUnit
              }
              onClick={() =>
                onChangeQuantity(component.componentLineId, parsedQuantity)
              }
            >
              Сохранить
            </AppButton>
          </div>
        ) : (
          <span className="tabular-nums text-[var(--app-text)]">
            {component.quantityPerUnit} на единицу
          </span>
        )}
        <p className="mt-1 text-xs text-[var(--app-muted)]">
          Всего: {formatQuantity(component.totalQuantity)}
        </p>
      </td>

      <td className="px-4 py-4 text-[var(--app-muted)]">—</td>
      <td className="px-4 py-4 text-[var(--app-muted)]">—</td>
      <td className="whitespace-nowrap px-4 py-4 tabular-nums text-[var(--app-text)]">
        {formatPrice(component.basePriceAmount, currency)}
      </td>
      <td className="px-4 py-4 text-[var(--app-muted)]">—</td>
      <td className="whitespace-nowrap px-4 py-4 tabular-nums text-[var(--app-text)]">
        {component.discountPercent.toFixed(2)}%
      </td>
      <td className="whitespace-nowrap px-4 py-4 tabular-nums text-[var(--app-text)]">
        {formatPrice(component.projectPriceAmount, currency)}
      </td>
      <td className="whitespace-nowrap px-4 py-4 font-semibold tabular-nums text-[var(--app-text)]">
        {formatPrice(component.totalAmount, currency)}
      </td>

      {editable && (
        <td className="px-4 py-4">
          <AppButton
            type="button"
            variant="danger"
            size="sm"
            loading={isRemoving}
            disabled={isChanging}
            onClick={() => onRemove(component)}
          >
            Удалить
          </AppButton>
        </td>
      )}
    </tr>
  );
}

function ComponentPickerRow({
  calculationId,
  line,
  currency,
  editable,
  addingKey,
  onAdd,
  canCreateProducts,
  onCreateComponent,
  onClose,
}: {
  calculationId: string;
  line: CatalogPriceCalculationLine;
  currency: string;
  editable: boolean;
  addingKey?: string;
  onAdd: (
    needDefinitionId: string,
    componentProductId: string,
    quantityPerUnit: number,
    manualSelection: boolean,
  ) => void;
  canCreateProducts: boolean;
  onCreateComponent: (article: string) => void;
  onClose: () => void;
}) {
  const [quantities, setQuantities] = useState<Record<string, string>>({});
  const [mode, setMode] = useState<"recommended" | "catalog">("recommended");
  const [manualNeedId, setManualNeedId] = useState("");
  const [manualSearchInput, setManualSearchInput] = useState("");
  const [manualSearch, setManualSearch] = useState("");
  const [manualProductTypeCode, setManualProductTypeCode] = useState("");
  const [manualPage, setManualPage] = useState(1);
  const compatibilityQuery = useQuery({
    queryKey: ["product-component-compatibility", line.productId],
    queryFn: () => getProductComponentCompatibility(line.productId),
  });
  const needs = compatibilityQuery.data?.needs ?? [];
  const effectiveMode = needs.length === 0 ? "catalog" : mode;
  const effectiveManualNeedId = manualNeedId || needs[0]?.needDefinitionId || "";
  const productTypesQuery = useQuery({
    queryKey: ["catalog-product-types"],
    queryFn: getCatalogProductTypes,
    enabled: mode === "catalog",
  });
  const manualProductsQuery = useQuery({
    queryKey: [
      "price-calculation-component-search",
      calculationId,
      manualSearch,
      manualProductTypeCode,
      manualPage,
    ],
    queryFn: () =>
      searchCatalogPriceCalculationProducts({
        calculationId,
        search: manualSearch,
        page: manualPage,
        pageSize: 20,
        productKind: "Component",
        productTypeCode: manualProductTypeCode || undefined,
      }),
    enabled: mode === "catalog" && Boolean(effectiveManualNeedId),
  });
  const columnCount = editable ? 11 : 10;
  const componentProductTypes = (productTypesQuery.data ?? []).filter(
    (productType) => productType.kind === "Component",
  );
  const manualTotalPages = Math.max(1, manualProductsQuery.data?.totalPages ?? 0);

  return (
    <tr className="bg-[var(--app-panel)]">
      <td colSpan={columnCount} className="px-4 py-4">
        <div className="rounded-2xl border border-[var(--app-accent-border)] bg-[var(--app-accent-soft)] p-4">
          <div className="flex items-start justify-between gap-4">
            <div>
              <h3 className="font-semibold text-[var(--app-text)]">
                Добавить комплектующее
              </h3>
              <p className="mt-1 text-sm text-[var(--app-muted)]">
                Количество задаётся на одну единицу основного товара.
              </p>
            </div>
            <AppButton type="button" variant="ghost" size="sm" onClick={onClose}>
              Закрыть
            </AppButton>
          </div>

          <div className="mt-4 flex flex-wrap gap-2">
            <AppButton
              type="button"
              variant={effectiveMode === "recommended" ? "primary" : "secondary"}
              size="sm"
              disabled={needs.length === 0}
              onClick={() => setMode("recommended")}
            >
              Рекомендуемые
            </AppButton>
            <AppButton
              type="button"
              variant={effectiveMode === "catalog" ? "primary" : "secondary"}
              size="sm"
              onClick={() => setMode("catalog")}
            >
              Найти в каталоге
            </AppButton>
          </div>

          {compatibilityQuery.isLoading && (
            <p className="mt-4 text-sm text-[var(--app-muted)]">
              Загружаем совместимые позиции...
            </p>
          )}

          {compatibilityQuery.isError && (
            <p className="mt-4 text-sm text-[var(--app-danger)]">
              {getApiErrorMessage(
                compatibilityQuery.error,
                "Не удалось получить совместимые комплектующие.",
              )}
            </p>
          )}

          {compatibilityQuery.data && compatibilityQuery.data.needs.length === 0 && (
            <p className="mt-4 text-sm text-[var(--app-muted)]">
              Для этого типа товара потребности в комплектующих не настроены.
            </p>
          )}

          {effectiveMode === "recommended" && (
            <div className="mt-4 grid gap-4">
            {needs.map((need) => {
              const availableComponents = need.compatibleComponents.filter(
                (candidate) =>
                  !line.components.some(
                    (component) =>
                      component.needDefinitionId === need.needDefinitionId &&
                      component.componentProductId === candidate.productId,
                  ),
              );

              return (
                <section
                  key={need.needDefinitionId}
                  className="rounded-xl border border-[var(--app-border)] bg-[var(--app-panel)] p-4"
                >
                  <h4 className="font-semibold text-[var(--app-text)]">
                    {need.name}
                  </h4>

                  {availableComponents.length === 0 ? (
                    <p className="mt-2 text-sm text-[var(--app-muted)]">
                      Подходящих новых комплектующих нет.
                    </p>
                  ) : (
                    <div className="mt-3 grid gap-2">
                      {availableComponents.map((candidate) => {
                        const candidateKey = `${need.needDefinitionId}:${candidate.productId}`;
                        const quantityValue = quantities[candidateKey] ?? "1";
                        const quantity = Number(quantityValue);

                        return (
                          <div
                            key={candidate.productId}
                            className="flex flex-col gap-3 rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] p-3 lg:flex-row lg:items-center lg:justify-between"
                          >
                            <div className="min-w-0">
                              <p className="font-medium text-[var(--app-text)]">
                                {candidate.name}
                              </p>
                              <p className="mt-1 text-xs text-[var(--app-muted)]">
                                {candidate.article} · {formatPrice(
                                  candidate.priceAmount,
                                  candidate.priceCurrency || currency,
                                )}
                              </p>
                            </div>

                            <div className="flex items-center gap-2">
                              <AppInput
                                type="number"
                                min="1"
                                max="1000000"
                                step="1"
                                value={quantityValue}
                                aria-label={`Количество ${candidate.name} на единицу товара`}
                                onChange={(event) =>
                                  setQuantities((current) => ({
                                    ...current,
                                    [candidateKey]: event.target.value,
                                  }))
                                }
                                className="w-24"
                              />
                              <AppButton
                                type="button"
                                variant="primary"
                                size="sm"
                                loading={addingKey === candidateKey}
                                disabled={!Number.isInteger(quantity) || quantity < 1}
                                onClick={() =>
                                  onAdd(
                                    need.needDefinitionId,
                                    candidate.productId,
                                    quantity,
                                    false,
                                  )
                                }
                              >
                                Добавить
                              </AppButton>
                            </div>
                          </div>
                        );
                      })}
                    </div>
                  )}
                </section>
              );
            })}
            </div>
          )}

          {effectiveMode === "catalog" && (
            <div className="mt-4 grid gap-4 rounded-xl border border-[var(--app-border)] bg-[var(--app-panel)] p-4">
              <div className="rounded-xl border border-[var(--app-warning-border)] bg-[var(--app-warning-soft)] px-4 py-3 text-sm text-[var(--app-warning)]">
                Ручной поиск не подтверждает совместимость. Добавленная позиция будет отмечена как выбранная вручную.
              </div>

              {needs.length === 0 ? (
                <div className="grid gap-3">
                  <div className="rounded-xl border border-[var(--app-warning-border)] bg-[var(--app-warning-soft)] p-4">
                    <p className="font-medium text-[var(--app-warning)]">
                      Комплектующее пока нельзя привязать к этому товару
                    </p>
                    <p className="mt-2 text-sm leading-6 text-[var(--app-muted)]">
                      Для этого типа товара не настроено ни одного назначения
                      комплектующего. Назначение задаётся один раз для всего
                      типа — например «Дополнительный контакт» или «Катушка».
                      После настройки здесь появятся поиск и рекомендации.
                    </p>
                  </div>
                  {canCreateProducts && (
                    <div className="flex flex-col gap-3 sm:flex-row">
                      <AppInput
                        value={manualSearchInput}
                        placeholder="Артикул нового комплектующего"
                        onChange={(event) =>
                          setManualSearchInput(event.target.value)
                        }
                      />
                      <AppButton
                        type="button"
                        variant="secondary"
                        disabled={manualSearchInput.trim().length === 0}
                        onClick={() =>
                          onCreateComponent(manualSearchInput.trim())
                        }
                      >
                        Создать карточку комплектующего
                      </AppButton>
                    </div>
                  )}
                </div>
              ) : (
                <>
                  <div className="grid gap-3 lg:grid-cols-[minmax(220px,0.8fr)_minmax(260px,1fr)_minmax(220px,0.8fr)_auto]">
                    <select
                      value={effectiveManualNeedId}
                      onChange={(event) => {
                        setManualNeedId(event.target.value);
                        setManualPage(1);
                      }}
                      aria-label="Какую потребность закрывает комплектующее"
                      className="min-h-11 rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] px-3 text-sm text-[var(--app-text)]"
                    >
                      {needs.map((need) => (
                        <option key={need.needDefinitionId} value={need.needDefinitionId}>
                          {need.name}
                        </option>
                      ))}
                    </select>

                    <AppInput
                      value={manualSearchInput}
                      placeholder="Название, артикул или производитель"
                      onChange={(event) => setManualSearchInput(event.target.value)}
                      onKeyDown={(event) => {
                        if (event.key === "Enter") {
                          event.preventDefault();
                          setManualSearch(manualSearchInput.trim());
                          setManualPage(1);
                        }
                      }}
                    />

                    <select
                      value={manualProductTypeCode}
                      onChange={(event) => {
                        setManualProductTypeCode(event.target.value);
                        setManualPage(1);
                      }}
                      aria-label="Тип комплектующего"
                      className="min-h-11 rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] px-3 text-sm text-[var(--app-text)]"
                    >
                      <option value="">Все типы комплектующих</option>
                      {componentProductTypes.map((productType) => (
                        <option key={productType.id} value={productType.code}>
                          {productType.name}
                        </option>
                      ))}
                    </select>

                    <AppButton
                      type="button"
                      variant="secondary"
                      onClick={() => {
                        setManualSearch(manualSearchInput.trim());
                        setManualPage(1);
                      }}
                    >
                      Найти
                    </AppButton>
                  </div>

                  {manualProductsQuery.isLoading && (
                    <p className="text-sm text-[var(--app-muted)]">Ищем комплектующие...</p>
                  )}

                  {manualProductsQuery.isError && (
                    <p className="text-sm text-[var(--app-danger)]">
                      {getApiErrorMessage(manualProductsQuery.error, "Не удалось найти комплектующие.")}
                    </p>
                  )}

                  {manualProductsQuery.data?.items.length === 0 && (
                    <div className="flex flex-col gap-3 rounded-xl border border-dashed border-[var(--app-border-strong)] p-4 sm:flex-row sm:items-center sm:justify-between">
                      <p className="text-sm text-[var(--app-muted)]">
                        По заданным условиям ничего не найдено.
                      </p>
                      {canCreateProducts && manualSearch.trim().length > 0 && (
                        <AppButton
                          type="button"
                          variant="secondary"
                          size="sm"
                          onClick={() => onCreateComponent(manualSearch.trim())}
                        >
                          Добавить комплектующее в каталог
                        </AppButton>
                      )}
                    </div>
                  )}

                  <div className="grid gap-2">
                    {manualProductsQuery.data?.items.map((candidate) => {
                      const candidateKey = `manual:${effectiveManualNeedId}:${candidate.productId}`;
                      const quantityValue = quantities[candidateKey] ?? "1";
                      const quantity = Number(quantityValue);
                      const isAlreadyAdded = line.components.some(
                        (component) =>
                          component.needDefinitionId === effectiveManualNeedId &&
                          component.componentProductId === candidate.productId,
                      );
                      const hasActivePrice = candidate.priceStatus === "Available";

                      return (
                        <div
                          key={candidate.productId}
                          className="flex flex-col gap-3 rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] p-3 lg:flex-row lg:items-center lg:justify-between"
                        >
                          <div className="min-w-0">
                            <p className="font-medium text-[var(--app-text)]">{candidate.name}</p>
                            <p className="mt-1 text-xs text-[var(--app-muted)]">
                              {candidate.article} · {candidate.manufacturerName} · {candidate.productTypeName} · {candidate.basePriceAmount === null
                                ? candidate.priceStatus === "ActivePriceAmbiguous"
                                  ? "несколько активных цен"
                                  : "нет активной цены"
                                : formatPrice(candidate.basePriceAmount, currency)}
                            </p>
                          </div>

                          <div className="flex items-center gap-2">
                            <AppInput
                              type="number"
                              min="1"
                              max="1000000"
                              step="1"
                              value={quantityValue}
                              aria-label={`Количество ${candidate.name} на единицу товара`}
                              onChange={(event) =>
                                setQuantities((current) => ({
                                  ...current,
                                  [candidateKey]: event.target.value,
                                }))
                              }
                              className="w-24"
                            />
                            <AppButton
                              type="button"
                              variant="primary"
                              size="sm"
                              loading={addingKey === candidateKey}
                              disabled={isAlreadyAdded || !hasActivePrice || !Number.isInteger(quantity) || quantity < 1}
                              onClick={() => onAdd(effectiveManualNeedId, candidate.productId, quantity, true)}
                            >
                              {isAlreadyAdded
                                ? "Добавлено"
                                : hasActivePrice
                                  ? "Добавить"
                                  : "Недоступно"}
                            </AppButton>
                          </div>
                        </div>
                      );
                    })}
                  </div>

                  {(manualProductsQuery.data?.totalPages ?? 0) > 1 && (
                    <div className="flex items-center justify-between gap-3">
                      <AppButton
                        type="button"
                        variant="secondary"
                        size="sm"
                        disabled={manualPage <= 1}
                        onClick={() => setManualPage((page) => Math.max(1, page - 1))}
                      >
                        Назад
                      </AppButton>
                      <span className="text-xs text-[var(--app-muted)]">
                        Страница {manualPage} из {manualTotalPages}
                      </span>
                      <AppButton
                        type="button"
                        variant="secondary"
                        size="sm"
                        disabled={manualPage >= manualTotalPages}
                        onClick={() => setManualPage((page) => Math.min(manualTotalPages, page + 1))}
                      >
                        Вперёд
                      </AppButton>
                    </div>
                  )}
                </>
              )}
            </div>
          )}
        </div>
      </td>
    </tr>
  );
}

function CalculationLineRow({
  calculationId,
  line,
  currency,
  editable,
  isChanging,
  isRemoving,
  changingComponentLineId,
  removingComponentLineId,
  addingComponentKey,
  onChangeQuantity,
  onRemove,
  onAddComponent,
  canCreateProducts,
  onCreateComponent,
  onChangeComponentQuantity,
  onRemoveComponent,
}: {
  calculationId: string;
  line: CatalogPriceCalculationLine;
  currency: string;
  editable: boolean;
  isChanging: boolean;
  isRemoving: boolean;
  changingComponentLineId?: string;
  removingComponentLineId?: string;
  addingComponentKey?: string;
  onChangeQuantity: (lineId: string, quantity: number) => void;
  onRemove: (line: CatalogPriceCalculationLine) => void;
  onAddComponent: (
    lineId: string,
    needDefinitionId: string,
    componentProductId: string,
    quantityPerUnit: number,
    manualSelection: boolean,
  ) => void;
  canCreateProducts: boolean;
  onCreateComponent: (article: string) => void;
  onChangeComponentQuantity: (
    lineId: string,
    componentLineId: string,
    quantityPerUnit: number,
  ) => void;
  onRemoveComponent: (
    lineId: string,
    component: CatalogPriceCalculationLineComponent,
  ) => void;
}) {
  const [quantity, setQuantity] = useState(line.quantity.toString());
  const [showComponentPicker, setShowComponentPicker] = useState(false);

  function handleSave(): void {
    const parsedQuantity = Number(quantity.replace(",", "."));

    if (!Number.isFinite(parsedQuantity) || parsedQuantity <= 0) {
      return;
    }

    onChangeQuantity(line.lineId, parsedQuantity);
  }

  return (
    <>
      <tr className="bg-[var(--app-panel)] align-top">
        <td className="px-4 py-4">
          <span className="inline-flex rounded-full border border-[var(--app-border)] bg-[var(--app-surface)] px-2 py-0.5 text-[11px] font-semibold text-[var(--app-muted)]">
            Основной товар
          </span>
          <p className="mt-2 font-medium text-[var(--app-text)]">{line.name}</p>
          <p className="mt-1 text-xs text-[var(--app-muted)]">{line.article}</p>

          {editable && (
            <AppButton
              type="button"
              variant="secondary"
              size="sm"
              className="mt-3"
              onClick={() => setShowComponentPicker((current) => !current)}
            >
              {showComponentPicker ? "Закрыть подбор" : "Добавить комплектующее"}
            </AppButton>
          )}
        </td>

        <td className="px-4 py-4 text-[var(--app-muted)]">
          {line.manufacturerName}
        </td>

        <td className="px-4 py-4">
          {editable ? (
            <div className="flex min-w-[170px] items-center gap-2">
              <AppInput
                type="number"
                min="0.001"
                step="0.001"
                value={quantity}
                disabled={isChanging || isRemoving}
                onChange={(event) => setQuantity(event.target.value)}
                className="max-w-24"
              />
              <AppButton
                type="button"
                variant="secondary"
                size="sm"
                loading={isChanging}
                disabled={Number(quantity.replace(",", ".")) === line.quantity}
                onClick={handleSave}
              >
                Сохранить
              </AppButton>
            </div>
          ) : (
            <span className="tabular-nums text-[var(--app-text)]">
              {formatQuantity(line.quantity)}
            </span>
          )}
          {line.unit && (
            <p className="mt-1 text-xs text-[var(--app-muted)]">
              Единица: {line.unit}
            </p>
          )}
        </td>

        <td className="whitespace-nowrap px-4 py-4 tabular-nums text-[var(--app-text)]">
          {formatQuantity(line.stockQuantity)}
        </td>
        <td
          className={`whitespace-nowrap px-4 py-4 font-semibold tabular-nums ${
            line.shortageQuantity > 0
              ? "text-[var(--app-danger)]"
              : "text-[var(--app-success)]"
          }`}
        >
          {line.shortageQuantity > 0
            ? formatQuantity(line.shortageQuantity)
            : "Достаточно"}
        </td>
        <td className="whitespace-nowrap px-4 py-4 tabular-nums text-[var(--app-text)]">
          {formatPrice(line.basePriceAmount, currency)}
        </td>
        <td className="whitespace-nowrap px-4 py-4 tabular-nums text-[var(--app-muted)]">
          {line.mrcPriceAmount === null || line.mrcPriceAmount === undefined
            ? "—"
            : formatPrice(line.mrcPriceAmount, currency)}
        </td>
        <td className="whitespace-nowrap px-4 py-4 tabular-nums text-[var(--app-text)]">
          {line.discountPercent.toFixed(2)}%
        </td>
        <td className="whitespace-nowrap px-4 py-4 tabular-nums text-[var(--app-text)]">
          {formatPrice(line.projectPriceAmount, currency)}
        </td>
        <td className="whitespace-nowrap px-4 py-4 font-semibold tabular-nums text-[var(--app-text)]">
          {formatPrice(line.totalAmount, currency)}
          {line.componentsTotalAmount > 0 && (
            <p className="mt-1 text-xs font-normal text-[var(--app-muted)]">
              Товар: {formatPrice(line.productTotalAmount, currency)} · Комплектующие: {formatPrice(line.componentsTotalAmount, currency)}
            </p>
          )}
        </td>

        {editable && (
          <td className="px-4 py-4">
            <AppButton
              type="button"
              variant="danger"
              size="sm"
              loading={isRemoving}
              disabled={isChanging}
              onClick={() => onRemove(line)}
            >
              Удалить
            </AppButton>
          </td>
        )}
      </tr>

      {line.components.map((component) => (
        <CalculationComponentRow
          key={`${component.componentLineId}-${component.quantityPerUnit}`}
          component={component}
          currency={currency}
          editable={editable}
          isChanging={changingComponentLineId === component.componentLineId}
          isRemoving={removingComponentLineId === component.componentLineId}
          onChangeQuantity={(componentLineId, quantityPerUnit) =>
            onChangeComponentQuantity(
              line.lineId,
              componentLineId,
              quantityPerUnit,
            )
          }
          onRemove={(selectedComponent) =>
            onRemoveComponent(line.lineId, selectedComponent)
          }
        />
      ))}

      {showComponentPicker && (
        <ComponentPickerRow
          calculationId={calculationId}
          line={line}
          currency={currency}
          editable={editable}
          addingKey={addingComponentKey}
          onAdd={(needDefinitionId, componentProductId, quantityPerUnit, manualSelection) =>
            onAddComponent(
              line.lineId,
              needDefinitionId,
              componentProductId,
              quantityPerUnit,
              manualSelection,
            )
          }
          canCreateProducts={canCreateProducts}
          onCreateComponent={onCreateComponent}
          onClose={() => setShowComponentPicker(false)}
        />
      )}
    </>
  );
}

function ManufacturerDiscountEditor({
  manufacturerId,
  manufacturerName,
  discount,
  disabled,
  isSaving,
  isRemoving,
  onSave,
  onRemove,
}: {
  manufacturerId: string;
  manufacturerName: string;
  discount?: CatalogPriceCalculationManufacturerDiscount;
  disabled: boolean;
  isSaving: boolean;
  isRemoving: boolean;
  onSave: (manufacturerId: string, discountPercent: number) => void;
  onRemove: (manufacturerId: string) => void;
}) {
  const [value, setValue] = useState(
    discount?.discountPercent.toString() ?? "0",
  );

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    const discountPercent = Number(value.replace(",", "."));

    if (
      !Number.isFinite(discountPercent) ||
      discountPercent < 0 ||
      discountPercent > 100
    ) {
      return;
    }

    onSave(manufacturerId, discountPercent);
  }

  return (
    <form
      onSubmit={handleSubmit}
      className="rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4"
    >
      <p className="font-semibold text-[var(--app-text)]">{manufacturerName}</p>

      <p className="mt-1 text-xs text-[var(--app-muted)]">
        Значение 60% означает оплату 40% базовой цены.
      </p>

      <div className="mt-4 flex items-end gap-2">
        <label className="grid min-w-0 flex-1 gap-2">
          <span className="text-sm text-[var(--app-muted)]">Скидка, %</span>

          <AppInput
            type="number"
            min="0"
            max="100"
            step="0.01"
            value={value}
            disabled={disabled || isSaving || isRemoving}
            onChange={(event) => setValue(event.target.value)}
          />
        </label>

        <AppButton
          type="submit"
          variant="primary"
          size="sm"
          loading={isSaving}
          disabled={disabled || isRemoving}
        >
          Применить
        </AppButton>

        {discount && (
          <AppButton
            type="button"
            variant="danger"
            size="sm"
            loading={isRemoving}
            disabled={disabled || isSaving}
            onClick={() => onRemove(manufacturerId)}
          >
            Сбросить
          </AppButton>
        )}
      </div>
    </form>
  );
}

function ProjectCardEditor({
  calculation,
  editable,
  isSaving,
  isSaved,
  onSave,
}: {
  calculation: CatalogPriceCalculationDetails;
  editable: boolean;
  isSaving: boolean;
  isSaved: boolean;
  onSave: (values: {
    customerName: string | null;
    objectName: string | null;
    projectNumber: string | null;
    responsibleName: string | null;
    comment: string | null;
    validUntil: string | null;
  }) => void;
}) {
  const [customerName, setCustomerName] = useState(
    calculation.customerName ?? "",
  );
  const [objectName, setObjectName] = useState(calculation.objectName ?? "");
  const [projectNumber, setProjectNumber] = useState(
    calculation.projectNumber ?? "",
  );
  const [responsibleName, setResponsibleName] = useState(
    calculation.responsibleName ?? "",
  );
  const [comment, setComment] = useState(calculation.comment ?? "");
  const [validUntil, setValidUntil] = useState(calculation.validUntil ?? "");
  const [hasChanges, setHasChanges] = useState(false);

  function normalize(value: string): string | null {
    const normalizedValue = value.trim();

    return normalizedValue.length > 0 ? normalizedValue : null;
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    onSave({
      customerName: normalize(customerName),
      objectName: normalize(objectName),
      projectNumber: normalize(projectNumber),
      responsibleName: normalize(responsibleName),
      comment: normalize(comment),
      validUntil: normalize(validUntil),
    });
  }

  return (
    <section
      aria-labelledby="project-card-title"
      className="rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5 sm:p-6"
    >
      <h2
        id="project-card-title"
        className="text-xl font-semibold text-[var(--app-text)]"
      >
        Карточка проекта
      </h2>

      <p className="mt-2 text-sm leading-6 text-[var(--app-muted)]">
        Реквизиты попадут в итоговый Excel и помогут идентифицировать
        предложение.
      </p>

      <form
        onSubmit={handleSubmit}
        onChange={() => setHasChanges(true)}
        className="mt-5 grid gap-4"
      >
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          <label className="grid gap-2">
            <span className="text-sm font-medium text-[var(--app-text)]">
              Заказчик
            </span>

            <AppInput
              value={customerName}
              maxLength={200}
              disabled={!editable || isSaving}
              onChange={(event) => setCustomerName(event.target.value)}
              placeholder="Название организации или ФИО"
            />
          </label>

          <label className="grid gap-2">
            <span className="text-sm font-medium text-[var(--app-text)]">
              Объект
            </span>

            <AppInput
              value={objectName}
              maxLength={300}
              disabled={!editable || isSaving}
              onChange={(event) => setObjectName(event.target.value)}
              placeholder="Название или адрес объекта"
            />
          </label>

          <label className="grid gap-2">
            <span className="text-sm font-medium text-[var(--app-text)]">
              Номер проекта
            </span>

            <AppInput
              value={projectNumber}
              maxLength={100}
              disabled={!editable || isSaving}
              onChange={(event) => setProjectNumber(event.target.value)}
              placeholder="Например, ПР-2026-019"
            />
          </label>

          <label className="grid gap-2">
            <span className="text-sm font-medium text-[var(--app-text)]">
              Ответственный
            </span>

            <AppInput
              value={responsibleName}
              maxLength={200}
              disabled={!editable || isSaving}
              onChange={(event) => setResponsibleName(event.target.value)}
              placeholder="ФИО сотрудника"
            />
          </label>

          <label className="grid gap-2">
            <span className="text-sm font-medium text-[var(--app-text)]">
              Предложение действительно до
            </span>

            <AppInput
              type="date"
              value={validUntil}
              disabled={!editable || isSaving}
              onChange={(event) => setValidUntil(event.target.value)}
            />
          </label>
        </div>

        <label className="grid gap-2">
          <span className="text-sm font-medium text-[var(--app-text)]">
            Комментарий
          </span>

          <textarea
            value={comment}
            maxLength={2000}
            rows={4}
            disabled={!editable || isSaving}
            onChange={(event) => setComment(event.target.value)}
            placeholder="Условия, примечания или дополнительная информация"
            className="w-full rounded-xl border border-[var(--app-input-border)] bg-[var(--app-input-bg)] px-4 py-3 text-sm text-[var(--app-text)] outline-none transition placeholder:text-[var(--app-muted)] focus:border-[var(--app-accent)] disabled:cursor-not-allowed disabled:opacity-60"
          />
        </label>

        {editable && (
          <div className="flex flex-wrap items-center gap-4">
            <AppButton
              type="submit"
              variant="primary"
              loading={isSaving}
              disabled={!hasChanges}
              className="w-fit"
            >
              Сохранить карточку
            </AppButton>

            {isSaved && !hasChanges && (
              <p
                role="status"
                className="text-sm font-medium text-[var(--app-success)]"
              >
                Карточка сохранена.
              </p>
            )}
          </div>
        )}
      </form>
    </section>
  );
}

export default function CatalogPriceCalculationPage() {
  const params = useParams<{ calculationId: string }>();
  const queryClient = useQueryClient();
  const { hasPermission } = useCurrentUserAccess();
  const canExport = hasPermission("PriceCalculationsExport");
  const canEditProducts = hasPermission("ProductsEdit");

  const calculationId = params.calculationId ?? "";

  const [search, setSearch] = useState("");
  const [appliedSearch, setAppliedSearch] = useState("");
  const [searchPage, setSearchPage] = useState(1);
  const [projectImportFile, setProjectImportFile] = useState<File | null>(null);
  const [importPreviewSection, setImportPreviewSection] = useState<
    "positions" | "components" | "characteristics"
  >("positions");
  const [showUnchangedCharacteristics, setShowUnchangedCharacteristics] =
    useState(false);
  const [missingProductDraft, setMissingProductDraft] =
    useState<MissingCatalogProductDraft | null>(null);

  const calculationQuery = useQuery({
    queryKey: catalogPriceCalculationQueryKeys.details(calculationId),
    queryFn: () => getCatalogPriceCalculation(calculationId),
    enabled: calculationId.length > 0,
  });

  const calculation = calculationQuery.data;
  const editable = calculation?.status === "Draft";

  const productsQuery = useQuery({
    queryKey: catalogPriceCalculationQueryKeys.products(
      calculationId,
      appliedSearch,
      searchPage,
      productSearchPageSize,
    ),
    queryFn: () =>
      searchCatalogPriceCalculationProducts({
        calculationId,
        search: appliedSearch,
        page: searchPage,
        pageSize: productSearchPageSize,
      }),
    enabled: calculationId.length > 0 && editable && appliedSearch.length > 0,
    placeholderData: (previousData) => previousData,
  });

  async function refreshCalculation(): Promise<void> {
    await Promise.all([
      queryClient.invalidateQueries({
        queryKey: catalogPriceCalculationQueryKeys.details(calculationId),
      }),
      queryClient.invalidateQueries({
        queryKey: catalogPriceCalculationQueryKeys.listRoot,
      }),
    ]);
  }

  const addLineMutation = useMutation({
    mutationFn: addCatalogPriceCalculationLine,
    onSuccess: refreshCalculation,
  });

  const quantityMutation = useMutation({
    mutationFn: changeCatalogPriceCalculationLineQuantity,
    onSuccess: refreshCalculation,
  });

  const removeLineMutation = useMutation({
    mutationFn: removeCatalogPriceCalculationLine,
    onSuccess: refreshCalculation,
  });

  const addComponentMutation = useMutation({
    mutationFn: addCatalogPriceCalculationLineComponent,
    onSuccess: refreshCalculation,
  });

  const componentQuantityMutation = useMutation({
    mutationFn: changeCatalogPriceCalculationLineComponentQuantity,
    onSuccess: refreshCalculation,
  });

  const removeComponentMutation = useMutation({
    mutationFn: removeCatalogPriceCalculationLineComponent,
    onSuccess: refreshCalculation,
  });

  const setDiscountMutation = useMutation({
    mutationFn: setCatalogPriceCalculationManufacturerDiscount,
    onSuccess: refreshCalculation,
  });

  const removeDiscountMutation = useMutation({
    mutationFn: removeCatalogPriceCalculationManufacturerDiscount,
    onSuccess: refreshCalculation,
  });

  const completeMutation = useMutation({
    mutationFn: completeCatalogPriceCalculation,
    onSuccess: refreshCalculation,
  });

  const exportMutation = useMutation({
    mutationFn: exportCatalogPriceCalculation,
  });

  const cardMutation = useMutation({
    mutationFn: updateCatalogPriceCalculationCard,
    onSuccess: refreshCalculation,
  });

  const importPreviewMutation = useMutation({
    mutationFn: previewCatalogPriceCalculationImport,
    onSuccess: (preview) => {
      const hasPositionChanges = preview.rows.some(
        (row) => row.status !== "Unchanged",
      );
      const hasComponentChanges = preview.componentRows.some(
        (row) => row.status !== "Unchanged",
      );
      const hasCharacteristicChanges = preview.characteristicRows.some(
        (row) => row.status !== "Unchanged",
      );

      if (hasPositionChanges) {
        setImportPreviewSection("positions");
      } else if (hasComponentChanges) {
        setImportPreviewSection("components");
      } else if (hasCharacteristicChanges) {
        setImportPreviewSection("characteristics");
      } else {
        setImportPreviewSection("positions");
      }
    },
  });

  const importApplyMutation = useMutation({
    mutationFn: applyCatalogPriceCalculationImport,
    onSuccess: async () => {
      await refreshCalculation();

      if (projectImportFile) {
        importPreviewMutation.mutate({
          calculationId,
          file: projectImportFile,
        });
      }
    },
  });

  const actionableImportRows = useMemo(
    () =>
      (importPreviewMutation.data?.rows ?? []).filter(
        (row) =>
          row.productId !== null &&
          (row.status === "New" ||
            row.status === "QuantityChanged" ||
            row.status === "Removed"),
      ),
    [importPreviewMutation.data?.rows],
  );

  const actionableComponentImportRows = useMemo(
    () =>
      (importPreviewMutation.data?.componentRows ?? []).filter(
        (row) =>
          row.mainLineId !== null &&
          row.needDefinitionId !== null &&
          row.componentProductId !== null &&
          (row.status === "New" ||
            row.status === "QuantityChanged" ||
            row.status === "Removed"),
      ),
    [importPreviewMutation.data?.componentRows],
  );

  const actionableCharacteristicImportRows = useMemo(
    () =>
      (importPreviewMutation.data?.characteristicRows ?? []).filter(
        (row) =>
          row.productId !== null &&
          (row.status === "Changed" || row.status === "Removed"),
      ),
    [importPreviewMutation.data?.characteristicRows],
  );

  const characteristicImportView = useMemo(() => {
    const rows = importPreviewMutation.data?.characteristicRows ?? [];
    const unchangedCount = rows.filter(
      (row) => row.status === "Unchanged",
    ).length;
    const visibleRows = showUnchangedCharacteristics
      ? rows
      : rows.filter((row) => row.status !== "Unchanged");
    const groups = new Map<
      string,
      {
        key: string;
        rowNumber: number;
        productName: string;
        article: string;
        productTypeName: string;
        rows: CatalogPriceCalculationCharacteristicImportPreviewRow[];
      }
    >();

    for (const row of visibleRows) {
      const key = row.productId ?? `${row.rowNumber}:${row.article}`;
      const existingGroup = groups.get(key);

      if (existingGroup) {
        existingGroup.rows.push(row);
        continue;
      }

      groups.set(key, {
        key,
        rowNumber: row.rowNumber,
        productName: row.productName,
        article: row.article,
        productTypeName: row.productTypeName,
        rows: [row],
      });
    }

    return {
      groups: Array.from(groups.values()),
      unchangedCount,
      attentionCount: rows.length - unchangedCount,
    };
  }, [
    importPreviewMutation.data?.characteristicRows,
    showUnchangedCharacteristics,
  ]);

  const manufacturers = useMemo(() => {
    const result = new Map<string, string>();

    for (const line of calculation?.lines ?? []) {
      result.set(line.manufacturerId, line.manufacturerName);

      for (const component of line.components) {
        result.set(component.manufacturerId, component.manufacturerName);
      }
    }

    return Array.from(result, ([manufacturerId, manufacturerName]) => ({
      manufacturerId,
      manufacturerName,
    }));
  }, [calculation?.lines]);

  const mutationError =
    addLineMutation.error ??
    quantityMutation.error ??
    removeLineMutation.error ??
    addComponentMutation.error ??
    componentQuantityMutation.error ??
    removeComponentMutation.error ??
    setDiscountMutation.error ??
    removeDiscountMutation.error ??
    completeMutation.error ??
    cardMutation.error ??
    exportMutation.error;

  const searchTotalPages = Math.max(1, productsQuery.data?.totalPages ?? 0);

  function handleSearch(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    setAppliedSearch(search.trim());
    setSearchPage(1);
  }

  function handleImportPreview(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    if (!projectImportFile) {
      return;
    }

    importApplyMutation.reset();
    importPreviewMutation.mutate({
      calculationId,
      file: projectImportFile,
    });
  }

  function handleApplyImport(): void {
    const rows = actionableImportRows.map((row) => ({
      action:
        row.status === "New"
          ? ("Add" as const)
          : row.status === "QuantityChanged"
            ? ("UpdateQuantity" as const)
            : ("Remove" as const),
      productId: row.productId as string,
      existingLineId: row.existingLineId,
      quantity: row.status === "Removed" ? null : row.quantity,
    }));

    const componentRows = actionableComponentImportRows.map((row) => ({
      action:
        row.status === "New"
          ? ("Add" as const)
          : row.status === "QuantityChanged"
            ? ("UpdateQuantity" as const)
            : ("Remove" as const),
      mainLineId: row.mainLineId as string,
      existingComponentLineId: row.existingComponentLineId,
      needDefinitionId: row.needDefinitionId as string,
      componentProductId: row.componentProductId as string,
      quantityPerUnit: row.status === "Removed" ? null : row.quantityPerUnit,
    }));

    const characteristicRows = actionableCharacteristicImportRows.map(
      (row) => ({
        action: row.status === "Removed" ? ("Remove" as const) : ("Set" as const),
        productId: row.productId as string,
        characteristicCode: row.characteristicCode,
        value: row.status === "Removed" ? null : row.newValue,
      }),
    );

    if (
      rows.length === 0 &&
      componentRows.length === 0 &&
      characteristicRows.length === 0
    ) {
      return;
    }

    importApplyMutation.mutate({
      calculationId,
      rows,
      componentRows,
      characteristicRows,
    });
  }

  async function refreshAfterProductCreation(
    product: CreateCatalogProductResponse,
    projectQuantity: number | null,
  ): Promise<void> {
    const source = missingProductDraft?.source;
    setMissingProductDraft(null);

    await queryClient.invalidateQueries({
      queryKey: catalogPriceCalculationQueryKeys.productsRoot(calculationId),
    });

    if (source === "search") {
      addLineMutation.mutate({
        calculationId,
        productId: product.productId,
        quantity: projectQuantity ?? 1,
      });
      return;
    }

    if (source === "componentSearch") {
      await queryClient.invalidateQueries({
        queryKey: ["price-calculation-component-search", calculationId],
      });
      return;
    }

    if (source !== "projectImport" || !projectImportFile) {
      return;
    }

    importApplyMutation.reset();
    importPreviewMutation.mutate({
      calculationId,
      file: projectImportFile,
    });
  }

  function handleSaveCard(values: {
    customerName: string | null;
    objectName: string | null;
    projectNumber: string | null;
    responsibleName: string | null;
    comment: string | null;
    validUntil: string | null;
  }): void {
    cardMutation.mutate({
      calculationId,
      ...values,
    });
  }

  function handleAdd(productId: string, quantity: number): void {
    addLineMutation.mutate({
      calculationId,
      productId,
      quantity,
    });
  }

  function handleChangeQuantity(lineId: string, quantity: number): void {
    quantityMutation.mutate({
      calculationId,
      lineId,
      quantity,
    });
  }

  function handleRemoveLine(line: CatalogPriceCalculationLine): void {
    const confirmed = window.confirm(
      `Удалить позицию «${line.name}» из расчёта?`,
    );

    if (!confirmed) {
      return;
    }

    removeLineMutation.mutate({
      calculationId,
      lineId: line.lineId,
    });
  }

  function handleAddComponent(
    lineId: string,
    needDefinitionId: string,
    componentProductId: string,
    quantityPerUnit: number,
    manualSelection: boolean,
  ): void {
    addComponentMutation.mutate({
      calculationId,
      lineId,
      needDefinitionId,
      componentProductId,
      quantityPerUnit,
      manualSelection,
    });
  }

  function handleChangeComponentQuantity(
    lineId: string,
    componentLineId: string,
    quantityPerUnit: number,
  ): void {
    componentQuantityMutation.mutate({
      calculationId,
      lineId,
      componentLineId,
      quantityPerUnit,
    });
  }

  function handleRemoveComponent(
    lineId: string,
    component: CatalogPriceCalculationLineComponent,
  ): void {
    const confirmed = window.confirm(
      `Удалить комплектующее «${component.name}» из расчёта?`,
    );

    if (!confirmed) {
      return;
    }

    removeComponentMutation.mutate({
      calculationId,
      lineId,
      componentLineId: component.componentLineId,
    });
  }

  function handleSetDiscount(
    manufacturerId: string,
    discountPercent: number,
  ): void {
    setDiscountMutation.mutate({
      calculationId,
      manufacturerId,
      discountPercent,
    });
  }

  function handleRemoveDiscount(manufacturerId: string): void {
    removeDiscountMutation.mutate({
      calculationId,
      manufacturerId,
    });
  }

  function handleComplete(): void {
    if (!calculation || calculation.lines.length === 0) {
      return;
    }

    const confirmed = window.confirm(
      "Завершить расчёт? После завершения изменять товары, количество и скидки будет нельзя.",
    );

    if (!confirmed) {
      return;
    }

    completeMutation.mutate(calculationId);
  }

  if (calculationQuery.isLoading) {
    return (
      <div
        role="status"
        className="flex min-h-64 items-center justify-center rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] text-sm text-[var(--app-muted)]"
      >
        Загружаем расчёт...
      </div>
    );
  }

  if (calculationQuery.isError || !calculation) {
    return (
      <PageWorkspace
        eyebrow="Работа с каталогом"
        title="Расчёт не найден"
        description={getApiErrorMessage(
          calculationQuery.error,
          "Не удалось загрузить расчёт цен.",
        )}
      >
        <Link
          href="/catalog/price-calculations"
          className="inline-flex min-h-11 w-fit items-center justify-center rounded-xl border border-[var(--app-button-secondary-border)] bg-[var(--app-button-secondary-bg)] px-4 py-2.5 text-sm font-semibold text-[var(--app-text)]"
        >
          Назад к расчётам
        </Link>
      </PageWorkspace>
    );
  }

  return (
    <PageWorkspace
      eyebrow="Расчёт цен"
      title={calculation.title}
      description={`Создан ${formatDate(calculation.createdAtUtc)}. Цена берётся из активного прайса, а при его отсутствии — из карточки каталога.`}
      status={<StatusBadge status={calculation.status} />}
      contentClassName="grid min-w-0 gap-6"
      actions={
        <>
          <Link
            href="/catalog/price-calculations"
            className="inline-flex min-h-11 items-center justify-center rounded-xl border border-[var(--app-button-secondary-border)] bg-[var(--app-button-secondary-bg)] px-4 py-2.5 text-sm font-semibold text-[var(--app-text)]"
          >
            Назад
          </Link>

          {canExport && (
            <AppButton
              type="button"
              variant="secondary"
              loading={exportMutation.isPending}
              disabled={calculation.lines.length === 0}
              onClick={() => exportMutation.mutate(calculationId)}
            >
              Скачать Excel
            </AppButton>
          )}

          {editable && (
            <AppButton
              type="button"
              variant="primary"
              loading={completeMutation.isPending}
              disabled={calculation.lines.length === 0}
              onClick={handleComplete}
            >
              Завершить расчёт
            </AppButton>
          )}
        </>
      }
    >
      {mutationError && (
        <section
          role="alert"
          className="rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm text-[var(--app-danger)]"
        >
          {getApiErrorMessage(
            mutationError,
            "Не удалось выполнить действие с расчётом.",
          )}
        </section>
      )}

      <section className="grid gap-4 sm:grid-cols-3">
        <div className="rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5">
          <p className="text-sm text-[var(--app-muted)]">Позиций</p>

          <p className="mt-2 text-2xl font-bold tabular-nums text-[var(--app-text)]">
            {calculation.lines.length}
          </p>
        </div>

        <div className="rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5">
          <p className="text-sm text-[var(--app-muted)]">Производителей</p>

          <p className="mt-2 text-2xl font-bold tabular-nums text-[var(--app-text)]">
            {manufacturers.length}
          </p>
        </div>

        <div className="rounded-2xl border border-[var(--app-accent-border)] bg-[var(--app-accent-soft)] p-5">
          <p className="text-sm text-[var(--app-muted)]">Итоговая сумма</p>

          <p className="mt-2 text-2xl font-bold tabular-nums text-[var(--app-accent)]">
            {formatPrice(calculation.totalAmount, calculation.currency)}
          </p>
        </div>
      </section>

      <ProjectCardEditor
        key={`${calculation.calculationId}-${calculation.updatedAtUtc ?? calculation.createdAtUtc}`}
        calculation={calculation}
        editable={editable}
        isSaving={cardMutation.isPending}
        isSaved={cardMutation.isSuccess}
        onSave={handleSaveCard}
      />

      {editable && (
        <section
          aria-labelledby="project-import-title"
          className="rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5 sm:p-6"
        >
          <h2
            id="project-import-title"
            className="text-xl font-semibold text-[var(--app-text)]"
          >
            Загрузить позиции из Excel
          </h2>

          <p className="mt-2 text-sm leading-6 text-[var(--app-muted)]">
            Обязательные колонки: «Артикул» и «Количество». Производитель
            рекомендуется, а наименование используется для проверки.
          </p>

          <form
            onSubmit={handleImportPreview}
            className="mt-5 grid gap-4 md:grid-cols-[minmax(0,1fr)_auto] md:items-end"
          >
            <label className="grid min-w-0 gap-2">
              <span className="text-sm font-medium text-[var(--app-text)]">
                Файл проекта
              </span>

              <input
                type="file"
                accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                disabled={importPreviewMutation.isPending}
                onChange={(event) => {
                  setProjectImportFile(event.target.files?.[0] ?? null);
                  setImportPreviewSection("positions");
                  setShowUnchangedCharacteristics(false);
                  importPreviewMutation.reset();
                  importApplyMutation.reset();
                }}
                className="min-h-11 w-full rounded-xl border border-[var(--app-input-border)] bg-[var(--app-input-bg)] px-3 py-2 text-sm text-[var(--app-text)] file:mr-4 file:rounded-lg file:border-0 file:bg-[var(--app-accent-soft)] file:px-3 file:py-2 file:font-semibold file:text-[var(--app-accent)]"
              />
            </label>

            <AppButton
              type="submit"
              variant="primary"
              loading={importPreviewMutation.isPending}
              disabled={!projectImportFile}
            >
              Проверить файл
            </AppButton>
          </form>

          {importPreviewMutation.isError && (
            <div
              role="alert"
              className="mt-5 rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm text-[var(--app-danger)]"
            >
              {getApiErrorMessage(
                importPreviewMutation.error,
                "Не удалось проверить файл проекта.",
              )}
            </div>
          )}

          {importPreviewMutation.data && (
            <div className="mt-6 grid gap-5">
              {importPreviewMutation.data.warning && (
                <div className="rounded-2xl border border-[var(--app-warning-border)] bg-[var(--app-warning-soft)] p-4 text-sm text-[var(--app-warning)]">
                  {importPreviewMutation.data.warning}
                </div>
              )}

              <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-6">
                <div className="rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4">
                  <p className="text-sm text-[var(--app-muted)]">
                    Прочитано строк
                  </p>

                  <p className="mt-2 text-2xl font-bold tabular-nums text-[var(--app-text)]">
                    {importPreviewMutation.data.readRowsCount}
                  </p>
                </div>

                <div className="rounded-2xl border border-[var(--app-success-border)] bg-[var(--app-success-soft)] p-4">
                  <p className="text-sm text-[var(--app-success)]">
                    Будет добавлено
                  </p>

                  <p className="mt-2 text-2xl font-bold tabular-nums text-[var(--app-success)]">
                    {importPreviewMutation.data.addedRowsCount}
                  </p>
                </div>

                <div className="rounded-2xl border border-[var(--app-warning-border)] bg-[var(--app-warning-soft)] p-4">
                  <p className="text-sm text-[var(--app-warning)]">Изменится</p>
                  <p className="mt-2 text-2xl font-bold tabular-nums text-[var(--app-warning)]">
                    {importPreviewMutation.data.updatedRowsCount}
                  </p>
                </div>

                <div className="rounded-2xl border border-[var(--app-warning-border)] bg-[var(--app-warning-soft)] p-4">
                  <p className="text-sm text-[var(--app-warning)]">Удалится</p>
                  <p className="mt-2 text-2xl font-bold tabular-nums text-[var(--app-warning)]">
                    {importPreviewMutation.data.removedRowsCount}
                  </p>
                </div>

                <div className="rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4">
                  <p className="text-sm text-[var(--app-muted)]">Без изменений</p>
                  <p className="mt-2 text-2xl font-bold tabular-nums text-[var(--app-text)]">
                    {importPreviewMutation.data.unchangedRowsCount}
                  </p>
                </div>

                <div className="rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4">
                  <p className="text-sm text-[var(--app-danger)]">Пропущено</p>

                  <p className="mt-2 text-2xl font-bold tabular-nums text-[var(--app-danger)]">
                    {importPreviewMutation.data.skippedRowsCount}
                  </p>
                </div>
              </div>

              <div className="flex flex-col gap-3 rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4 sm:flex-row sm:items-center sm:justify-between">
                <p className="text-sm leading-6 text-[var(--app-muted)]">
                  Проверьте список ниже. После подтверждения добавления,
                  изменения количества и удаления будут применены одной операцией.
                </p>

                <AppButton
                  type="button"
                  variant="primary"
                  loading={importApplyMutation.isPending}
                  disabled={
                    (actionableImportRows.length === 0 &&
                      actionableComponentImportRows.length === 0 &&
                      actionableCharacteristicImportRows.length === 0) ||
                    importApplyMutation.isSuccess
                  }
                  onClick={handleApplyImport}
                >
                  Применить показанные изменения
                </AppButton>
              </div>

              {importApplyMutation.isError && (
                <div
                  role="alert"
                  className="rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm text-[var(--app-danger)]"
                >
                  {getApiErrorMessage(
                    importApplyMutation.error,
                    "Не удалось применить изменения из файла.",
                  )}
                </div>
              )}

              {importApplyMutation.data && (
                <div
                  role="status"
                  className="rounded-2xl border border-[var(--app-success-border)] bg-[var(--app-success-soft)] p-4 text-sm text-[var(--app-success)]"
                >
                  Добавлено: {importApplyMutation.data.addedLinesCount}, изменено: {" "}
                  {importApplyMutation.data.updatedLinesCount}, удалено: {" "}
                  {importApplyMutation.data.removedLinesCount}. Комплектующих добавлено: {" "}
                  {importApplyMutation.data.addedComponentsCount}, изменено: {" "}
                  {importApplyMutation.data.updatedComponentsCount}, удалено: {" "}
                  {importApplyMutation.data.removedComponentsCount}.
                  Характеристик изменено: {" "}
                  {importApplyMutation.data.updatedCharacteristicsCount}, очищено: {" "}
                  {importApplyMutation.data.removedCharacteristicsCount}. Строки с ошибками не применялись.
                </div>
              )}

              <div
                role="tablist"
                aria-label="Разделы проверки файла"
                className="flex flex-wrap gap-2 rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-2"
              >
                <AppButton
                  type="button"
                  size="sm"
                  variant={
                    importPreviewSection === "positions" ? "primary" : "secondary"
                  }
                  onClick={() => setImportPreviewSection("positions")}
                >
                  Позиции ({importPreviewMutation.data.rows.length})
                </AppButton>
                <AppButton
                  type="button"
                  size="sm"
                  variant={
                    importPreviewSection === "components" ? "primary" : "secondary"
                  }
                  disabled={importPreviewMutation.data.componentRows.length === 0}
                  onClick={() => setImportPreviewSection("components")}
                >
                  Комплектующие ({importPreviewMutation.data.componentRows.length})
                </AppButton>
                <AppButton
                  type="button"
                  size="sm"
                  variant={
                    importPreviewSection === "characteristics"
                      ? "primary"
                      : "secondary"
                  }
                  disabled={
                    importPreviewMutation.data.characteristicRows.length === 0
                  }
                  onClick={() => setImportPreviewSection("characteristics")}
                >
                  Характеристики ({importPreviewMutation.data.characteristicRows.length})
                </AppButton>
              </div>

              <div
                className={`${importPreviewSection === "positions" ? "" : "hidden"} overflow-x-auto rounded-2xl border border-[var(--app-border)]`}
              >
                <table className="w-full min-w-[1450px] border-collapse text-left text-sm">
                  <thead className="bg-[var(--app-surface)] text-[var(--app-muted)]">
                    <tr>
                      <th className="px-4 py-3 font-medium">Строка</th>
                      <th className="px-4 py-3 font-medium">Исходные данные</th>
                      <th className="px-4 py-3 font-medium">
                        Товар в каталоге
                      </th>
                      <th className="px-4 py-3 font-medium">Количество</th>
                      <th className="px-4 py-3 font-medium">На складе</th>
                      <th className="px-4 py-3 font-medium">Дефицит</th>
                      <th className="px-4 py-3 font-medium">Цена</th>
                      <th className="px-4 py-3 font-medium">Результат</th>
                    </tr>
                  </thead>

                  <tbody className="divide-y divide-[var(--app-border)]">
                    {importPreviewMutation.data.rows
                      .slice(0, 100)
                      .map((row) => (
                        <tr
                          key={`${row.rowNumber}-${row.article}`}
                          className="bg-[var(--app-panel)] align-top"
                        >
                          <td className="px-4 py-4 tabular-nums text-[var(--app-muted)]">
                            {row.rowNumber}
                          </td>

                          <td className="px-4 py-4">
                            <p className="font-medium text-[var(--app-text)]">
                              {row.sourceName ?? "Без наименования"}
                            </p>

                            <p className="mt-1 text-xs text-[var(--app-muted)]">
                              Артикул: {row.article || "—"}
                            </p>

                            <p className="mt-1 text-xs text-[var(--app-muted)]">
                              Производитель: {row.sourceManufacturer ?? "—"}
                            </p>
                          </td>

                          <td className="px-4 py-4">
                            <p className="font-medium text-[var(--app-text)]">
                              {row.productName ?? "—"}
                            </p>

                            {row.productArticle && (
                              <p className="mt-1 text-xs text-[var(--app-muted)]">
                                {row.manufacturerName} · {row.productArticle}
                              </p>
                            )}
                          </td>

                          <td className="px-4 py-4 tabular-nums text-[var(--app-text)]">
                            {row.status === "QuantityChanged"
                              ? `${formatQuantity(row.currentQuantity ?? 0)} → ${formatQuantity(row.quantity ?? 0)}`
                              : row.status === "Removed"
                                ? `${formatQuantity(row.currentQuantity ?? 0)} → удаление`
                                : row.quantity === null
                                  ? "—"
                                  : formatQuantity(row.quantity)}
                          </td>

                          <td className="px-4 py-4 tabular-nums text-[var(--app-text)]">
                            {row.stockQuantity === null
                              ? "—"
                              : formatQuantity(row.stockQuantity)}
                          </td>

                          <td
                            className={`px-4 py-4 font-semibold tabular-nums ${
                              (row.shortageQuantity ?? 0) > 0
                                ? "text-[var(--app-danger)]"
                                : "text-[var(--app-success)]"
                            }`}
                          >
                            {row.shortageQuantity === null
                              ? "—"
                              : row.shortageQuantity > 0
                                ? formatQuantity(row.shortageQuantity)
                                : "Достаточно"}
                          </td>

                          <td className="whitespace-nowrap px-4 py-4 tabular-nums text-[var(--app-text)]">
                            {row.basePriceAmount === null
                              ? "—"
                              : formatPrice(
                                  row.basePriceAmount,
                                  calculation.currency,
                                )}
                          </td>

                          <td className="px-4 py-4">
                            <span
                              className={`inline-flex rounded-full border px-3 py-1 text-xs font-semibold ${getImportStatusClassName(row.status)}`}
                            >
                              {getImportStatusLabel(row.status)}
                            </span>

                            {row.message && (
                              <p className="mt-2 max-w-80 text-xs leading-5 text-[var(--app-muted)]">
                                {row.message}
                              </p>
                            )}

                            {canEditProducts &&
                              row.status === "ProductNotFound" &&
                              row.article && (
                                <AppButton
                                  type="button"
                                  size="sm"
                                  variant="secondary"
                                  className="mt-3"
                                  onClick={() =>
                                    setMissingProductDraft({
                                      article: row.article,
                                      name:
                                        row.sourceName?.trim() || row.article,
                                      manufacturerName:
                                        row.sourceManufacturer,
                                      kind: "MainProduct",
                                      priceAmount: row.basePriceAmount,
                                      projectQuantity: null,
                                      source: "projectImport",
                                    })
                                  }
                                >
                                  Добавить в каталог
                                </AppButton>
                              )}
                          </td>
                        </tr>
                      ))}
                  </tbody>
                </table>
              </div>

              {importPreviewSection === "components" &&
                importPreviewMutation.data.componentRows.length > 0 && (
                <div className="grid gap-3">
                  <div>
                    <h3 className="text-lg font-semibold text-[var(--app-text)]">
                      Комплектующие
                    </h3>
                    <p className="mt-1 text-sm text-[var(--app-muted)]">
                      Изменения из листа «Комплектующие» применяются вместе с
                      основными позициями.
                    </p>
                  </div>

                  <div className="overflow-x-auto rounded-2xl border border-[var(--app-border)]">
                    <table className="w-full min-w-[1100px] border-collapse text-left text-sm">
                      <thead className="bg-[var(--app-surface)] text-[var(--app-muted)]">
                        <tr>
                          <th className="px-4 py-3 font-medium">Строка</th>
                          <th className="px-4 py-3 font-medium">Основной товар</th>
                          <th className="px-4 py-3 font-medium">Потребность</th>
                          <th className="px-4 py-3 font-medium">Комплектующее</th>
                          <th className="px-4 py-3 font-medium">Количество на единицу</th>
                          <th className="px-4 py-3 font-medium">Результат</th>
                        </tr>
                      </thead>

                      <tbody className="divide-y divide-[var(--app-border)]">
                        {importPreviewMutation.data.componentRows
                          .slice(0, 100)
                          .map((row) => (
                            <tr
                              key={`${row.rowNumber}-${row.mainProductArticle}-${row.componentArticle}`}
                              className="bg-[var(--app-panel)] align-top"
                            >
                              <td className="px-4 py-4 tabular-nums text-[var(--app-muted)]">
                                {row.rowNumber}
                              </td>
                              <td className="px-4 py-4 font-medium text-[var(--app-text)]">
                                {row.mainProductArticle || "—"}
                              </td>
                              <td className="px-4 py-4 text-[var(--app-text)]">
                                {row.needName || "—"}
                              </td>
                              <td className="px-4 py-4 font-medium text-[var(--app-text)]">
                                {row.componentArticle || "—"}
                              </td>
                              <td className="px-4 py-4 tabular-nums text-[var(--app-text)]">
                                {row.status === "QuantityChanged"
                                  ? `${row.currentQuantityPerUnit ?? 0} → ${row.quantityPerUnit ?? 0}`
                                  : row.status === "Removed"
                                    ? `${row.currentQuantityPerUnit ?? 0} → удаление`
                                    : row.quantityPerUnit ?? "—"}
                              </td>
                              <td className="px-4 py-4">
                                <span
                                  className={`inline-flex rounded-full border px-3 py-1 text-xs font-semibold ${getComponentImportStatusClassName(row.status)}`}
                                >
                                  {getComponentImportStatusLabel(row.status)}
                                </span>

                                {row.message && (
                                  <p className="mt-2 max-w-80 text-xs leading-5 text-[var(--app-muted)]">
                                    {row.message}
                                  </p>
                                )}

                                {canEditProducts &&
                                  row.status === "ComponentNotFound" &&
                                  row.componentArticle && (
                                    <AppButton
                                      type="button"
                                      size="sm"
                                      variant="secondary"
                                      className="mt-3"
                                      onClick={() =>
                                        setMissingProductDraft({
                                          article: row.componentArticle,
                                          name: row.componentArticle,
                                          manufacturerName: null,
                                          kind: "Component",
                                          priceAmount: null,
                                          projectQuantity: null,
                                          source: "projectImport",
                                        })
                                      }
                                    >
                                      Добавить в каталог
                                    </AppButton>
                                  )}
                              </td>
                            </tr>
                          ))}
                      </tbody>
                    </table>
                  </div>

                  {importPreviewMutation.data.componentRows.length > 100 && (
                    <p className="text-sm text-[var(--app-muted)]">
                      Показаны первые 100 комплектующих из {" "}
                      {importPreviewMutation.data.componentRows.length}.
                    </p>
                  )}
                </div>
              )}

              {importPreviewSection === "characteristics" &&
                importPreviewMutation.data.characteristicRows.length > 0 && (
                <div className="grid gap-3">
                  <div className="flex flex-col gap-3 lg:flex-row lg:items-end lg:justify-between">
                    <div>
                      <h3 className="text-lg font-semibold text-[var(--app-text)]">
                        Изменения характеристик
                      </h3>
                      <p className="mt-1 text-sm text-[var(--app-muted)]">
                        По умолчанию показаны только изменения и ошибки. Они
                        применятся к карточкам каталога после подтверждения.
                      </p>
                    </div>

                    <div className="flex flex-wrap items-center gap-2">
                      <span className="rounded-full border border-[var(--app-warning-border)] bg-[var(--app-warning-soft)] px-3 py-1.5 text-xs font-semibold text-[var(--app-warning)]">
                        Требуют внимания: {characteristicImportView.attentionCount}
                      </span>
                      <span className="rounded-full border border-[var(--app-border)] bg-[var(--app-surface)] px-3 py-1.5 text-xs font-semibold text-[var(--app-muted)]">
                        Без изменений: {characteristicImportView.unchangedCount}
                      </span>
                      {characteristicImportView.unchangedCount > 0 && (
                        <AppButton
                          type="button"
                          size="sm"
                          variant="secondary"
                          onClick={() =>
                            setShowUnchangedCharacteristics((current) => !current)
                          }
                        >
                          {showUnchangedCharacteristics
                            ? "Скрыть без изменений"
                            : "Показать без изменений"}
                        </AppButton>
                      )}
                    </div>
                  </div>

                  {characteristicImportView.groups.length === 0 ? (
                    <div className="rounded-2xl border border-[var(--app-success-border)] bg-[var(--app-success-soft)] p-4 text-sm text-[var(--app-success)]">
                      Характеристики товаров не изменились.
                    </div>
                  ) : (
                    <div className="grid gap-4">
                      {characteristicImportView.groups.slice(0, 100).map((group) => (
                        <section
                          key={group.key}
                          className="overflow-hidden rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel)]"
                        >
                          <div className="flex flex-col gap-1 border-b border-[var(--app-border)] bg-[var(--app-surface)] px-4 py-3 sm:flex-row sm:items-center sm:justify-between">
                            <div>
                              <p className="font-semibold text-[var(--app-text)]">
                                {group.productName || "Товар не найден"}
                              </p>
                              <p className="mt-1 text-xs text-[var(--app-muted)]">
                                {group.article || "Без артикула"}
                                {group.productTypeName
                                  ? ` · ${group.productTypeName}`
                                  : ""}
                              </p>
                            </div>
                            <span className="text-xs text-[var(--app-muted)]">
                              Строка Excel: {group.rowNumber}
                            </span>
                          </div>

                          <div className="divide-y divide-[var(--app-border)]">
                            {group.rows.map((row) => (
                              <div
                                key={`${row.rowNumber}-${row.characteristicCode}`}
                                className="grid gap-3 px-4 py-4 lg:grid-cols-[minmax(220px,1fr)_minmax(260px,1.2fr)_minmax(220px,0.8fr)] lg:items-center"
                              >
                                <div>
                                  <p className="font-medium text-[var(--app-text)]">
                                    {row.characteristicName || "Характеристика"}
                                    {row.unit ? `, ${row.unit}` : ""}
                                  </p>
                                  <p className="mt-1 text-xs text-[var(--app-muted)]">
                                    {row.isRequired
                                      ? "Обязательная"
                                      : "Необязательная"}
                                  </p>
                                </div>

                                <div className="flex flex-wrap items-center gap-2 text-sm">
                                  <span className="rounded-lg bg-[var(--app-surface)] px-3 py-2 text-[var(--app-muted)]">
                                    {formatCharacteristicImportValue(
                                      row.currentValue,
                                      row.dataType,
                                    )}
                                  </span>
                                  <span className="text-[var(--app-muted)]">→</span>
                                  <span className="rounded-lg bg-[var(--app-accent-soft)] px-3 py-2 font-medium text-[var(--app-text)]">
                                    {row.status === "Removed"
                                      ? "Очистить"
                                      : formatCharacteristicImportValue(
                                          row.newValue,
                                          row.dataType,
                                        )}
                                  </span>
                                </div>

                                <div>
                                  <span
                                    className={`inline-flex rounded-full border px-3 py-1 text-xs font-semibold ${getCharacteristicImportStatusClassName(row.status)}`}
                                  >
                                    {getCharacteristicImportStatusLabel(row.status)}
                                  </span>
                                  {row.message && row.status !== "Unchanged" && (
                                    <p className="mt-2 text-xs leading-5 text-[var(--app-muted)]">
                                      {row.message}
                                    </p>
                                  )}
                                </div>
                              </div>
                            ))}
                          </div>
                        </section>
                      ))}
                    </div>
                  )}

                  {characteristicImportView.groups.length > 100 && (
                    <p className="text-sm text-[var(--app-muted)]">
                      Показаны первые 100 товаров из {" "}
                      {characteristicImportView.groups.length}.
                    </p>
                  )}
                </div>
              )}

              {importPreviewSection === "positions" &&
                importPreviewMutation.data.rows.length > 100 && (
                <p className="text-sm text-[var(--app-muted)]">
                  Показаны первые 100 строк из{" "}
                  {importPreviewMutation.data.rows.length}. Все строки файла
                  были проверены.
                </p>
              )}
            </div>
          )}
        </section>
      )}

      {editable && (
        <section
          aria-labelledby="product-search-title"
          className="rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5 sm:p-6"
        >
          <h2
            id="product-search-title"
            className="text-xl font-semibold text-[var(--app-text)]"
          >
            Добавить товар
          </h2>

          <p className="mt-2 text-sm text-[var(--app-muted)]">
            Поиск выполняется по всему каталогу. Если для товара есть одна
            актуальная цена в прайсе, используется она; иначе расчёт возьмёт
            цену из карточки товара.
          </p>

          <form
            onSubmit={handleSearch}
            className="mt-5 grid gap-3 sm:grid-cols-[minmax(0,1fr)_auto]"
          >
            <AppInput
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Артикул или наименование товара"
            />

            <AppButton
              type="submit"
              variant="primary"
              disabled={search.trim().length === 0}
            >
              Найти
            </AppButton>
          </form>

          {productsQuery.isError && (
            <div
              role="alert"
              className="mt-4 rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm text-[var(--app-danger)]"
            >
              {getApiErrorMessage(
                productsQuery.error,
                "Не удалось выполнить поиск товаров.",
              )}
            </div>
          )}

          {productsQuery.isLoading ? (
            <p role="status" className="mt-5 text-sm text-[var(--app-muted)]">
              Ищем товары...
            </p>
          ) : productsQuery.data && productsQuery.data.items.length > 0 ? (
            <>
              <div className="mt-5 overflow-x-auto rounded-2xl border border-[var(--app-border)]">
                <table className="w-full min-w-[850px] border-collapse text-left text-sm">
                  <thead className="bg-[var(--app-surface)] text-[var(--app-muted)]">
                    <tr>
                      <th className="px-4 py-3 font-medium">Товар</th>
                      <th className="px-4 py-3 font-medium">Производитель</th>
                      <th className="px-4 py-3 font-medium">Базовая цена</th>
                      <th className="px-4 py-3 font-medium">МРЦ</th>
                      <th className="px-4 py-3 font-medium">Действие</th>
                    </tr>
                  </thead>

                  <tbody className="divide-y divide-[var(--app-border)]">
                    {productsQuery.data.items.map((product) => (
                      <ProductSearchRow
                        key={product.productId}
                        product={product}
                        currency={calculation.currency}
                        isAdding={
                          addLineMutation.isPending &&
                          addLineMutation.variables?.productId ===
                            product.productId
                        }
                        onAdd={handleAdd}
                      />
                    ))}
                  </tbody>
                </table>
              </div>

              <nav className="mt-4 flex items-center justify-between">
                <AppButton
                  type="button"
                  variant="secondary"
                  disabled={searchPage <= 1 || productsQuery.isFetching}
                  onClick={() =>
                    setSearchPage((current) => Math.max(1, current - 1))
                  }
                >
                  Назад
                </AppButton>

                <p className="text-sm text-[var(--app-muted)]">
                  Страница {searchPage} из {searchTotalPages}
                </p>

                <AppButton
                  type="button"
                  variant="secondary"
                  disabled={
                    searchPage >= searchTotalPages || productsQuery.isFetching
                  }
                  onClick={() =>
                    setSearchPage((current) =>
                      Math.min(searchTotalPages, current + 1),
                    )
                  }
                >
                  Вперёд
                </AppButton>
              </nav>
            </>
          ) : appliedSearch.length > 0 && productsQuery.isSuccess ? (
            <div className="mt-5 flex flex-col gap-3 rounded-2xl border border-dashed border-[var(--app-border-strong)] p-5 sm:flex-row sm:items-center sm:justify-between">
              <p className="text-sm text-[var(--app-muted)]">
                В каталоге подходящие товары не найдены.
              </p>
              {canEditProducts && (
                <AppButton
                  type="button"
                  size="sm"
                  variant="secondary"
                  onClick={() =>
                    setMissingProductDraft({
                      article: appliedSearch,
                      name: appliedSearch,
                      manufacturerName: null,
                      kind: "MainProduct",
                      priceAmount: 0,
                      projectQuantity: 1,
                      source: "search",
                    })
                  }
                >
                  Добавить товар в каталог
                </AppButton>
              )}
            </div>
          ) : null}
        </section>
      )}

      {editable && manufacturers.length > 0 && (
        <section
          aria-labelledby="discounts-title"
          className="rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5 sm:p-6"
        >
          <h2
            id="discounts-title"
            className="text-xl font-semibold text-[var(--app-text)]"
          >
            Проектные скидки
          </h2>

          <p className="mt-2 text-sm text-[var(--app-muted)]">
            Скидка применяется ко всем строкам соответствующего производителя.
          </p>

          <div className="mt-5 grid gap-4 lg:grid-cols-2">
            {manufacturers.map((manufacturer) => (
              <ManufacturerDiscountEditor
                key={`${manufacturer.manufacturerId}-${calculation.manufacturerDiscounts.find((item) => item.manufacturerId === manufacturer.manufacturerId)?.discountPercent ?? "none"}`}
                manufacturerId={manufacturer.manufacturerId}
                manufacturerName={manufacturer.manufacturerName}
                discount={calculation.manufacturerDiscounts.find(
                  (item) => item.manufacturerId === manufacturer.manufacturerId,
                )}
                disabled={!editable}
                isSaving={
                  setDiscountMutation.isPending &&
                  setDiscountMutation.variables?.manufacturerId ===
                    manufacturer.manufacturerId
                }
                isRemoving={
                  removeDiscountMutation.isPending &&
                  removeDiscountMutation.variables?.manufacturerId ===
                    manufacturer.manufacturerId
                }
                onSave={handleSetDiscount}
                onRemove={handleRemoveDiscount}
              />
            ))}
          </div>
        </section>
      )}

      <section
        aria-labelledby="calculation-lines-title"
        className="rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5 sm:p-6"
      >
        <h2
          id="calculation-lines-title"
          className="text-xl font-semibold text-[var(--app-text)]"
        >
          Позиции расчёта
        </h2>

        {calculation.lines.length > 0 ? (
          <div className="mt-5 overflow-x-auto rounded-2xl border border-[var(--app-border)]">
            <table className="w-full min-w-[1750px] border-collapse text-left text-sm">
              <thead className="bg-[var(--app-surface)] text-[var(--app-muted)]">
                <tr>
                  <th className="px-4 py-3 font-medium">Товар</th>
                  <th className="px-4 py-3 font-medium">Производитель</th>
                  <th className="px-4 py-3 font-medium">Количество</th>
                  <th className="px-4 py-3 font-medium">На складе</th>
                  <th className="px-4 py-3 font-medium">Дефицит</th>
                  <th className="px-4 py-3 font-medium">Базовая цена</th>
                  <th className="px-4 py-3 font-medium">МРЦ</th>
                  <th className="px-4 py-3 font-medium">Скидка</th>
                  <th className="px-4 py-3 font-medium">Проектная цена</th>
                  <th className="px-4 py-3 font-medium">Сумма</th>
                  {editable && (
                    <th className="px-4 py-3 font-medium">Действие</th>
                  )}
                </tr>
              </thead>

              <tbody className="divide-y divide-[var(--app-border)]">
                {calculation.lines.map((line) => (
                  <CalculationLineRow
                    key={`${line.lineId}-${line.quantity}`}
                    calculationId={calculationId}
                    line={line}
                    currency={calculation.currency}
                    editable={editable}
                    isChanging={
                      quantityMutation.isPending &&
                      quantityMutation.variables?.lineId === line.lineId
                    }
                    isRemoving={
                      removeLineMutation.isPending &&
                      removeLineMutation.variables?.lineId === line.lineId
                    }
                    changingComponentLineId={
                      componentQuantityMutation.isPending &&
                      componentQuantityMutation.variables?.lineId === line.lineId
                        ? componentQuantityMutation.variables.componentLineId
                        : undefined
                    }
                    removingComponentLineId={
                      removeComponentMutation.isPending &&
                      removeComponentMutation.variables?.lineId === line.lineId
                        ? removeComponentMutation.variables.componentLineId
                        : undefined
                    }
                    addingComponentKey={
                      addComponentMutation.isPending &&
                      addComponentMutation.variables?.lineId === line.lineId
                        ? `${addComponentMutation.variables.manualSelection ? "manual:" : ""}${addComponentMutation.variables.needDefinitionId}:${addComponentMutation.variables.componentProductId}`
                        : undefined
                    }
                    onChangeQuantity={handleChangeQuantity}
                    onRemove={handleRemoveLine}
                    onAddComponent={handleAddComponent}
                    canCreateProducts={canEditProducts}
                    onCreateComponent={(article) =>
                      setMissingProductDraft({
                        article,
                        name: article,
                        manufacturerName: null,
                        kind: "Component",
                        priceAmount: 0,
                        projectQuantity: null,
                        source: "componentSearch",
                      })
                    }
                    onChangeComponentQuantity={
                      handleChangeComponentQuantity
                    }
                    onRemoveComponent={handleRemoveComponent}
                  />
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <div className="mt-5 rounded-2xl border border-dashed border-[var(--app-border-strong)] bg-[var(--app-surface)] p-6">
            <h3 className="font-semibold text-[var(--app-text)]">
              В расчёте пока нет позиций
            </h3>

            <p className="mt-2 text-sm text-[var(--app-muted)]">
              Найдите товар по артикулу или наименованию и добавьте его в
              расчёт.
            </p>
          </div>
        )}
      </section>

      {missingProductDraft && (
        <CreateCatalogProductDialog
          article={missingProductDraft.article}
          name={missingProductDraft.name}
          manufacturerName={missingProductDraft.manufacturerName}
          kind={missingProductDraft.kind}
          priceAmount={missingProductDraft.priceAmount}
          projectQuantity={missingProductDraft.projectQuantity}
          onClose={() => setMissingProductDraft(null)}
          onCreated={refreshAfterProductCreation}
        />
      )}
    </PageWorkspace>
  );
}
