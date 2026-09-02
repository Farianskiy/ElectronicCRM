"use client";

import { useMutation } from "@tanstack/react-query";
import { exportCatalogRecognitionDatasetBundle } from "../api/exportCatalogRecognitionDatasetBundle";
import type {
  CatalogRecognitionDatasetBundleExportMetadata,
  CatalogRecognitionDatasetSplit,
} from "../model/datasetTypes";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";

interface RecognitionDatasetBundleExportSectionProps {
  cutoffLocal: string;
  onValidationError: (message: string | null) => void;
}

export function RecognitionDatasetBundleExportSection({
  cutoffLocal,
  onValidationError,
}: RecognitionDatasetBundleExportSectionProps) {
  const exportMutation = useMutation({
    mutationFn: exportCatalogRecognitionDatasetBundle,
  });

  function handleExport(): void {
    const cutoff = new Date(cutoffLocal);

    if (Number.isNaN(cutoff.getTime())) {
      onValidationError("Укажите корректный момент отсечения.");
      return;
    }

    if (cutoff.getTime() > Date.now()) {
      onValidationError("Момент отсечения не может находиться в будущем.");
      return;
    }

    onValidationError(null);
    exportMutation.mutate(cutoff.toISOString());
  }

  return (
    <div className="mt-6 grid gap-5 border-t border-white/10 pt-6">
      <div>
        <h3 className="text-lg font-semibold text-white">
          Замороженный Train / Validation / Test пакет
        </h3>

        <p className="mt-2 text-sm leading-6 text-slate-400">
          Использует тот же момент отсечения, но разделяет примеры по группам
          товаров. Примеры одного товара не попадут одновременно в обучение и
          независимую проверку.
        </p>
      </div>

      <div className="rounded-2xl border border-violet-500/25 bg-violet-500/[0.07] p-4">
        <p className="font-medium text-violet-100">
          Почему этот пакет называется замороженным
        </p>

        <p className="mt-2 text-sm leading-6 text-violet-200/75">
          Одинаковые данные, одинаковый cutoff и одинаковая версия алгоритма
          разделения дают одинаковые части Train, Validation и Test. Это
          позволяет воспроизвести обучение и проверить, на каком именно наборе
          была оценена модель.
        </p>
      </div>

      <div>
        <button
          type="button"
          disabled={exportMutation.isPending}
          onClick={handleExport}
          className="rounded-2xl bg-violet-500 px-5 py-3 text-sm font-semibold text-white transition hover:bg-violet-400 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {exportMutation.isPending
            ? "Формируем ZIP-пакет..."
            : "Скачать замороженный Train / Validation / Test пакет"}
        </button>
      </div>

      {exportMutation.isError && (
        <div className="rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-200">
          {getApiErrorMessage(
            exportMutation.error,
            "Не удалось сформировать замороженный датасет.",
          )}
        </div>
      )}

      {exportMutation.data && (
        <DatasetBundleExportResult metadata={exportMutation.data} />
      )}
    </div>
  );
}

function DatasetBundleExportResult({
  metadata,
}: {
  metadata: CatalogRecognitionDatasetBundleExportMetadata;
}) {
  const manifest = metadata.manifest;

  return (
    <div className="grid gap-5">
      <div>
        <h3 className="text-lg font-semibold text-violet-200">
          Замороженный пакет сформирован
        </h3>

        <p className="mt-2 text-sm text-slate-400">
          ZIP уже передан браузеру. Показанные ниже сведения получены из
          manifest этого пакета.
        </p>
      </div>

      {manifest.warnings.length > 0 ? (
        <div className="rounded-2xl border border-amber-500/35 bg-amber-500/[0.09] p-5">
          <p className="font-semibold text-amber-100">
            Датасет сформирован, но требует внимания
          </p>

          <div className="mt-4 grid gap-3">
            {manifest.warnings.map((warning) => (
              <div
                key={warning.code}
                className="rounded-xl border border-amber-500/20 bg-black/20 p-4"
              >
                <p className="font-mono text-xs text-amber-300">
                  {warning.code}
                </p>

                <p className="mt-2 text-sm leading-6 text-amber-100/85">
                  {warning.message}
                </p>
              </div>
            ))}
          </div>

          <p className="mt-4 text-xs leading-5 text-amber-200/70">
            Предупреждение не запрещает скачать пакет. Оно означает, что данных
            пока недостаточно для надёжного обучения или независимой оценки
            модели.
          </p>
        </div>
      ) : (
        <div className="rounded-2xl border border-emerald-500/30 bg-emerald-500/[0.08] p-4">
          <p className="font-medium text-emerald-100">
            Manifest не содержит предупреждений
          </p>

          <p className="mt-2 text-sm text-emerald-200/75">
            Все части пакета содержат примеры, а количество групп товаров
            достигло минимального рекомендуемого значения.
          </p>
        </div>
      )}

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <MetadataCard label="Файл" value={metadata.fileName} monospace />
        <MetadataCard label="Версия ZIP" value={manifest.bundleFormatVersion} />
        <MetadataCard
          label="Версия JSONL"
          value={manifest.datasetFormatVersion}
        />
        <MetadataCard
          label="Версия разделения"
          value={manifest.splitAlgorithmVersion}
        />
        <MetadataCard
          label="Всего примеров"
          value={manifest.exampleCount.toString()}
        />
        <MetadataCard
          label="Групп товаров"
          value={manifest.productGroupCount.toString()}
        />
        <MetadataCard
          label="Предупреждений"
          value={manifest.warnings.length.toString()}
        />
        <MetadataCard
          label="Экспортирован"
          value={formatUtcDate(metadata.exportedAtUtc)}
        />
      </div>

      <div className="grid gap-4 xl:grid-cols-3">
        {manifest.splits.map((split) => (
          <DatasetSplitCard
            key={split.split}
            split={split.split}
            fileName={split.fileName}
            exampleCount={split.exampleCount}
            productGroupCount={split.productGroupCount}
            sha256={split.sha256}
          />
        ))}
      </div>

      <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
        <p className="text-xs text-slate-500">Момент отсечения UTC</p>

        <p className="mt-2 font-mono text-sm text-slate-200">
          {manifest.finalizedUntilUtc}
        </p>
      </div>

      <div className="rounded-2xl border border-violet-500/25 bg-violet-500/[0.06] p-4">
        <p className="text-sm font-medium text-violet-100">
          Общий SHA-256 датасета
        </p>

        <p className="mt-2 break-all font-mono text-sm text-violet-200">
          {manifest.datasetSha256}
        </p>

        <p className="mt-2 text-xs leading-5 text-violet-100/70">
          Он вычислен из SHA-256 файлов Train, Validation и Test. Изменение
          любой части приведёт к другому общему отпечатку.
        </p>
      </div>
    </div>
  );
}

function DatasetSplitCard({
  split,
  fileName,
  exampleCount,
  productGroupCount,
  sha256,
}: {
  split: CatalogRecognitionDatasetSplit;
  fileName: string;
  exampleCount: number;
  productGroupCount: number;
  sha256: string;
}) {
  const presentation = getSplitPresentation(split);

  return (
    <div
      className={`rounded-2xl border p-5 ${presentation.containerClassName}`}
    >
      <div className="flex items-start justify-between gap-4">
        <div>
          <p className={`font-semibold ${presentation.titleClassName}`}>
            {presentation.label}
          </p>

          <p className="mt-1 font-mono text-xs text-slate-400">{fileName}</p>
        </div>

        <span className="rounded-full border border-white/10 bg-black/20 px-3 py-1 text-xs text-slate-300">
          {exampleCount} прим.
        </span>
      </div>

      <p className="mt-4 text-sm leading-6 text-slate-400">
        {presentation.description}
      </p>

      <div className="mt-4 grid grid-cols-2 gap-3">
        <MetadataCard label="Примеров" value={exampleCount.toString()} />
        <MetadataCard
          label="Групп товаров"
          value={productGroupCount.toString()}
        />
      </div>

      <div className="mt-3 rounded-xl border border-white/10 bg-black/20 p-3">
        <p className="text-xs text-slate-500">SHA-256 файла</p>

        <p className="mt-2 break-all font-mono text-xs text-slate-300">
          {sha256}
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

function getSplitPresentation(split: CatalogRecognitionDatasetSplit) {
  switch (split) {
    case "Train":
      return {
        label: "Train — обучение",
        description:
          "Основная часть данных, на которой будущая модель будет изменять свои параметры.",
        containerClassName: "border-blue-500/25 bg-blue-500/[0.05]",
        titleClassName: "text-blue-200",
      };

    case "Validation":
      return {
        label: "Validation — настройка",
        description:
          "Используется для выбора параметров и сравнения версий модели во время разработки.",
        containerClassName: "border-amber-500/25 bg-amber-500/[0.05]",
        titleClassName: "text-amber-200",
      };

    case "Test":
      return {
        label: "Test — независимая проверка",
        description:
          "Не участвует в обучении и используется только для итоговой объективной оценки.",
        containerClassName: "border-emerald-500/25 bg-emerald-500/[0.05]",
        titleClassName: "text-emerald-200",
      };
  }
}

function formatUtcDate(value: string): string {
  return `${new Intl.DateTimeFormat("ru-RU", {
    dateStyle: "medium",
    timeStyle: "medium",
    timeZone: "UTC",
  }).format(new Date(value))} UTC`;
}
