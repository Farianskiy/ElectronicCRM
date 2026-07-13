"use client";

import { useMutation } from "@tanstack/react-query";
import axios from "axios";
import Link from "next/link";
import type { FormEvent } from "react";
import { useState } from "react";
import { askCatalogAssistant } from "@/features/catalogAssistant/api/askCatalogAssistant";
import { createDictionarySuggestion } from "@/features/catalogAssistant/api/createDictionarySuggestion";
import type {
  AskCatalogAssistantResponse,
  CatalogAssistantClarification,
  CatalogAssistantProduct,
  CatalogAssistantReplacement,
} from "@/features/catalogAssistant/model/types";
import { useAuthSession } from "@/features/auth/model/useAuthSession";
import { isTechnicalUser } from "@/shared/api/authToken";
import { formatPercent, formatPrice } from "@/shared/lib/formatters";
import { PageHeader } from "@/shared/ui/PageHeader";

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

function ProductCard({
  product,
  score,
}: {
  product: CatalogAssistantProduct;
  score?: number;
}) {
  return (
    <article className="rounded-3xl border border-white/10 bg-white/[0.04] p-5">
      <div className="flex flex-col justify-between gap-4 lg:flex-row lg:items-start">
        <div>
          <h3 className="text-lg font-semibold text-white">{product.name}</h3>

          <p className="mt-1 text-sm text-slate-400">
            Артикул: {product.article}
          </p>
        </div>

        {score !== undefined && (
          <span className="w-fit rounded-full bg-teal-500/15 px-3 py-1 text-xs font-medium text-teal-300">
            score: {score}
          </span>
        )}
      </div>

      <div className="mt-4 grid gap-3 md:grid-cols-4">
        <InfoCard label="Производитель" value={product.manufacturerName} />
        <InfoCard label="Тип" value={product.productTypeName} />
        <InfoCard
          label="Цена"
          value={formatPrice(product.priceAmount, product.priceCurrency)}
        />
        <InfoCard label="Остаток" value={`${product.stockQuantity}`} />
      </div>

      <div className="mt-4">
        <Link
          href={`/catalog/products/${product.id}`}
          className="inline-flex rounded-xl bg-white/[0.06] px-4 py-2 text-sm font-medium text-slate-200 transition hover:bg-teal-500 hover:text-white"
        >
          Открыть карточку
        </Link>
      </div>
    </article>
  );
}

function InfoCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-white/10 bg-black/20 p-3">
      <p className="text-xs text-slate-400">{label}</p>
      <p className="mt-1 text-sm font-semibold text-white">{value}</p>
    </div>
  );
}

function ClarificationBlock({
  clarification,
  onCreateSuggestion,
  isPending,
  isSuccess,
  error,
}: {
  clarification: CatalogAssistantClarification;
  onCreateSuggestion: () => void;
  isPending: boolean;
  isSuccess: boolean;
  error: unknown | null;
}) {
  const errorMessage =
    error === null || error === undefined ? null : getErrorMessage(error);

  return (
    <section className="rounded-3xl border border-amber-500/30 bg-amber-500/10 p-6">
      <div className="flex flex-col justify-between gap-5 lg:flex-row lg:items-start">
        <div>
          <h2 className="text-xl font-semibold text-amber-100">
            Нужно уточнение
          </h2>

          <p className="mt-2 text-sm text-amber-200">
            {clarification.question}
          </p>

          <div className="mt-5 grid gap-3 md:grid-cols-4">
            <InfoCard label="Неизвестное слово" value={clarification.unknownPhrase} />
            <InfoCard label="Тип" value={clarification.suggestedKind} />
            <InfoCard
              label="Значение"
              value={clarification.suggestedTargetValue}
            />
            <InfoCard
              label="Уверенность"
              value={formatPercent(clarification.confidence)}
            />
          </div>
        </div>

        <button
          type="button"
          onClick={onCreateSuggestion}
          disabled={isPending || isSuccess}
          className="w-fit rounded-2xl bg-amber-500 px-5 py-3 text-sm font-medium text-slate-950 disabled:opacity-60"
        >
          {isPending
            ? "Отправляем..."
            : isSuccess
              ? "Отправлено"
              : "Отправить предложение"}
        </button>
      </div>

      {isSuccess && (
        <p className="mt-4 rounded-2xl border border-green-500/30 bg-green-500/10 p-3 text-sm text-green-200">
          Предложение отправлено техническому специалисту.
        </p>
      )}

      {errorMessage && (
        <p className="mt-4 rounded-2xl border border-red-500/30 bg-red-500/10 p-3 text-sm text-red-200">
          {errorMessage}
        </p>
      )}
    </section>
  );
}

function TechnicalParsedRequestBlock({
  response,
}: {
  response: AskCatalogAssistantResponse;
}) {
  return (
    <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
      <h2 className="text-xl font-semibold text-white">
        Техническая информация
      </h2>

      <div className="mt-5 grid gap-3 md:grid-cols-4">
        <InfoCard label="Intent" value={response.parsedRequest.intent} />
        <InfoCard
          label="Search"
          value={response.parsedRequest.search ?? "—"}
        />
        <InfoCard
          label="Product type"
          value={response.parsedRequest.productTypeCode ?? "—"}
        />
        <InfoCard
          label="Manufacturer"
          value={response.parsedRequest.manufacturer ?? "—"}
        />
      </div>

      {response.parsedRequest.characteristics.length > 0 && (
        <div className="mt-5">
          <p className="mb-2 text-sm font-medium text-slate-300">
            Распознанные характеристики
          </p>

          <div className="flex flex-wrap gap-2">
            {response.parsedRequest.characteristics.map((characteristic) => (
              <span
                key={`${characteristic.code}-${characteristic.value}`}
                className="rounded-full bg-teal-500/15 px-3 py-1 text-sm text-teal-300"
              >
                {characteristic.code}: {characteristic.value}
              </span>
            ))}
          </div>
        </div>
      )}

      <details className="mt-5 rounded-2xl border border-white/10 bg-black/30 p-4">
        <summary className="cursor-pointer text-sm font-medium text-slate-200">
          Raw JSON
        </summary>

        <pre className="mt-4 max-h-[520px] overflow-auto text-xs text-slate-300">
          {JSON.stringify(response, null, 2)}
        </pre>
      </details>
    </section>
  );
}

export default function CatalogAssistantPage() {
  const session = useAuthSession();
  const technical = isTechnicalUser(session);

  const [message, setMessage] = useState("найди автомат чент 1п 16а");
  const [onlyInStock, setOnlyInStock] = useState(false);
  const [minimumScore, setMinimumScore] = useState(70);
  const [pageSize, setPageSize] = useState(20);

  const assistantMutation = useMutation({
    mutationFn: askCatalogAssistant,
  });

  const suggestionMutation = useMutation({
    mutationFn: createDictionarySuggestion,
  });

  const response = assistantMutation.data;
  const clarification = response?.parsedRequest.clarification ?? null;

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
    <div className="grid gap-6">
      <PageHeader
        title="Catalog Assistant"
        description="Поиск товаров через естественный текстовый запрос."
      />

      <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-5">
        <form onSubmit={handleSubmit} className="grid gap-4">
          <label className="grid gap-2">
            <span className="text-sm font-medium text-slate-300">
              Что нужно найти?
            </span>

            <textarea
              value={message}
              onChange={(event) => setMessage(event.target.value)}
              className="min-h-28 rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none placeholder:text-slate-600 focus:border-teal-400"
            />
          </label>

          <div className="grid gap-4 md:grid-cols-3">
            <label className="flex items-center gap-3 rounded-2xl border border-white/10 bg-black/20 px-4 py-3">
              <input
                type="checkbox"
                checked={onlyInStock}
                onChange={(event) => setOnlyInStock(event.target.checked)}
              />
              <span className="text-sm text-slate-300">Только в наличии</span>
            </label>

            <label className="grid gap-2">
              <span className="text-sm font-medium text-slate-300">
                Минимальное совпадение
              </span>
              <input
                type="number"
                min={0}
                max={100}
                value={minimumScore}
                onChange={(event) => setMinimumScore(Number(event.target.value))}
                className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none focus:border-teal-400"
              />
            </label>

            <label className="grid gap-2">
              <span className="text-sm font-medium text-slate-300">
                Сколько товаров показать
              </span>
              <input
                type="number"
                min={1}
                max={100}
                value={pageSize}
                onChange={(event) => setPageSize(Number(event.target.value))}
                className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none focus:border-teal-400"
              />
            </label>
          </div>

          {assistantMutation.isError && (
            <p className="rounded-2xl border border-red-500/30 bg-red-500/10 p-3 text-sm text-red-200">
              {getErrorMessage(assistantMutation.error)}
            </p>
          )}

          <button
            type="submit"
            disabled={assistantMutation.isPending}
            className="w-fit rounded-2xl bg-teal-500 px-5 py-3 text-sm font-medium text-white disabled:opacity-60"
          >
            {assistantMutation.isPending ? "Ищем..." : "Найти"}
          </button>
        </form>
      </section>

      {response && clarification && (
        <ClarificationBlock
          clarification={clarification}
          onCreateSuggestion={handleCreateSuggestion}
          isPending={suggestionMutation.isPending}
          isSuccess={suggestionMutation.isSuccess}
          error={suggestionMutation.error ?? null}
        />
      )}

      {response?.answer && (
        <section className="rounded-3xl border border-blue-500/30 bg-blue-500/10 p-5 text-blue-100">
          {response.answer}
        </section>
      )}

      {response && response.sourceProduct && (
        <section className="grid gap-4">
          <h2 className="text-xl font-semibold text-white">Исходный товар</h2>
          <ProductCard product={response.sourceProduct} />
        </section>
      )}

      {response && response.replacements.length > 0 && (
        <section className="grid gap-4">
          <h2 className="text-xl font-semibold text-white">Замены</h2>

          {response.replacements.map((replacement: CatalogAssistantReplacement) => (
            <ProductCard
              key={replacement.id}
              product={replacement}
              score={replacement.replacementScore}
            />
          ))}
        </section>
      )}

      {response && response.products.length > 0 && (
        <section className="grid gap-4">
          <h2 className="text-xl font-semibold text-white">Найденные товары</h2>

          {response.products.map((product) => (
            <ProductCard key={product.id} product={product} />
          ))}
        </section>
      )}

      {response &&
        response.products.length === 0 &&
        response.replacements.length === 0 &&
        !response.needsClarification && (
          <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
            <h2 className="text-xl font-semibold text-white">
              Товары не найдены
            </h2>
            <p className="mt-2 text-sm text-slate-400">
              Попробуй изменить запрос или отключить фильтр наличия.
            </p>
          </section>
        )}

      {response && technical && <TechnicalParsedRequestBlock response={response} />}
    </div>
  );
}