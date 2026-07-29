"use client";

import { useQuery } from "@tanstack/react-query";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { formatDate } from "@/shared/lib/formatters";
import { getCatalogImportBatchHistory } from "../api/getCatalogImportBatchHistory";
import type {
  CatalogImportBatchHistoryItem,
  CatalogImportHistoryEventType,
} from "../model/types";

interface CatalogImportBatchHistoryProps {
  batchId: string;
}

interface HistoryEventPresentation {
  title: string;
  markerClassName: string;
  borderClassName: string;
}

const presentations: Record<
  CatalogImportHistoryEventType,
  HistoryEventPresentation
> = {
  Uploaded: {
    title: "Пакет загружен",
    markerClassName: "bg-slate-400",
    borderClassName: "border-slate-500/30",
  },

  Submitted: {
    title: "Отправлен на проверку",
    markerClassName: "bg-blue-400",
    borderClassName: "border-blue-500/30",
  },

  ReviewStarted: {
    title: "Проверка начата",
    markerClassName: "bg-teal-400",
    borderClassName: "border-teal-500/30",
  },

  ChangesRequested: {
    title: "Возвращён на исправление",
    markerClassName: "bg-orange-400",
    borderClassName: "border-orange-500/30",
  },

  Rejected: {
    title: "Окончательно отклонён",
    markerClassName: "bg-red-400",
    borderClassName: "border-red-500/30",
  },

  Applied: {
    title: "Применён к каталогу",
    markerClassName: "bg-green-400",
    borderClassName: "border-green-500/30",
  },
};

function getActorLabel(item: CatalogImportBatchHistoryItem): string {
  const displayName = item.actorDisplayName?.trim() || "Пользователь не найден";

  const userType = item.actorUserType ? ` · ${item.actorUserType}` : "";

  return `${displayName}${userType}`;
}

export function CatalogImportBatchHistory({
  batchId,
}: CatalogImportBatchHistoryProps) {
  const historyQuery = useQuery({
    queryKey: ["catalog-import-batches", "history", batchId],
    queryFn: () => getCatalogImportBatchHistory(batchId),
    enabled: batchId.length > 0,
  });

  if (historyQuery.isLoading) {
    return (
      <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6 text-slate-300">
        Загружаем историю пакета...
      </section>
    );
  }

  if (historyQuery.isError) {
    return (
      <section className="rounded-3xl border border-red-500/30 bg-red-500/10 p-6 text-red-200">
        {getApiErrorMessage(
          historyQuery.error,
          "Не удалось загрузить историю пакета.",
        )}
      </section>
    );
  }

  const items = historyQuery.data?.items ?? [];

  return (
    <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
      <div>
        <h2 className="text-xl font-semibold text-white">История пакета</h2>

        <p className="mt-2 text-sm leading-6 text-slate-400">
          Основные этапы обработки и пользователи, принимавшие решения.
        </p>
      </div>

      {items.length === 0 ? (
        <p className="mt-5 text-sm text-slate-500">История пока отсутствует.</p>
      ) : (
        <ol className="mt-6 grid gap-4">
          {items.map((item, index) => {
            const presentation = presentations[item.eventType];

            return (
              <li
                key={[item.eventType, item.occurredAtUtc, index].join("-")}
                className={[
                  "relative rounded-2xl border",
                  "bg-black/20 p-5 pl-12",
                  presentation.borderClassName,
                ].join(" ")}
              >
                <span
                  className={[
                    "absolute left-5 top-6",
                    "h-3 w-3 rounded-full",
                    presentation.markerClassName,
                  ].join(" ")}
                />

                {index < items.length - 1 && (
                  <span className="absolute left-[25px] top-9 h-[calc(100%+17px)] w-px bg-white/10" />
                )}

                <div className="flex flex-col justify-between gap-2 md:flex-row md:items-start">
                  <div>
                    <h3 className="font-semibold text-white">
                      {presentation.title}
                    </h3>

                    <p className="mt-1 text-sm text-slate-400">
                      {getActorLabel(item)}
                    </p>

                    {item.actorEmail && (
                      <p className="mt-1 text-xs text-slate-600">
                        {item.actorEmail}
                      </p>
                    )}
                  </div>

                  <time
                    dateTime={item.occurredAtUtc}
                    className="whitespace-nowrap text-xs text-slate-500"
                  >
                    {formatDate(item.occurredAtUtc)}
                  </time>
                </div>

                {item.comment && (
                  <p className="mt-4 whitespace-pre-wrap rounded-xl bg-white/[0.04] p-4 text-sm leading-6 text-slate-300">
                    {item.comment}
                  </p>
                )}
              </li>
            );
          })}
        </ol>
      )}
    </section>
  );
}
