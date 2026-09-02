"use client";

import { useMutation } from "@tanstack/react-query";
import type { FormEvent } from "react";
import { useState } from "react";
import { exportCatalogRecognitionDataset } from "../api/exportCatalogRecognitionDataset";
import { RecognitionDatasetBundleExportSection } from "./RecognitionDatasetBundleExportSection";
import type {
  CatalogRecognitionDatasetExampleKind,
  CatalogRecognitionDatasetExportMetadata,
} from "../model/datasetTypes";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";

interface ExampleKindPresentation {
  kind: CatalogRecognitionDatasetExampleKind;
  label: string;
  description: string;
  colorClassName: string;
}

const exampleKindPresentations: ExampleKindPresentation[] = [
  {
    kind: "AcceptedSpan",
    label: "Подтверждённый span",
    description:
      "Recognition нашёл фрагмент, а пользователь подтвердил его без исправления.",
    colorClassName: "text-teal-300",
  },
  {
    kind: "AcceptedValue",
    label: "Подтверждённое значение",
    description:
      "Значение подтверждено, но точные координаты фрагмента отсутствуют.",
    colorClassName: "text-emerald-300",
  },
  {
    kind: "CorrectedValue",
    label: "Исправленное значение",
    description: "Пользователь заменил предложенное системой значение.",
    colorClassName: "text-blue-300",
  },
  {
    kind: "RejectedSpan",
    label: "Отклонённый span",
    description:
      "Найденный фрагмент подтверждён человеком как ошибочное распознавание.",
    colorClassName: "text-red-300",
  },
  {
    kind: "RejectedValue",
    label: "Отклонённое значение",
    description:
      "Предложение отклонено, но точных координат фрагмента не было.",
    colorClassName: "text-rose-300",
  },
  {
    kind: "ManualValue",
    label: "Добавлено вручную",
    description:
      "Recognition ничего не предложил, итоговое значение добавил пользователь.",
    colorClassName: "text-violet-300",
  },
  {
    kind: "ConflictResolution",
    label: "Разрешённый конфликт",
    description:
      "Пользователь выбрал итоговое значение при конфликте источников.",
    colorClassName: "text-amber-300",
  },
];

export function RecognitionDatasetExportPanel() {
  const [cutoffLocal, setCutoffLocal] = useState(() =>
    formatDateTimeLocal(new Date()),
  );
  const [validationError, setValidationError] = useState<string | null>(null);

  const exportMutation = useMutation({
    mutationFn: exportCatalogRecognitionDataset,
  });

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    const cutoff = new Date(cutoffLocal);

    if (Number.isNaN(cutoff.getTime())) {
      setValidationError("Укажите корректный момент отсечения.");
      return;
    }

    if (cutoff.getTime() > Date.now()) {
      setValidationError("Момент отсечения не может находиться в будущем.");
      return;
    }

    setValidationError(null);
    exportMutation.mutate(cutoff.toISOString());
  }

  return (
    <section className="rounded-3xl border border-cyan-500/25 bg-cyan-500/[0.05] p-6">
      <div>
        <h2 className="text-xl font-semibold text-white">ML-датасет</h2>

        <p className="mt-2 text-sm text-slate-400">
          Экспортирует только финализированные Feedback, которые разрешены для
          обучения. Одна строка JSONL соответствует одному человеческому
          решению.
        </p>
      </div>

      <div className="mt-5 rounded-2xl border border-amber-500/30 bg-amber-500/[0.08] p-4">
        <p className="font-medium text-amber-100">
          Экспорт не запускает обучение
        </p>

        <p className="mt-2 text-sm text-amber-200/80">
          Операция ничего не изменяет в CRM, не активирует ML-модель и не
          создаёт словарные правила. Она только формирует снимок подтверждённых
          данных.
        </p>
      </div>

      <form onSubmit={handleSubmit} className="mt-6 grid gap-4">
        <label className="grid gap-2">
          <span className="text-sm font-medium text-slate-300">
            Включить Feedback, финализированные до
          </span>

          <input
            type="datetime-local"
            step={1}
            max={formatDateTimeLocal(new Date())}
            value={cutoffLocal}
            onChange={(event) => {
              setCutoffLocal(event.target.value);
              setValidationError(null);
            }}
            className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none focus:border-cyan-400 focus:ring-2 focus:ring-cyan-400/20"
          />

          <p className="text-xs text-slate-500">
            Время вводится в часовом поясе вашего компьютера и перед отправкой
            преобразуется в UTC. Одинаковая граница и одинаковые данные дают
            одинаковый JSONL и SHA-256.
          </p>
        </label>

        {validationError && (
          <div className="rounded-2xl border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-200">
            {validationError}
          </div>
        )}

        <div>
          <button
            type="submit"
            disabled={exportMutation.isPending}
            className="rounded-2xl bg-cyan-500 px-5 py-3 text-sm font-semibold text-slate-950 transition hover:bg-cyan-400 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {exportMutation.isPending
              ? "Формируем JSONL..."
              : "Скачать ML-датасет"}
          </button>
        </div>
      </form>

      {exportMutation.isError && (
        <div className="mt-5 rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-200">
          {getApiErrorMessage(
            exportMutation.error,
            "Не удалось экспортировать ML-датасет.",
          )}
        </div>
      )}

      {exportMutation.data && (
        <DatasetExportResult metadata={exportMutation.data} />
      )}

      <RecognitionDatasetBundleExportSection
        cutoffLocal={cutoffLocal}
        onValidationError={setValidationError}
      />
    </section>
  );
}

function DatasetExportResult({
  metadata,
}: {
  metadata: CatalogRecognitionDatasetExportMetadata;
}) {
  return (
    <div className="mt-6 grid gap-5 border-t border-white/10 pt-6">
      <div>
        <h3 className="text-lg font-semibold text-teal-200">
          Датасет успешно сформирован
        </h3>

        <p className="mt-2 text-sm text-slate-400">
          Файл уже передан браузеру для скачивания. Ниже показаны метаданные
          именно этого экспорта.
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <MetadataCard label="Файл" value={metadata.fileName} monospace />
        <MetadataCard label="Версия формата" value={metadata.formatVersion} />
        <MetadataCard
          label="Всего примеров"
          value={metadata.exampleCount.toString()}
        />
        <MetadataCard
          label="Экспортирован"
          value={formatUtcDate(metadata.exportedAtUtc)}
        />
      </div>

      <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        {exampleKindPresentations.map((presentation) => (
          <div
            key={presentation.kind}
            className="rounded-2xl border border-white/10 bg-black/20 p-4"
          >
            <p className="text-sm text-slate-400">{presentation.label}</p>

            <p
              className={`mt-2 text-3xl font-semibold ${presentation.colorClassName}`}
            >
              {metadata.exampleCounts[presentation.kind]}
            </p>

            <p className="mt-2 text-xs leading-5 text-slate-500">
              {presentation.description}
            </p>
          </div>
        ))}
      </div>

      <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
        <p className="text-xs text-slate-500">Момент отсечения UTC</p>
        <p className="mt-2 font-mono text-sm text-slate-200">
          {metadata.finalizedUntilUtc}
        </p>
      </div>

      <div className="rounded-2xl border border-teal-500/25 bg-teal-500/[0.06] p-4">
        <p className="text-sm font-medium text-teal-100">
          SHA-256 содержимого JSONL
        </p>
        <p className="mt-2 break-all font-mono text-sm text-teal-200">
          {metadata.sha256}
        </p>
        <p className="mt-2 text-xs text-teal-100/70">
          Этот отпечаток позволяет доказать, что файл не изменился после
          экспорта.
        </p>
      </div>
    </div>
  );
}

function MetadataCard({
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
        className={`mt-2 break-all text-sm text-slate-200 ${monospace ? "font-mono" : ""}`}
      >
        {value}
      </p>
    </div>
  );
}

function formatDateTimeLocal(date: Date): string {
  const localDate = new Date(
    date.getTime() - date.getTimezoneOffset() * 60_000,
  );

  return localDate.toISOString().slice(0, 19);
}

function formatUtcDate(value: string): string {
  return `${new Intl.DateTimeFormat("ru-RU", {
    dateStyle: "medium",
    timeStyle: "medium",
    timeZone: "UTC",
  }).format(new Date(value))} UTC`;
}
