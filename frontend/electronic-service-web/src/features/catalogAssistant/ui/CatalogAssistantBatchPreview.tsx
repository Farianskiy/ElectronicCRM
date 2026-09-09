import Link from "next/link";
import type {
  CatalogAssistantBatchLine,
  PreviewCatalogAssistantBatchResponse,
} from "../model/types";
import type { CatalogPriceCalculationListItem } from "@/features/catalogPriceCalculations/model/types";
import { formatPrice } from "@/shared/lib/formatters";

function getStatusLabel(status: CatalogAssistantBatchLine["status"]): string {
  switch (status) {
    case "Matched":
      return "Сопоставлено";
    case "MultipleMatches":
      return "Нужно выбрать";
    case "NotFound":
      return "Не найдено";
    case "NeedsClarification":
      return "Нужно уточнение";
    case "MissingQuantity":
      return "Укажите количество";
    default:
      return "Некорректно";
  }
}

export function CatalogAssistantBatchPreview({
  preview,
  calculations,
  selectedCalculationId,
  selectedProducts,
  onCalculationChange,
  onProductSelect,
  onApply,
  isApplying,
  isApplied,
  applyErrorMessage,
}: {
  preview: PreviewCatalogAssistantBatchResponse;
  calculations: CatalogPriceCalculationListItem[];
  selectedCalculationId: string;
  selectedProducts: Record<number, string>;
  onCalculationChange: (calculationId: string) => void;
  onProductSelect: (lineNumber: number, productId: string) => void;
  onApply: () => void;
  isApplying: boolean;
  isApplied: boolean;
  applyErrorMessage: string | null;
}) {
  const selectedLinesCount = preview.lines.filter(
    (line) => line.quantity !== null && selectedProducts[line.lineNumber],
  ).length;

  return (
    <section className="grid gap-5 rounded-3xl border border-white/10 bg-white/[0.04] p-6">
      <div>
        <h2 className="text-xl font-semibold text-white">
          Разобранные позиции
        </h2>

        {preview.commonText && (
          <p className="mt-2 text-sm text-slate-400">
            Общие условия: {preview.commonText}
          </p>
        )}
      </div>

      <div className="grid gap-3 md:grid-cols-3">
        <SummaryCard label="Позиций" value={preview.totalLines} />
        <SummaryCard label="Сопоставлено" value={preview.matchedLines} />
        <SummaryCard
          label="Требуют внимания"
          value={preview.requiresAttentionLines}
        />
      </div>

      <div className="grid gap-3 rounded-2xl border border-white/10 bg-black/20 p-4 md:grid-cols-[minmax(0,1fr)_auto] md:items-end">
        <label className="grid gap-2">
          <span className="text-sm font-medium text-slate-300">
            Проект для добавления
          </span>

          <select
            value={selectedCalculationId}
            onChange={(event) => onCalculationChange(event.target.value)}
            className="rounded-2xl border border-white/10 bg-slate-950 px-4 py-3 text-slate-100 outline-none focus:border-teal-400"
          >
            <option value="">Выберите черновик проекта</option>

            {calculations.map((calculation) => (
              <option
                key={calculation.calculationId}
                value={calculation.calculationId}
              >
                {calculation.title}
              </option>
            ))}
          </select>
        </label>

        <button
          type="button"
          onClick={onApply}
          disabled={
            isApplying || !selectedCalculationId || selectedLinesCount === 0
          }
          className="rounded-2xl bg-teal-500 px-5 py-3 text-sm font-medium text-white disabled:opacity-60"
        >
          {isApplying
            ? "Добавляем..."
            : `Добавить выбранные (${selectedLinesCount})`}
        </button>
      </div>

      {isApplied && (
        <p className="rounded-2xl border border-green-500/30 bg-green-500/10 p-3 text-sm text-green-200">
          Позиции успешно добавлены в проект.
        </p>
      )}

      {applyErrorMessage && (
        <p className="rounded-2xl border border-red-500/30 bg-red-500/10 p-3 text-sm text-red-200">
          {applyErrorMessage}
        </p>
      )}

      <div className="grid gap-4">
        {preview.lines.map((line) => (
          <article
            key={line.lineNumber}
            className="rounded-2xl border border-white/10 bg-black/20 p-4"
          >
            <div className="flex flex-col justify-between gap-3 md:flex-row md:items-start">
              <div>
                <p className="text-sm font-semibold text-white">
                  Позиция {line.lineNumber}: {line.sourceText}
                </p>

                <p className="mt-1 text-xs text-slate-400">
                  Количество: {line.quantity ?? "не указано"}
                </p>
              </div>

              <span className="w-fit rounded-full bg-teal-500/15 px-3 py-1 text-xs font-medium text-teal-300">
                {getStatusLabel(line.status)}
              </span>
            </div>

            <p className="mt-3 text-sm text-slate-300">{line.message}</p>

            {line.products.length > 0 && (
              <div className="mt-4 grid gap-3">
                {line.products.map((product) => {
                  const selected =
                    selectedProducts[line.lineNumber] === product.id;

                  return (
                    <div
                      key={product.id}
                      className={`rounded-2xl border p-4 ${
                        selected
                          ? "border-teal-400 bg-teal-500/10"
                          : "border-white/10 bg-white/[0.03]"
                      }`}
                    >
                      <div className="flex flex-col justify-between gap-3 lg:flex-row lg:items-start">
                        <div>
                          <p className="font-semibold text-white">
                            {product.name}
                          </p>

                          <p className="mt-1 text-xs text-slate-400">
                            {product.article} · {product.manufacturerName}
                          </p>

                          <p className="mt-1 text-xs text-slate-400">
                            {formatPrice(
                              product.priceAmount,
                              product.priceCurrency,
                            )}{" "}
                            · остаток {product.stockQuantity}
                          </p>
                        </div>

                        <div className="flex gap-2">
                          <button
                            type="button"
                            onClick={() =>
                              onProductSelect(line.lineNumber, product.id)
                            }
                            className="rounded-xl bg-teal-500 px-4 py-2 text-sm font-medium text-white"
                          >
                            {selected ? "Выбрано" : "Выбрать"}
                          </button>

                          <Link
                            href={`/catalog/products/${product.id}`}
                            className="rounded-xl border border-white/10 px-4 py-2 text-sm text-slate-200"
                          >
                            Карточка
                          </Link>
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </article>
        ))}
      </div>
    </section>
  );
}

function SummaryCard({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-2xl border border-white/10 bg-black/20 p-3">
      <p className="text-xs text-slate-400">{label}</p>
      <p className="mt-1 text-sm font-semibold text-white">{value}</p>
    </div>
  );
}
