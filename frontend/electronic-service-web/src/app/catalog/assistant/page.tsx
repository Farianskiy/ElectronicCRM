"use client";

import { useMutation } from "@tanstack/react-query";
import axios from "axios";
import Link from "next/link";
import type { FormEvent } from "react";
import { useMemo, useState } from "react";
import { askCatalogAssistant } from "@/features/catalogAssistant/api/askCatalogAssistant";
import { createDictionarySuggestion } from "@/features/catalogAssistant/api/createDictionarySuggestion";
import type {
  AskCatalogAssistantResponse,
  CatalogAssistantCharacteristic,
  CatalogAssistantClarification,
  CatalogAssistantProduct,
} from "@/features/catalogAssistant/model/types";
import {
  getAuthSession,
  isTechnicalUser,
} from "@/shared/api/authToken";
import { RequireAuth } from "@/features/auth/ui/RequireAuth";
import { AppShell } from "@/widgets/appShell/AppShell";

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

  return "Произошла неизвестная ошибка.";
}

function getProducts(response?: AskCatalogAssistantResponse): CatalogAssistantProduct[] {
  if (!response) {
    return [];
  }

  return response.products ?? response.items ?? [];
}

function getClarification(
  response?: AskCatalogAssistantResponse,
): CatalogAssistantClarification | null {
  if (!response) {
    return null;
  }

  return response.clarification ?? response.parsedRequest?.clarification ?? null;
}

function formatPrice(value?: number | null): string {
  if (value === null || value === undefined) {
    return "—";
  }

  return new Intl.NumberFormat("ru-RU", {
    style: "currency",
    currency: "RUB",
    maximumFractionDigits: 2,
  }).format(value);
}

function formatStock(value?: number | null): string {
  if (value === null || value === undefined) {
    return "—";
  }

  return `${value}`;
}

function formatCharacteristicValue(
  characteristic: CatalogAssistantCharacteristic,
): string {
  const value = characteristic.value ?? "—";

  return value;
}

function getCharacteristicTitle(
  characteristic: CatalogAssistantCharacteristic,
): string {
  return characteristic.name ?? characteristic.code;
}

function hasCharacteristics(product: CatalogAssistantProduct): boolean {
  return Boolean(product.characteristics && product.characteristics.length > 0);
}

function AssistantUnderstandingBlock({
  response,
}: {
  response: AskCatalogAssistantResponse;
}) {
  const parsedRequest = response.parsedRequest;

  if (!parsedRequest) {
    return null;
  }

  return (
    <section className="rounded-2xl bg-white p-6 shadow">
      <h2 className="text-xl font-semibold text-slate-900">
        Как система поняла запрос
      </h2>

      <div className="mt-4 grid gap-3 text-sm md:grid-cols-2 lg:grid-cols-4">
        <div className="rounded-xl bg-slate-50 p-4">
          <p className="font-medium text-slate-500">Действие</p>
          <p className="mt-1 text-slate-900">{parsedRequest.intent ?? "—"}</p>
        </div>

        <div className="rounded-xl bg-slate-50 p-4">
          <p className="font-medium text-slate-500">Тип товара</p>
          <p className="mt-1 text-slate-900">
            {parsedRequest.productTypeCode ?? "—"}
          </p>
        </div>

        <div className="rounded-xl bg-slate-50 p-4">
          <p className="font-medium text-slate-500">Производитель</p>
          <p className="mt-1 text-slate-900">
            {parsedRequest.manufacturer ?? "—"}
          </p>
        </div>

        <div className="rounded-xl bg-slate-50 p-4">
          <p className="font-medium text-slate-500">Текстовый поиск</p>
          <p className="mt-1 text-slate-900">
            {parsedRequest.textQuery ?? "—"}
          </p>
        </div>
      </div>

      {parsedRequest.characteristics &&
        parsedRequest.characteristics.length > 0 && (
          <div className="mt-4">
            <p className="mb-2 text-sm font-medium text-slate-500">
              Найденные характеристики
            </p>

            <div className="flex flex-wrap gap-2">
              {parsedRequest.characteristics.map((characteristic) => (
                <span
                  key={`${characteristic.code}-${characteristic.value}`}
                  className="rounded-full bg-blue-50 px-3 py-1 text-sm text-blue-800"
                >
                  {characteristic.code}: {characteristic.value}
                </span>
              ))}
            </div>
          </div>
        )}
    </section>
  );
}

function ClarificationBlock({
  clarification,
  suggestionIsPending,
  suggestionIsSuccess,
  suggestionError,
  onCreateSuggestion,
}: {
  clarification: CatalogAssistantClarification;
  suggestionIsPending: boolean;
  suggestionIsSuccess: boolean;
  suggestionError: unknown | null;
  onCreateSuggestion: () => void;
}) {
  const suggestionErrorMessage =
    suggestionError === null || suggestionError === undefined
      ? null
      : getErrorMessage(suggestionError);

  return (
    <section className="rounded-2xl border border-amber-200 bg-amber-50 p-6 shadow">
      <div className="flex flex-col justify-between gap-4 md:flex-row md:items-start">
        <div>
          <h2 className="text-xl font-semibold text-amber-950">
            Нужно уточнение
          </h2>

          <p className="mt-2 text-amber-900">{clarification.question}</p>

          <div className="mt-4 grid gap-3 text-sm md:grid-cols-4">
            <div className="rounded-xl bg-white/70 p-3">
              <p className="font-medium text-amber-900">Неизвестное слово</p>
              <p className="mt-1 text-amber-950">
                {clarification.unknownPhrase}
              </p>
            </div>

            <div className="rounded-xl bg-white/70 p-3">
              <p className="font-medium text-amber-900">Тип</p>
              <p className="mt-1 text-amber-950">
                {clarification.suggestedKind}
              </p>
            </div>

            <div className="rounded-xl bg-white/70 p-3">
              <p className="font-medium text-amber-900">Предложенное значение</p>
              <p className="mt-1 text-amber-950">
                {clarification.suggestedTargetValue}
              </p>
            </div>

            <div className="rounded-xl bg-white/70 p-3">
              <p className="font-medium text-amber-900">Уверенность</p>
              <p className="mt-1 text-amber-950">
                {Math.round(clarification.confidence * 100)}%
              </p>
            </div>
          </div>
        </div>

        <button
          type="button"
          onClick={onCreateSuggestion}
          disabled={suggestionIsPending || suggestionIsSuccess}
          className="shrink-0 rounded-lg bg-amber-600 px-4 py-2 font-medium text-white disabled:opacity-60"
        >
          {suggestionIsPending
            ? "Отправляем..."
            : suggestionIsSuccess
              ? "Отправлено"
              : "Отправить предложение"}
        </button>
      </div>

      {suggestionIsSuccess && (
        <p className="mt-4 rounded-lg bg-green-100 p-3 text-sm text-green-800">
          Предложение отправлено техническому специалисту.
        </p>
      )}

      {suggestionErrorMessage && (
        <p className="mt-4 rounded-lg bg-red-100 p-3 text-sm text-red-700">
          {suggestionErrorMessage}
        </p>
      )}
    </section>
  );
}

function ProductCard({ product }: { product: CatalogAssistantProduct }) {
  return (
    <article className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
      <div className="flex flex-col justify-between gap-4 lg:flex-row lg:items-start">
        <div>
          <h3 className="text-lg font-semibold text-slate-900">
            {product.name}
          </h3>

          <p className="mt-1 text-sm text-slate-500">
            Артикул: {product.article ?? "—"}
          </p>
        </div>

        {product.score !== null && product.score !== undefined && (
          <span className="w-fit rounded-full bg-slate-100 px-3 py-1 text-sm font-medium text-slate-700">
            score: {product.score}
          </span>
        )}
      </div>

      <div className="mt-4 grid gap-3 text-sm md:grid-cols-4">
        <div className="rounded-xl bg-slate-50 p-3">
          <p className="font-medium text-slate-500">Производитель</p>
          <p className="mt-1 text-slate-900">
            {product.manufacturerName ?? product.manufacturer ?? "—"}
          </p>
        </div>

        <div className="rounded-xl bg-slate-50 p-3">
          <p className="font-medium text-slate-500">Тип товара</p>
          <p className="mt-1 text-slate-900">
            {product.productTypeName ?? product.productType ?? "—"}
          </p>
        </div>

        <div className="rounded-xl bg-slate-50 p-3">
          <p className="font-medium text-slate-500">Цена</p>
          <p className="mt-1 text-slate-900">{formatPrice(product.price)}</p>
        </div>

        <div className="rounded-xl bg-slate-50 p-3">
          <p className="font-medium text-slate-500">Остаток</p>
          <p className="mt-1 text-slate-900">
            {formatStock(product.stockQuantity)}
          </p>
        </div>
      </div>

      <details className="mt-4 rounded-xl border border-slate-200 bg-slate-50 p-4">
        <summary className="cursor-pointer font-medium text-slate-900">
          Характеристики
        </summary>

        {!hasCharacteristics(product) ? (
          <p className="mt-3 text-sm text-slate-600">
            В ответе assistant-а характеристики для этого товара не пришли.
            Следующим этапом можно подключить загрузку полной карточки товара по
            id.
          </p>
        ) : (
          <div className="mt-4 grid gap-2 md:grid-cols-2">
            {product.characteristics?.map((characteristic) => (
              <div
                key={`${product.id}-${characteristic.code}-${characteristic.value}`}
                className="flex justify-between gap-4 rounded-lg bg-white px-3 py-2 text-sm"
              >
                <span className="font-medium text-slate-600">
                  {getCharacteristicTitle(characteristic)}
                </span>
                <span className="text-right text-slate-900">
                  {formatCharacteristicValue(characteristic)}
                </span>
              </div>
            ))}
          </div>
        )}
      </details>
    </article>
  );
}

export default function CatalogAssistantPage() {
  const [message, setMessage] = useState("найди автомат чент 1п 16а");
  const [onlyInStock, setOnlyInStock] = useState(false);
  const [minimumScore, setMinimumScore] = useState(70);
  const [pageSize, setPageSize] = useState(20);
  const session = getAuthSession();
  const technical = isTechnicalUser(session);

  const assistantMutation = useMutation({
    mutationFn: askCatalogAssistant,
  });

  const suggestionMutation = useMutation({
    mutationFn: createDictionarySuggestion,
  });

  const response = assistantMutation.data;
  const products = useMemo(() => getProducts(response), [response]);
  const clarification = useMemo(() => getClarification(response), [response]);

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    suggestionMutation.reset();

    assistantMutation.mutate({
      message,
      onlyInStock,
      minimumScore,
      page: 1,
      pageSize,
    });
  }

  function handleCreateSuggestion() {
    if (!clarification) {
      return;
    }

    suggestionMutation.mutate({
      originalMessage: message,
      unknownPhrase: clarification.unknownPhrase,
      suggestedKind: clarification.suggestedKind,
      suggestedTargetCode: clarification.suggestedTargetCode,
      suggestedTargetValue: clarification.suggestedTargetValue,
      confidence: clarification.confidence,
    });
  }

  return (
    <main className="min-h-screen bg-slate-100 p-8">
      <div className="mx-auto grid max-w-7xl gap-6">
        <header className="flex items-center justify-between gap-6">
          <div>
            <h1 className="text-3xl font-bold text-slate-900">
              Catalog Assistant
            </h1>
            <p className="mt-2 text-slate-600">
              Поиск товаров через текстовый запрос.
            </p>
          </div>

          <Link
            href="/"
            className="rounded-lg bg-slate-900 px-4 py-2 text-white"
          >
            На главную
          </Link>
        </header>

        <section className="rounded-2xl bg-white p-6 shadow">
          <form onSubmit={handleSubmit} className="grid gap-4">
            <label className="grid gap-2">
              <span className="text-sm font-medium text-slate-700">
                Что нужно найти?
              </span>
              <textarea
                className="min-h-28 rounded-lg border border-slate-300 px-3 py-2 text-slate-900 outline-none focus:border-blue-500"
                value={message}
                onChange={(event) => setMessage(event.target.value)}
              />
            </label>

            <div className="grid gap-4 md:grid-cols-3">
              <label className="flex items-center gap-2 rounded-xl bg-slate-50 p-3">
                <input
                  type="checkbox"
                  checked={onlyInStock}
                  onChange={(event) => setOnlyInStock(event.target.checked)}
                />
                <span className="text-sm text-slate-700">
                  Только в наличии
                </span>
              </label>

              <label className="grid gap-2">
                <span className="text-sm font-medium text-slate-700">
                  Минимальное совпадение
                </span>
                <input
                  className="rounded-lg border border-slate-300 px-3 py-2 text-slate-900"
                  type="number"
                  min={0}
                  max={100}
                  value={minimumScore}
                  onChange={(event) => setMinimumScore(Number(event.target.value))}
                />
              </label>

              <label className="grid gap-2">
                <span className="text-sm font-medium text-slate-700">
                  Сколько товаров показать
                </span>
                <input
                  className="rounded-lg border border-slate-300 px-3 py-2 text-slate-900"
                  type="number"
                  min={1}
                  max={100}
                  value={pageSize}
                  onChange={(event) => setPageSize(Number(event.target.value))}
                />
              </label>
            </div>

            {assistantMutation.isError && (
              <p className="rounded-lg bg-red-50 p-3 text-sm text-red-700">
                {getErrorMessage(assistantMutation.error)}
              </p>
            )}

            <button
              type="submit"
              disabled={assistantMutation.isPending}
              className="w-fit rounded-lg bg-blue-600 px-5 py-2 font-medium text-white disabled:opacity-60"
            >
              {assistantMutation.isPending ? "Ищем..." : "Найти"}
            </button>
          </form>
        </section>

        {response && clarification && (
          <ClarificationBlock
            clarification={clarification}
            suggestionIsPending={suggestionMutation.isPending}
            suggestionIsSuccess={suggestionMutation.isSuccess}
            suggestionError={suggestionMutation.error ?? null}
            onCreateSuggestion={handleCreateSuggestion}
          />
        )}

        {response && technical && <AssistantUnderstandingBlock response={response} />}

        {response && response.answer && (
          <section className="rounded-2xl bg-blue-50 p-5 text-blue-950 shadow">
            {response.answer}
          </section>
        )}

        {response && (
          <section className="rounded-2xl bg-white p-6 shadow">
            <div className="flex items-center justify-between gap-4">
              <div>
                <h2 className="text-xl font-semibold text-slate-900">
                  Найденные товары
                </h2>
                <p className="mt-1 text-sm text-slate-600">
                  Количество: {products.length}
                </p>
              </div>
            </div>

            {products.length === 0 ? (
              <p className="mt-6 text-slate-600">Товары не найдены.</p>
            ) : (
              <div className="mt-6 grid gap-4">
                {products.map((product) => (
                  <ProductCard key={product.id} product={product} />
                ))}
              </div>
            )}
          </section>
        )}

        {response && technical && (
          <details className="rounded-2xl bg-slate-900 p-6 text-slate-100 shadow">
            <summary className="cursor-pointer font-medium">
              Техническая информация
            </summary>

            <pre className="mt-4 max-h-[500px] overflow-auto text-sm">
              {JSON.stringify(response, null, 2)}
            </pre>
          </details>
        )}
      </div>
    </main>
  );
}