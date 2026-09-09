"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useMemo, useState, type FormEvent } from "react";
import {
  addCatalogPriceCalculationLine,
  applyCatalogPriceCalculationImport,
  changeCatalogPriceCalculationLineQuantity,
  completeCatalogPriceCalculation,
  getCatalogPriceCalculation,
  previewCatalogPriceCalculationImport,
  removeCatalogPriceCalculationLine,
  removeCatalogPriceCalculationManufacturerDiscount,
  searchCatalogPriceCalculationProducts,
  setCatalogPriceCalculationManufacturerDiscount,
  updateCatalogPriceCalculationCard,
} from "@/features/catalogPriceCalculations/api/catalogPriceCalculationEditorApi";
import { exportCatalogPriceCalculation } from "@/features/catalogPriceCalculations/api/exportCatalogPriceCalculation";
import { catalogPriceCalculationQueryKeys } from "@/features/catalogPriceCalculations/model/queryKeys";
import type {
  CatalogPriceCalculationDetails,
  CatalogPriceCalculationImportRowStatus,
  CatalogPriceCalculationLine,
  CatalogPriceCalculationManufacturerDiscount,
  CatalogPriceCalculationProductSearchItem,
  CatalogPriceCalculationStatus,
} from "@/features/catalogPriceCalculations/model/types";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { formatDate, formatPrice } from "@/shared/lib/formatters";
import { AppButton } from "@/shared/ui/AppButton";
import { AppInput } from "@/shared/ui/AppInput";
import { PageWorkspace } from "@/shared/ui/PageWorkspace";

const productSearchPageSize = 20;

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
    case "Matched":
      return "Сопоставлена";
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
  return status === "Matched"
    ? "border-[var(--app-success-border)] bg-[var(--app-success-soft)] text-[var(--app-success)]"
    : "border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] text-[var(--app-danger)]";
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
  onAdd: (productId: string) => void;
}) {
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
        {formatPrice(product.basePriceAmount, currency)}
      </td>

      <td className="whitespace-nowrap px-4 py-4 tabular-nums text-[var(--app-muted)]">
        {product.mrcPriceAmount === null || product.mrcPriceAmount === undefined
          ? "—"
          : formatPrice(product.mrcPriceAmount, currency)}
      </td>

      <td className="px-4 py-4">
        <AppButton
          type="button"
          variant="primary"
          size="sm"
          loading={isAdding}
          onClick={() => onAdd(product.productId)}
        >
          Добавить
        </AppButton>
      </td>
    </tr>
  );
}

function CalculationLineRow({
  line,
  currency,
  editable,
  isChanging,
  isRemoving,
  onChangeQuantity,
  onRemove,
}: {
  line: CatalogPriceCalculationLine;
  currency: string;
  editable: boolean;
  isChanging: boolean;
  isRemoving: boolean;
  onChangeQuantity: (lineId: string, quantity: number) => void;
  onRemove: (line: CatalogPriceCalculationLine) => void;
}) {
  const [quantity, setQuantity] = useState(line.quantity.toString());

  function handleSave(): void {
    const parsedQuantity = Number(quantity.replace(",", "."));

    if (!Number.isFinite(parsedQuantity) || parsedQuantity <= 0) {
      return;
    }

    onChangeQuantity(line.lineId, parsedQuantity);
  }

  return (
    <tr className="bg-[var(--app-panel)] align-top">
      <td className="px-4 py-4">
        <p className="font-medium text-[var(--app-text)]">{line.name}</p>

        <p className="mt-1 text-xs text-[var(--app-muted)]">{line.article}</p>
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

  const calculationId = params.calculationId ?? "";

  const [search, setSearch] = useState("");
  const [appliedSearch, setAppliedSearch] = useState("");
  const [searchPage, setSearchPage] = useState(1);
  const [projectImportFile, setProjectImportFile] = useState<File | null>(null);

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
  });

  const importApplyMutation = useMutation({
    mutationFn: applyCatalogPriceCalculationImport,
    onSuccess: refreshCalculation,
  });

  const matchedImportRows = useMemo(
    () =>
      (importPreviewMutation.data?.rows ?? []).filter(
        (row) =>
          row.status === "Matched" &&
          row.productId !== null &&
          row.quantity !== null,
      ),
    [importPreviewMutation.data?.rows],
  );

  const manufacturers = useMemo(() => {
    const result = new Map<string, string>();

    for (const line of calculation?.lines ?? []) {
      result.set(line.manufacturerId, line.manufacturerName);
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
    const rows = matchedImportRows.map((row) => ({
      productId: row.productId as string,
      quantity: row.quantity as number,
    }));

    if (rows.length === 0) {
      return;
    }

    importApplyMutation.mutate({
      calculationId,
      rows,
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

  function handleAdd(productId: string): void {
    addLineMutation.mutate({
      calculationId,
      productId,
      quantity: 1,
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
      description={`Создан ${formatDate(calculation.createdAtUtc)}. Цены получены из активных прайс-листов и изменяются только backend.`}
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

          <AppButton
            type="button"
            variant="secondary"
            loading={exportMutation.isPending}
            disabled={calculation.lines.length === 0}
            onClick={() => exportMutation.mutate(calculationId)}
          >
            Скачать Excel
          </AppButton>

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
              <div className="grid gap-3 sm:grid-cols-3">
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
                    Сопоставлено
                  </p>

                  <p className="mt-2 text-2xl font-bold tabular-nums text-[var(--app-success)]">
                    {importPreviewMutation.data.matchedRowsCount}
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
                  В проект попадут только сопоставленные позиции. Строки с
                  ошибками будут пропущены.
                </p>

                <AppButton
                  type="button"
                  variant="primary"
                  loading={importApplyMutation.isPending}
                  disabled={
                    matchedImportRows.length === 0 ||
                    importApplyMutation.isSuccess
                  }
                  onClick={handleApplyImport}
                >
                  Добавить сопоставленные позиции в проект
                </AppButton>
              </div>

              {importApplyMutation.isError && (
                <div
                  role="alert"
                  className="rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm text-[var(--app-danger)]"
                >
                  {getApiErrorMessage(
                    importApplyMutation.error,
                    "Не удалось добавить позиции из файла в проект.",
                  )}
                </div>
              )}

              {importApplyMutation.data && (
                <div
                  role="status"
                  className="rounded-2xl border border-[var(--app-success-border)] bg-[var(--app-success-soft)] p-4 text-sm text-[var(--app-success)]"
                >
                  Добавлено позиций: {importApplyMutation.data.addedLinesCount}.
                  Строки с ошибками не добавлялись.
                </div>
              )}

              <div className="overflow-x-auto rounded-2xl border border-[var(--app-border)]">
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
                            {row.quantity === null
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
                          </td>
                        </tr>
                      ))}
                  </tbody>
                </table>
              </div>

              {importPreviewMutation.data.rows.length > 100 && (
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
            Поиск показывает только товары с корректной строкой в активном
            прайсе производителя.
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
                      <th className="px-4 py-3 font-medium">Прайс 100%</th>
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
            <p className="mt-5 rounded-2xl border border-dashed border-[var(--app-border-strong)] p-5 text-sm text-[var(--app-muted)]">
              В активных прайсах подходящие товары не найдены.
            </p>
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
                  <th className="px-4 py-3 font-medium">Прайс 100%</th>
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
                    onChangeQuantity={handleChangeQuantity}
                    onRemove={handleRemoveLine}
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
    </PageWorkspace>
  );
}
