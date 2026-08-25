"use client";

import { useQuery } from "@tanstack/react-query";
import { useState, type FormEvent } from "react";
import { getCatalogProductTypes } from "@/features/catalogMetadata/api/getCatalogProductTypes";
import { getCatalogProductTypeCharacteristicSchema } from "@/features/catalogProductTypes/api/getCatalogProductTypeCharacteristicSchema";
import { AppSelect } from "@/shared/ui/AppSelect";
import type {
  ApproveDictionarySuggestionRequest,
  AssistantDictionarySuggestion,
  DictionaryTermKind,
} from "../model/types";

const kindOptions: Array<{
  value: DictionaryTermKind;
  label: string;
}> = [
  {
    value: "Characteristic",
    label: "Характеристика товара",
  },
  {
    value: "Manufacturer",
    label: "Производитель",
  },
  {
    value: "ProductType",
    label: "Тип товара",
  },
  {
    value: "SearchToken",
    label: "Поисковый термин",
  },
];

interface DictionarySuggestionApprovalFormProps {
  suggestion: AssistantDictionarySuggestion;
  reviewComment: string;
  disabled: boolean;
  onReviewCommentChange: (value: string) => void;
  onApprove: (request: ApproveDictionarySuggestionRequest) => void;
}

export function DictionarySuggestionApprovalForm({
  suggestion,
  reviewComment,
  disabled,
  onReviewCommentChange,
  onApprove,
}: DictionarySuggestionApprovalFormProps) {
  const initialKind = isDictionaryTermKind(suggestion.suggestedKind)
    ? suggestion.suggestedKind
    : "Characteristic";

  const [phrase, setPhrase] = useState(suggestion.unknownPhrase);
  const [kind, setKind] = useState<DictionaryTermKind>(initialKind);
  const [targetCode, setTargetCode] = useState(
    suggestion.suggestedTargetCode ?? "",
  );
  const [targetValue, setTargetValue] = useState(
    suggestion.suggestedTargetValue,
  );
  const [productTypeCode, setProductTypeCode] = useState(
    suggestion.productTypeCode ?? "",
  );
  const [priority, setPriority] = useState("100");
  const [changeConfirmed, setChangeConfirmed] = useState(false);

  const productTypesQuery = useQuery({
    queryKey: ["catalog-product-types"],
    queryFn: getCatalogProductTypes,
    staleTime: 5 * 60 * 1000,
  });

  const characteristicSchemaQuery = useQuery({
    queryKey: ["catalog-product-type-characteristic-schema", productTypeCode],
    queryFn: () => getCatalogProductTypeCharacteristicSchema(productTypeCode),
    enabled: kind === "Characteristic" && productTypeCode.length > 0,
    staleTime: 5 * 60 * 1000,
  });

  const productTypes = productTypesQuery.data ?? [];
  const characteristics = characteristicSchemaQuery.data?.characteristics ?? [];

  const normalizedPhrase = phrase.trim();
  const normalizedTargetCode = targetCode.trim();
  const normalizedTargetValue = targetValue.trim();
  const numericPriority = Number(priority);

  const validationErrors: string[] = [];

  if (!normalizedPhrase) {
    validationErrors.push(
      "Укажите фразу, которая должна находиться в наименовании.",
    );
  }

  if (!normalizedTargetValue) {
    validationErrors.push("Укажите итоговое значение словарного правила.");
  }

  if (
    !Number.isInteger(numericPriority) ||
    numericPriority < 1 ||
    numericPriority > 10000
  ) {
    validationErrors.push("Priority должен быть целым числом от 1 до 10000.");
  }

  if (kind === "Characteristic" && !normalizedTargetCode) {
    validationErrors.push(
      "Для типа Characteristic необходимо выбрать характеристику.",
    );
  }

  if (
    kind === "Characteristic" &&
    productTypeCode &&
    characteristicSchemaQuery.data &&
    !characteristics.some(
      (characteristic) => characteristic.code === normalizedTargetCode,
    )
  ) {
    validationErrors.push(
      "Выбранная характеристика не входит в схему типа товара.",
    );
  }

  const referenceDataIsLoading =
    productTypesQuery.isLoading ||
    characteristicSchemaQuery.isLoading ||
    characteristicSchemaQuery.isFetching;

  const referenceDataHasError =
    productTypesQuery.isError || characteristicSchemaQuery.isError;

  const canSubmit =
    !disabled &&
    !referenceDataIsLoading &&
    !referenceDataHasError &&
    changeConfirmed &&
    validationErrors.length === 0;

  function handleKindChange(value: string): void {
    const nextKind = value as DictionaryTermKind;

    setKind(nextKind);

    if (nextKind !== "Characteristic") {
      setTargetCode("");
    }
  }

  function handleProductTypeChange(value: string): void {
    setProductTypeCode(value);

    if (kind === "Characteristic") {
      setTargetCode("");
    }
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    if (!canSubmit) {
      return;
    }

    onApprove({
      phrase: normalizedPhrase,
      kind,
      targetCode: kind === "Characteristic" ? normalizedTargetCode : null,
      targetValue: normalizedTargetValue,
      productTypeCode: productTypeCode || null,
      priority: numericPriority,
      reviewComment: reviewComment.trim() || null,
    });
  }

  return (
    <section className="mt-5 rounded-2xl border border-green-500/30 bg-green-500/[0.06] p-5">
      <div>
        <h4 className="text-lg font-semibold text-white">
          Изменить и одобрить
        </h4>

        <p className="mt-2 text-sm leading-6 text-slate-300">
          Поля заполнены предложением CRM, но окончательное правило определяет
          Technical-пользователь. Изменения в этой форме не перезаписывают
          исходное предложение.
        </p>
      </div>

      <div className="mt-4 rounded-2xl border border-amber-500/30 bg-amber-500/10 p-4">
        <p className="font-medium text-amber-100">
          Будет создано постоянное активное правило
        </p>

        <p className="mt-2 text-sm leading-6 text-amber-100/80">
          После одобрения Recognition Engine сможет использовать новый
          Approved-термин в следующих импортах и запросах ассистента.
        </p>
      </div>

      <form onSubmit={handleSubmit} className="mt-5 grid gap-5">
        <div className="grid gap-4 lg:grid-cols-2">
          <label className="grid gap-2">
            <span className="text-sm font-medium text-slate-300">
              Фраза в наименовании
            </span>

            <input
              value={phrase}
              disabled={disabled}
              onChange={(event) => setPhrase(event.target.value)}
              className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none focus:border-teal-400 disabled:opacity-50"
            />

            <span className="text-xs leading-5 text-slate-500">
              Движок будет искать именно эту фразу в наименовании товара.
            </span>
          </label>

          <div className="grid gap-2">
            <span className="text-sm font-medium text-slate-300">
              Тип знания
            </span>

            <AppSelect
              ariaLabel="Тип словарного знания"
              value={kind}
              disabled={disabled}
              onChange={handleKindChange}
              options={kindOptions}
            />

            <span className="text-xs leading-5 text-slate-500">
              Для характеристик обязательно указывается TargetCode.
            </span>
          </div>

          <div className="grid gap-2">
            <span className="text-sm font-medium text-slate-300">
              Область типа товара
            </span>

            <AppSelect
              ariaLabel="Область словарного правила"
              value={productTypeCode}
              disabled={disabled || productTypesQuery.isLoading}
              onChange={handleProductTypeChange}
              options={[
                {
                  value: "",
                  label: "Глобальное правило — все типы товаров",
                },
                ...productTypes.map((productType) => ({
                  value: productType.code,
                  label: `${productType.name} — ${productType.code}`,
                })),
              ]}
            />

            <span className="text-xs leading-5 text-slate-500">
              Для неоднозначных сокращений безопаснее выбирать конкретный тип
              товара.
            </span>
          </div>

          {kind === "Characteristic" && productTypeCode ? (
            <div className="grid gap-2">
              <span className="text-sm font-medium text-slate-300">
                Характеристика
              </span>

              <AppSelect
                ariaLabel="Характеристика словарного правила"
                value={targetCode}
                disabled={
                  disabled ||
                  characteristicSchemaQuery.isLoading ||
                  characteristicSchemaQuery.isError
                }
                onChange={setTargetCode}
                options={[
                  {
                    value: "",
                    label: "Выберите характеристику",
                  },
                  ...characteristics.map((characteristic) => ({
                    value: characteristic.code,
                    label: `${characteristic.name} — ${characteristic.code}`,
                  })),
                ]}
              />

              <span className="text-xs leading-5 text-slate-500">
                Список ограничен схемой выбранного типа товара.
              </span>
            </div>
          ) : kind === "Characteristic" ? (
            <label className="grid gap-2">
              <span className="text-sm font-medium text-slate-300">
                Код характеристики
              </span>

              <input
                value={targetCode}
                disabled={disabled}
                onChange={(event) => setTargetCode(event.target.value)}
                placeholder="Например: PRODUCT_SERIES"
                className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 font-mono text-slate-100 outline-none placeholder:text-slate-600 focus:border-teal-400 disabled:opacity-50"
              />

              <span className="text-xs leading-5 text-slate-500">
                Для глобального правила backend проверит существование
                характеристики.
              </span>
            </label>
          ) : (
            <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
              <p className="text-sm font-medium text-slate-300">
                TargetCode не используется
              </p>

              <p className="mt-2 text-xs leading-5 text-slate-500">
                Код характеристики нужен только для типа знания Characteristic.
              </p>
            </div>
          )}

          <label className="grid gap-2">
            <span className="text-sm font-medium text-slate-300">
              Итоговое значение
            </span>

            <input
              value={targetValue}
              disabled={disabled}
              onChange={(event) => setTargetValue(event.target.value)}
              className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none focus:border-teal-400 disabled:opacity-50"
            />

            <span className="text-xs leading-5 text-slate-500">
              Это значение Recognition Engine вернёт после совпадения фразы.
            </span>
          </label>

          <label className="grid gap-2">
            <span className="text-sm font-medium text-slate-300">Priority</span>

            <input
              type="number"
              min={1}
              max={10000}
              step={1}
              value={priority}
              disabled={disabled}
              onChange={(event) => setPriority(event.target.value)}
              className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none focus:border-teal-400 disabled:opacity-50"
            />

            <span className="text-xs leading-5 text-slate-500">
              При прочих равных правило с большим Priority имеет преимущество.
            </span>
          </label>
        </div>

        <label className="grid gap-2">
          <span className="text-sm font-medium text-slate-300">
            Комментарий решения
          </span>

          <textarea
            value={reviewComment}
            disabled={disabled}
            onChange={(event) => onReviewCommentChange(event.target.value)}
            placeholder="Объясните, почему правило подтверждено или как оно было исправлено."
            className="min-h-24 rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none placeholder:text-slate-600 focus:border-teal-400 disabled:opacity-50"
          />
        </label>

        <div className="rounded-2xl border border-teal-500/20 bg-black/20 p-4">
          <p className="text-sm font-semibold text-white">Итоговое правило</p>

          <p className="mt-2 break-words font-mono text-sm text-teal-200">
            {normalizedPhrase || "ФРАЗА"} →{" "}
            {kind === "Characteristic"
              ? `${normalizedTargetCode || "ХАРАКТЕРИСТИКА"} = `
              : ""}
            {normalizedTargetValue || "ЗНАЧЕНИЕ"}
          </p>

          <p className="mt-2 text-xs text-slate-500">
            Область: {productTypeCode || "Глобальная"} · Priority:{" "}
            {priority || "не указан"}
          </p>
        </div>

        {referenceDataHasError && (
          <div className="rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-200">
            Не удалось загрузить типы товаров или схему характеристик. Одобрение
            временно недоступно.
          </div>
        )}

        {validationErrors.length > 0 && (
          <div className="rounded-2xl border border-amber-500/30 bg-amber-500/10 p-4">
            <p className="font-medium text-amber-100">
              Проверьте окончательное правило
            </p>

            <ul className="mt-2 grid gap-1 text-sm text-amber-100/80">
              {validationErrors.map((error) => (
                <li key={error}>• {error}</li>
              ))}
            </ul>
          </div>
        )}

        <label className="flex items-start gap-3 rounded-2xl border border-white/10 bg-black/20 p-4">
          <input
            type="checkbox"
            checked={changeConfirmed}
            disabled={disabled}
            onChange={(event) => setChangeConfirmed(event.target.checked)}
            className="mt-1 size-4 accent-green-500"
          />

          <span className="text-sm leading-6 text-slate-300">
            Я проверил итоговую фразу, значение и область действия. Создать
            постоянный Approved-термин словаря.
          </span>
        </label>

        <div className="flex justify-end">
          <button
            type="submit"
            disabled={!canSubmit}
            className="rounded-xl bg-green-500 px-5 py-3 text-sm font-semibold text-white transition hover:bg-green-400 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {disabled ? "Сохраняем решение..." : "Создать правило и одобрить"}
          </button>
        </div>
      </form>
    </section>
  );
}

function isDictionaryTermKind(value: string): value is DictionaryTermKind {
  return kindOptions.some((option) => option.value === value);
}
