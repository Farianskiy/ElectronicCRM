"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import Link from "next/link";
import { useRouter } from "next/navigation";
import {
  useRef,
  useState,
  type ChangeEvent,
  type DragEvent,
  type FormEvent,
} from "react";
import { analyzeCatalogImportBatch } from "@/features/catalogImports/api/analyzeCatalogImportBatch";
import { createCatalogImportBatch } from "@/features/catalogImports/api/createCatalogImportBatch";
import type { AnalyzeCatalogImportBatchResponse } from "@/features/catalogImports/model/types";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { formatFileSize } from "@/shared/lib/formatters";
import { PageHeader } from "@/shared/ui/PageHeader";

const maximumFileSizeBytes = 10 * 1024 * 1024;

type UploadPhase = "idle" | "uploading" | "analyzing";

function validateExcelFile(file: File): string | null {
  if (file.size === 0) {
    return "Выбранный Excel-файл пуст.";
  }

  if (file.size > maximumFileSizeBytes) {
    return "Размер Excel-файла не должен превышать 10 МБ.";
  }

  if (!file.name.toLowerCase().endsWith(".xlsx")) {
    return "Поддерживаются только Excel-файлы с расширением .xlsx.";
  }

  return null;
}

function getPhaseLabel(phase: UploadPhase): string {
  switch (phase) {
    case "uploading":
      return "Загружаем Excel-файл...";
    case "analyzing":
      return "Анализируем структуру и строки...";
    default:
      return "Загрузить и проанализировать";
  }
}

export default function NewCatalogImportPage() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [validationError, setValidationError] = useState<string | null>(null);
  const [createdBatchId, setCreatedBatchId] = useState<string | null>(null);
  const [phase, setPhase] = useState<UploadPhase>("idle");
  const [isDragging, setIsDragging] = useState(false);

  const uploadMutation = useMutation({
    mutationFn: async (
      file: File,
    ): Promise<AnalyzeCatalogImportBatchResponse> => {
      setCreatedBatchId(null);
      setPhase("uploading");

      const createdBatch = await createCatalogImportBatch(file);

      setCreatedBatchId(createdBatch.batchId);
      setPhase("analyzing");

      return analyzeCatalogImportBatch(createdBatch.batchId);
    },

    onSuccess: async (analysisResult) => {
      await queryClient.invalidateQueries({
        queryKey: ["catalog-import-batches", "my"],
      });

      router.push(`/catalog/imports/${analysisResult.batchId}`);
    },

    onError: () => {
      setPhase("idle");
    },
  });

  const isBusy = uploadMutation.isPending;

  function selectFile(file: File | null): void {
    uploadMutation.reset();
    setCreatedBatchId(null);

    if (!file) {
      setSelectedFile(null);
      setValidationError(null);

      return;
    }

    const error = validateExcelFile(file);

    if (error) {
      setSelectedFile(null);
      setValidationError(error);

      if (fileInputRef.current) {
        fileInputRef.current.value = "";
      }

      return;
    }

    setSelectedFile(file);
    setValidationError(null);
  }

  function handleFileChange(event: ChangeEvent<HTMLInputElement>): void {
    selectFile(event.target.files?.[0] ?? null);
  }

  function handleDragOver(event: DragEvent<HTMLDivElement>): void {
    event.preventDefault();

    if (!isBusy) {
      setIsDragging(true);
    }
  }

  function handleDragLeave(event: DragEvent<HTMLDivElement>): void {
    event.preventDefault();
    setIsDragging(false);
  }

  function handleDrop(event: DragEvent<HTMLDivElement>): void {
    event.preventDefault();
    setIsDragging(false);

    if (isBusy) {
      return;
    }

    selectFile(event.dataTransfer.files?.[0] ?? null);
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    if (!selectedFile) {
      setValidationError("Сначала выберите Excel-файл.");

      return;
    }

    const error = validateExcelFile(selectedFile);

    if (error) {
      setValidationError(error);

      return;
    }

    setValidationError(null);
    uploadMutation.mutate(selectedFile);
  }

  function clearFile(): void {
    if (isBusy) {
      return;
    }

    setSelectedFile(null);
    setValidationError(null);
    uploadMutation.reset();
    setCreatedBatchId(null);

    if (fileInputRef.current) {
      fileInputRef.current.value = "";
    }
  }

  return (
    <div className="grid gap-6">
      <PageHeader
        title="Загрузка Excel"
        description="Создайте новый пакет импорта и запустите автоматический анализ файла."
      />

      <div>
        <Link
          href="/catalog/imports"
          className="inline-flex rounded-xl bg-white/[0.06] px-4 py-2 text-sm font-medium text-slate-200 transition hover:bg-white/[0.1]"
        >
          ← Назад к импортам
        </Link>
      </div>

      <form onSubmit={handleSubmit} className="grid gap-6">
        <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
          <div>
            <h2 className="text-xl font-semibold text-white">
              Исходный Excel-файл
            </h2>

            <p className="mt-2 text-sm leading-6 text-slate-400">
              Поддерживается формат .xlsx. Максимальный размер файла — 10 МБ.
            </p>
          </div>

          <div
            role="button"
            tabIndex={0}
            onClick={() => {
              if (!isBusy) {
                fileInputRef.current?.click();
              }
            }}
            onKeyDown={(event) => {
              if (!isBusy && (event.key === "Enter" || event.key === " ")) {
                event.preventDefault();
                fileInputRef.current?.click();
              }
            }}
            onDragOver={handleDragOver}
            onDragLeave={handleDragLeave}
            onDrop={handleDrop}
            className={[
              "mt-6 cursor-pointer rounded-3xl border-2 border-dashed",
              "p-8 text-center transition",
              isDragging
                ? "border-teal-400 bg-teal-500/10"
                : "border-white/15 bg-black/20 hover:border-teal-500/50 hover:bg-teal-500/[0.05]",
              isBusy ? "cursor-not-allowed opacity-60" : "",
            ].join(" ")}
          >
            <input
              ref={fileInputRef}
              type="file"
              accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
              disabled={isBusy}
              onChange={handleFileChange}
              className="hidden"
            />

            <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl bg-teal-500/10 text-2xl text-teal-300">
              XLSX
            </div>

            <h3 className="mt-4 text-lg font-semibold text-white">
              {selectedFile
                ? "Excel-файл выбран"
                : "Перетащите Excel-файл сюда"}
            </h3>

            <p className="mt-2 text-sm text-slate-400">
              {selectedFile
                ? "Нажмите на область, чтобы выбрать другой файл."
                : "Или нажмите на область и выберите файл на компьютере."}
            </p>
          </div>

          {selectedFile && (
            <div className="mt-5 flex flex-col justify-between gap-4 rounded-2xl border border-teal-500/20 bg-teal-500/[0.06] p-4 sm:flex-row sm:items-center">
              <div className="min-w-0">
                <p className="truncate font-medium text-white">
                  {selectedFile.name}
                </p>

                <p className="mt-1 text-sm text-slate-400">
                  {formatFileSize(selectedFile.size)}
                </p>
              </div>

              <button
                type="button"
                disabled={isBusy}
                onClick={clearFile}
                className="rounded-xl border border-white/10 bg-white/[0.05] px-4 py-2 text-sm text-slate-300 transition hover:bg-white/[0.1] disabled:cursor-not-allowed disabled:opacity-50"
              >
                Убрать файл
              </button>
            </div>
          )}
        </section>

        <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
          <h2 className="text-xl font-semibold text-white">
            Что произойдёт после загрузки
          </h2>

          <div className="mt-5 grid gap-4 md:grid-cols-3">
            <StepCard
              number="1"
              title="Загрузка"
              description="Исходный файл будет сохранён как отдельный пакет импорта."
              active={phase === "uploading"}
            />

            <StepCard
              number="2"
              title="Анализ"
              description="Backend прочитает колонки, строки и проверит значения."
              active={phase === "analyzing"}
            />

            <StepCard
              number="3"
              title="Результат"
              description="Откроется карточка пакета со статусом и статистикой."
              active={false}
            />
          </div>
        </section>

        {validationError && (
          <section className="rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-200">
            {validationError}
          </section>
        )}

        {uploadMutation.isError && (
          <section className="rounded-2xl border border-red-500/30 bg-red-500/10 p-5">
            <h2 className="font-semibold text-red-100">
              Не удалось завершить импорт
            </h2>

            <p className="mt-2 text-sm leading-6 text-red-200">
              {getApiErrorMessage(
                uploadMutation.error,
                "Не удалось загрузить или проанализировать Excel-файл.",
              )}
            </p>

            {createdBatchId && (
              <div className="mt-4">
                <p className="text-sm text-red-200/80">
                  Пакет уже был создан, но анализ завершился ошибкой.
                </p>

                <Link
                  href={`/catalog/imports/${createdBatchId}`}
                  className="mt-3 inline-flex rounded-xl border border-red-400/30 bg-red-500/10 px-4 py-2 text-sm font-medium text-red-100 transition hover:bg-red-500/20"
                >
                  Открыть созданный пакет
                </Link>
              </div>
            )}
          </section>
        )}

        <div className="flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
          <Link
            href="/catalog/imports"
            className={[
              "rounded-2xl border border-white/10",
              "bg-white/[0.05] px-5 py-3",
              "text-center text-sm font-medium text-slate-200",
              "transition hover:bg-white/[0.1]",
              isBusy ? "pointer-events-none opacity-50" : "",
            ].join(" ")}
          >
            Отмена
          </Link>

          <button
            type="submit"
            disabled={!selectedFile || isBusy}
            className="rounded-2xl bg-teal-500 px-6 py-3 text-sm font-semibold text-white transition hover:bg-teal-400 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {getPhaseLabel(phase)}
          </button>
        </div>
      </form>
    </div>
  );
}

function StepCard({
  number,
  title,
  description,
  active,
}: {
  number: string;
  title: string;
  description: string;
  active: boolean;
}) {
  return (
    <div
      className={[
        "rounded-2xl border p-4 transition",
        active
          ? "border-teal-400/50 bg-teal-500/10"
          : "border-white/10 bg-black/20",
      ].join(" ")}
    >
      <div
        className={[
          "flex h-9 w-9 items-center justify-center",
          "rounded-xl text-sm font-bold",
          active ? "bg-teal-500 text-white" : "bg-white/[0.06] text-slate-300",
        ].join(" ")}
      >
        {number}
      </div>

      <h3 className="mt-4 font-semibold text-white">{title}</h3>

      <p className="mt-2 text-sm leading-6 text-slate-400">{description}</p>
    </div>
  );
}
