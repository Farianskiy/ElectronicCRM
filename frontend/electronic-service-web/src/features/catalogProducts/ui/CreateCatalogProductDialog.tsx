"use client";

import { useMemo, useState, type FormEvent } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { createCatalogProduct } from "../api/createCatalogProduct";
import { getCatalogManufacturers } from "@/features/catalogMetadata/api/getCatalogManufacturers";
import { getCatalogProductTypes } from "@/features/catalogMetadata/api/getCatalogProductTypes";
import { getCatalogProductTypeCharacteristics } from "@/features/catalogMetadata/api/getCatalogProductTypeCharacteristics";
import type {
  CatalogCharacteristicDataType,
  CatalogProductTypeKind,
} from "@/features/catalogMetadata/model/types";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppButton } from "@/shared/ui/AppButton";
import { AppInput } from "@/shared/ui/AppInput";
import { AppSelect } from "@/shared/ui/AppSelect";
import type { CreateCatalogProductResponse } from "../model/types";
import {
  POLE_CONFIGURATION_OPTIONS,
  POLES_CHARACTERISTIC_CODE,
} from "../model/poleConfigurations";

function normalizeCharacteristicValue(
  dataType: CatalogCharacteristicDataType,
  value: string,
): string | null {
  const trimmedValue = value.trim();

  if (!trimmedValue) {
    return null;
  }

  if (dataType === "Number") {
    const normalizedNumber = trimmedValue.replace(",", ".");
    return Number.isFinite(Number(normalizedNumber)) ? normalizedNumber : null;
  }

  return trimmedValue;
}

interface CreateCatalogProductDialogProps {
  article: string;
  name: string;
  manufacturerName: string | null;
  kind: CatalogProductTypeKind;
  priceAmount?: number | null;
  projectQuantity?: number | null;
  onClose: () => void;
  onCreated: (
    product: CreateCatalogProductResponse,
    projectQuantity: number | null,
  ) => void;
}

export function CreateCatalogProductDialog({
  article: initialArticle,
  name: initialName,
  manufacturerName,
  kind,
  priceAmount = 0,
  projectQuantity: initialProjectQuantity = null,
  onClose,
  onCreated,
}: CreateCatalogProductDialogProps) {
  const [article, setArticle] = useState(initialArticle);
  const [name, setName] = useState(initialName);
  const [manufacturerId, setManufacturerId] = useState("");
  const [productTypeId, setProductTypeId] = useState("");
  const [price, setPrice] = useState(String(priceAmount ?? 0));
  const [stockQuantity, setStockQuantity] = useState("0");
  const [projectQuantity, setProjectQuantity] = useState(
    initialProjectQuantity === null ? "" : String(initialProjectQuantity),
  );
  const [characteristicValues, setCharacteristicValues] = useState<
    Record<string, string>
  >({});
  const [validationError, setValidationError] = useState<string | null>(null);

  const manufacturersQuery = useQuery({
    queryKey: ["catalog-metadata", "manufacturers"],
    queryFn: getCatalogManufacturers,
  });
  const productTypesQuery = useQuery({
    queryKey: ["catalog-metadata", "product-types"],
    queryFn: getCatalogProductTypes,
  });
  const productTypes = useMemo(
    () => (productTypesQuery.data ?? []).filter((item) => item.kind === kind),
    [kind, productTypesQuery.data],
  );
  const effectiveManufacturerId = useMemo(() => {
    if (manufacturerId || !manufacturerName) {
      return manufacturerId;
    }

    const normalizedName = manufacturerName.trim().toLocaleUpperCase("ru-RU");

    return (
      manufacturersQuery.data?.find(
        (item) =>
          item.name.trim().toLocaleUpperCase("ru-RU") === normalizedName,
      )?.id ?? ""
    );
  }, [manufacturerId, manufacturerName, manufacturersQuery.data]);
  const effectiveProductTypeId =
    productTypeId || (productTypes.length === 1 ? productTypes[0]?.id ?? "" : "");
  const effectiveProductType = productTypes.find(
    (item) => item.id === effectiveProductTypeId,
  );
  const characteristicsQuery = useQuery({
    queryKey: [
      "catalog-product-type-characteristics",
      effectiveProductType?.code ?? "",
    ],
    queryFn: () =>
      getCatalogProductTypeCharacteristics(effectiveProductType?.code ?? ""),
    enabled: Boolean(effectiveProductType?.code),
    staleTime: 5 * 60 * 1000,
  });
  const characteristics = useMemo(
    () =>
      [...(characteristicsQuery.data ?? [])].sort((first, second) => {
        if (first.isRequired !== second.isRequired) {
          return first.isRequired ? -1 : 1;
        }

        return first.name.localeCompare(second.name, "ru");
      }),
    [characteristicsQuery.data],
  );
  const characteristicsAreValid = characteristics.every((characteristic) => {
    const rawValue = characteristicValues[characteristic.code] ?? "";

    if (!rawValue.trim()) {
      return !characteristic.isRequired;
    }

    return normalizeCharacteristicValue(characteristic.dataType, rawValue) !== null;
  });
  const createMutation = useMutation({
    mutationFn: createCatalogProduct,
    onSuccess: (product) =>
      onCreated(
        product,
        initialProjectQuantity === null
          ? null
          : Number(projectQuantity.replace(",", ".")),
      ),
  });

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();
    setValidationError(null);

    const parsedPrice = Number(price.replace(",", "."));
    const parsedStockQuantity = Number(stockQuantity.replace(",", "."));
    const parsedProjectQuantity = Number(projectQuantity.replace(",", "."));

    if (
      !article.trim() ||
      !name.trim() ||
      !effectiveManufacturerId ||
      !effectiveProductTypeId ||
      !Number.isFinite(parsedPrice) ||
      parsedPrice < 0 ||
      !Number.isFinite(parsedStockQuantity) ||
      parsedStockQuantity < 0 ||
      (initialProjectQuantity !== null &&
        (!Number.isFinite(parsedProjectQuantity) || parsedProjectQuantity <= 0))
    ) {
      return;
    }

    const characteristicRequests: Array<{ code: string; value: string }> = [];

    for (const characteristic of characteristics) {
      const rawValue = characteristicValues[characteristic.code] ?? "";
      const normalizedValue = normalizeCharacteristicValue(
        characteristic.dataType,
        rawValue,
      );

      if (!rawValue.trim()) {
        if (characteristic.isRequired) {
          setValidationError(
            `Заполните обязательную характеристику «${characteristic.name}».`,
          );
          return;
        }

        continue;
      }

      if (normalizedValue === null) {
        setValidationError(
          `Укажите корректное значение характеристики «${characteristic.name}».`,
        );
        return;
      }

      characteristicRequests.push({
        code: characteristic.code,
        value: normalizedValue,
      });
    }

    createMutation.mutate({
      article: article.trim(),
      name: name.trim(),
      manufacturerId: effectiveManufacturerId,
      productTypeId: effectiveProductTypeId,
      priceAmount: parsedPrice,
      stockQuantity: parsedStockQuantity,
      characteristics: characteristicRequests,
    });
  }

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="create-product-dialog-title"
      className="fixed inset-0 z-[100] flex items-center justify-center bg-black/60 p-4"
      onMouseDown={(event) => {
        if (event.currentTarget === event.target) {
          onClose();
        }
      }}
    >
      <form
        className="grid max-h-[90vh] w-full max-w-2xl gap-5 overflow-y-auto rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-6 shadow-2xl"
        onSubmit={handleSubmit}
      >
        <div className="flex items-start justify-between gap-4">
          <div>
            <h2
              id="create-product-dialog-title"
              className="text-xl font-semibold text-[var(--app-text)]"
            >
              Добавить {kind === "Component" ? "комплектующее" : "товар"} в каталог
            </h2>
            <p className="mt-2 text-sm leading-6 text-[var(--app-muted)]">
              Без обучения и распознавания: проверьте основные поля и сохраните.
              После сохранения система продолжит текущее действие.
            </p>
          </div>

          <AppButton type="button" variant="ghost" size="sm" onClick={onClose}>
            Закрыть
          </AppButton>
        </div>

        <label className="grid gap-2 text-sm font-medium text-[var(--app-text)]">
          Артикул
          <AppInput
            value={article}
            maxLength={100}
            onChange={(event) => setArticle(event.target.value)}
          />
        </label>

        <label className="grid gap-2 text-sm font-medium text-[var(--app-text)]">
          Наименование
          <AppInput
            value={name}
            maxLength={500}
            onChange={(event) => setName(event.target.value)}
          />
        </label>

        <div className="grid gap-4 sm:grid-cols-2">
          <label className="grid gap-2 text-sm font-medium text-[var(--app-text)]">
            Производитель
            <AppSelect
              ariaLabel="Производитель"
              expandInFlow
              value={effectiveManufacturerId}
              disabled={manufacturersQuery.isLoading}
              onChange={setManufacturerId}
              options={(manufacturersQuery.data ?? []).map((item) => ({
                value: item.id,
                label: item.name,
              }))}
            />
          </label>

          <label className="grid gap-2 text-sm font-medium text-[var(--app-text)]">
            {kind === "Component" ? "Тип комплектующего" : "Тип товара"}
            <AppSelect
              ariaLabel="Тип товара"
              expandInFlow
              value={effectiveProductTypeId}
              disabled={productTypesQuery.isLoading || productTypes.length === 0}
              onChange={(nextProductTypeId) => {
                setProductTypeId(nextProductTypeId);
                setCharacteristicValues({});
                setValidationError(null);
              }}
              options={productTypes.map((item) => ({
                value: item.id,
                label: item.name,
              }))}
            />
            {!productTypesQuery.isLoading && productTypes.length === 0 && (
              <span className="text-xs font-normal text-[var(--app-danger)]">
                {kind === "Component"
                  ? "В справочнике пока нет типов комплектующих. Сначала создайте тип с назначением «Комплектующее»."
                  : "В справочнике пока нет типов основных товаров."}
              </span>
            )}
          </label>
        </div>

        <div className="rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] px-4 py-3">
          <p className="text-xs text-[var(--app-muted)]">Назначение в каталоге</p>
          <p className="mt-1 text-sm font-semibold text-[var(--app-text)]">
            {kind === "Component" ? "Комплектующее" : "Основной товар"}
          </p>
        </div>

        {effectiveProductTypeId && (
          <section className="grid gap-4 rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4">
            <div>
              <h3 className="text-sm font-semibold text-[var(--app-text)]">
                Характеристики
              </h3>
              <p className="mt-1 text-xs leading-5 text-[var(--app-muted)]">
                Поля зависят от выбранного типа товара и сохраняются вместе с
                карточкой.
              </p>
            </div>

            {characteristicsQuery.isLoading && (
              <p className="text-sm text-[var(--app-muted)]">
                Загружаем характеристики типа...
              </p>
            )}

            {characteristicsQuery.isError && (
              <p role="alert" className="text-sm text-[var(--app-danger)]">
                {getApiErrorMessage(
                  characteristicsQuery.error,
                  "Не удалось загрузить характеристики типа товара.",
                )}
              </p>
            )}

            {!characteristicsQuery.isLoading &&
              !characteristicsQuery.isError &&
              characteristics.length === 0 && (
                <p className="text-sm text-[var(--app-muted)]">
                  Для этого типа характеристики не настроены.
                </p>
              )}

            <div className="grid gap-4">
              {characteristics.map((characteristic) => {
                const label = `${characteristic.name}${
                  characteristic.unit ? `, ${characteristic.unit}` : ""
                }${characteristic.isRequired ? " *" : ""}`;
                const value = characteristicValues[characteristic.code] ?? "";

                return (
                  <label
                    key={characteristic.id}
                    className="grid gap-2 text-sm font-medium text-[var(--app-text)]"
                  >
                    {label}
                    {characteristic.code === POLES_CHARACTERISTIC_CODE ? (
                      <AppSelect
                        ariaLabel={label}
                        expandInFlow
                        value={value}
                        onChange={(nextValue) => {
                          setCharacteristicValues((current) => ({
                            ...current,
                            [characteristic.code]: nextValue,
                          }));
                          setValidationError(null);
                        }}
                        options={[
                          { value: "", label: "Не указано" },
                          ...POLE_CONFIGURATION_OPTIONS,
                        ]}
                      />
                    ) : characteristic.dataType === "Boolean" ? (
                      <AppSelect
                        ariaLabel={label}
                        expandInFlow
                        value={value}
                        onChange={(nextValue) => {
                          setCharacteristicValues((current) => ({
                            ...current,
                            [characteristic.code]: nextValue,
                          }));
                          setValidationError(null);
                        }}
                        options={[
                          { value: "", label: "Не указано" },
                          { value: "true", label: "Да" },
                          { value: "false", label: "Нет" },
                        ]}
                      />
                    ) : (
                      <AppInput
                        type="text"
                        inputMode={
                          characteristic.dataType === "Number"
                            ? "decimal"
                            : "text"
                        }
                        value={value}
                        onChange={(event) => {
                          setCharacteristicValues((current) => ({
                            ...current,
                            [characteristic.code]: event.target.value,
                          }));
                          setValidationError(null);
                        }}
                        placeholder={
                          characteristic.dataType === "Number"
                            ? "Введите число"
                            : "Введите значение"
                        }
                      />
                    )}
                  </label>
                );
              })}
            </div>
          </section>
        )}

        <label className="grid gap-2 text-sm font-medium text-[var(--app-text)]">
          Цена для карточки товара
          <AppInput
            type="text"
            inputMode="decimal"
            value={price}
            onChange={(event) => setPrice(event.target.value)}
          />
          <span className="text-xs font-normal text-[var(--app-muted)]">
            Эта цена будет использована в расчёте, если для товара нет одной
            однозначной актуальной цены в прайсе производителя.
          </span>
        </label>

        <div
          className={
            initialProjectQuantity === null
              ? "grid gap-4"
              : "grid gap-4 sm:grid-cols-2"
          }
        >
          <label className="grid gap-2 text-sm font-medium text-[var(--app-text)]">
            Остаток на складе
            <AppInput
              type="text"
              inputMode="decimal"
              value={stockQuantity}
              onChange={(event) => setStockQuantity(event.target.value)}
            />
            <span className="text-xs font-normal text-[var(--app-muted)]">
              Это складской остаток карточки товара, а не количество в проекте.
            </span>
          </label>

          {initialProjectQuantity !== null && (
            <label className="grid gap-2 text-sm font-medium text-[var(--app-text)]">
              Количество в проекте
              <AppInput
                type="text"
                inputMode="decimal"
                value={projectQuantity}
                onChange={(event) => setProjectQuantity(event.target.value)}
              />
              <span className="text-xs font-normal text-[var(--app-muted)]">
                После создания товар сразу добавится в текущий расчёт.
              </span>
            </label>
          )}
        </div>

        {(validationError || createMutation.isError) && (
          <div
            role="alert"
            className="rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm text-[var(--app-danger)]"
          >
            {validationError ??
              getApiErrorMessage(
                createMutation.error,
                "Не удалось создать товар.",
              )}
          </div>
        )}

        <div className="flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
          <AppButton type="button" variant="secondary" onClick={onClose}>
            Отмена
          </AppButton>
          <AppButton
            type="submit"
            variant="primary"
            loading={createMutation.isPending}
            disabled={
              !article.trim() ||
              !name.trim() ||
              !effectiveManufacturerId ||
              !effectiveProductTypeId ||
              characteristicsQuery.isLoading ||
              characteristicsQuery.isError ||
              !characteristicsAreValid ||
              !Number.isFinite(Number(price.replace(",", "."))) ||
              Number(price.replace(",", ".")) < 0 ||
              !Number.isFinite(Number(stockQuantity.replace(",", "."))) ||
              Number(stockQuantity.replace(",", ".")) < 0 ||
              (initialProjectQuantity !== null &&
                (!Number.isFinite(Number(projectQuantity.replace(",", "."))) ||
                  Number(projectQuantity.replace(",", ".")) <= 0))
            }
          >
            Добавить в каталог
          </AppButton>
        </div>
      </form>
    </div>
  );
}
