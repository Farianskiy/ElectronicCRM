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
import { PageWorkspace } from "@/shared/ui/PageWorkspace";
import { AppButton } from "@/shared/ui/AppButton";
import { catalogImportQueryKeys } from "@/features/catalogImports";

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
      queryClient.setQueryData(
        catalogImportQueryKeys.analysis(analysisResult.batchId),
        analysisResult,
      );

      await queryClient.invalidateQueries({
        queryKey: catalogImportQueryKeys.myRoot,
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
    <PageWorkspace
      eyebrow="Работа с каталогом"
      title="Загрузка Excel"
      description="Создайте новый пакет импорта и запустите автоматический анализ файла."
      contentClassName="grid min-w-0 gap-6"
      actions={
        <Link
          href="/catalog/imports"
          className="inline-flex min-h-11 w-full items-center justify-center gap-2 rounded-xl border border-[var(--app-button-secondary-border)] bg-[var(--app-button-secondary-bg)] px-4 py-2.5 text-sm font-semibold text-[var(--app-text)] transition-colors hover:border-[var(--app-border-strong)] hover:bg-[var(--app-surface-hover)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--app-accent)] motion-reduce:transition-none sm:w-auto"
        >
          <span aria-hidden="true">←</span>
          Назад к импортам
        </Link>
      }
    >
      <form onSubmit={handleSubmit} className="grid min-w-0 gap-6">
        <section className="min-w-0 rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5 shadow-sm shadow-[var(--app-shadow)] sm:p-6">
          <h2 className="text-xl font-semibold text-[var(--app-text)]">
            Исходный Excel-файл
          </h2>

          <p
            id="excel-file-requirements"
            className="mt-2 text-sm leading-6 text-[var(--app-muted)]"
          >
            Поддерживается формат .xlsx. Максимальный размер файла — 10 МБ.
          </p>

          <div
            role="button"
            aria-label="Выбрать Excel-файл"
            aria-describedby="excel-file-requirements"
            aria-disabled={isBusy}
            tabIndex={isBusy ? -1 : 0}
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
              "mt-6 min-w-0 rounded-3xl border-2 border-dashed",
              "p-6 text-center transition-colors sm:p-8",
              "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--app-accent)]",
              "motion-reduce:transition-none",
              isDragging
                ? "border-[var(--app-accent)] bg-[var(--app-accent-soft)]"
                : "border-[var(--app-border-strong)] bg-[var(--app-surface)]",
              isBusy
                ? "cursor-not-allowed opacity-60"
                : "cursor-pointer hover:border-[var(--app-accent)] hover:bg-[var(--app-accent-soft)]",
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

            <div
              aria-hidden="true"
              className="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl bg-[var(--app-accent-soft)] text-sm font-bold text-[var(--app-accent)]"
            >
              XLSX
            </div>

            <h3 className="mt-4 text-lg font-semibold text-[var(--app-text)]">
              {selectedFile
                ? "Excel-файл выбран"
                : "Перетащите Excel-файл сюда"}
            </h3>

            <p className="mt-2 text-sm leading-6 text-[var(--app-muted)]">
              {isBusy
                ? "Дождитесь завершения обработки файла."
                : selectedFile
                  ? "Нажмите на область, чтобы выбрать другой файл."
                  : "Или нажмите на область и выберите файл на компьютере."}
            </p>
          </div>

          {selectedFile && (
            <div className="mt-5 flex min-w-0 flex-col justify-between gap-4 rounded-2xl border border-[var(--app-accent-border)] bg-[var(--app-accent-soft)] p-4 sm:flex-row sm:items-center">
              <div className="min-w-0">
                <p className="font-medium text-[var(--app-text)] [overflow-wrap:anywhere]">
                  {selectedFile.name}
                </p>
                <p className="mt-1 text-sm text-[var(--app-muted)]">
                  {formatFileSize(selectedFile.size)}
                </p>
              </div>

              <AppButton
                type="button"
                variant="danger"
                disabled={isBusy}
                onClick={clearFile}
                className="w-full sm:w-auto"
              >
                Убрать файл
              </AppButton>
            </div>
          )}
        </section>

        <section className="min-w-0 rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5 shadow-sm shadow-[var(--app-shadow)] sm:p-6">
          <h2 className="text-xl font-semibold text-[var(--app-text)]">
            Что произойдёт после загрузки
          </h2>

          <div className="mt-5 grid min-w-0 gap-4 md:grid-cols-3">
            <StepCard
              number="1"
              title="Загрузка"
              description="Исходный файл будет сохранён как отдельный пакет импорта."
              active={phase === "uploading"}
            />

            <StepCard
              number="2"
              title="Анализ"
              description="Система прочитает колонки, строки и проверит значения."
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
          <section
            role="alert"
            className="min-w-0 rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm text-[var(--app-danger)] [overflow-wrap:anywhere]"
          >
            {validationError}
          </section>
        )}

        {uploadMutation.isError && (
          <section
            role="alert"
            className="min-w-0 rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-5"
          >
            <h2 className="font-semibold text-[var(--app-danger)]">
              Не удалось завершить импорт
            </h2>

            <p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-[var(--app-danger)] [overflow-wrap:anywhere]">
              {getApiErrorMessage(
                uploadMutation.error,
                "Не удалось загрузить или проанализировать Excel-файл.",
              )}
            </p>

            {createdBatchId && (
              <div className="mt-4">
                <p className="text-sm text-[var(--app-text)]">
                  Пакет уже был создан, но анализ завершился ошибкой.
                </p>

                <Link
                  href={`/catalog/imports/${createdBatchId}`}
                  className="mt-3 inline-flex min-h-11 w-full items-center justify-center rounded-xl border border-[var(--app-button-primary-border)] bg-[var(--app-button-primary-bg)] px-4 py-2.5 text-sm font-semibold text-[var(--app-button-primary-text)] transition-colors hover:border-[var(--app-button-primary-hover-border)] hover:bg-[var(--app-button-primary-hover-bg)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--app-accent)] motion-reduce:transition-none sm:w-auto"
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
            aria-disabled={isBusy}
            tabIndex={isBusy ? -1 : undefined}
            onClick={(event) => {
              if (isBusy) {
                event.preventDefault();
              }
            }}
            className={[
              "inline-flex min-h-11 items-center justify-center rounded-xl border px-4 py-2.5",
              "border-[var(--app-button-secondary-border)] bg-[var(--app-button-secondary-bg)]",
              "text-sm font-semibold text-[var(--app-text)] transition-colors",
              "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--app-accent)]",
              "motion-reduce:transition-none",
              isBusy
                ? "pointer-events-none opacity-50"
                : "hover:border-[var(--app-border-strong)] hover:bg-[var(--app-surface-hover)]",
            ].join(" ")}
          >
            Отмена
          </Link>

          <AppButton
            type="submit"
            variant="primary"
            disabled={!selectedFile || isBusy}
            loading={isBusy}
          >
            <span role="status">{getPhaseLabel(phase)}</span>
          </AppButton>
        </div>
      </form>
    </PageWorkspace>
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
      aria-current={active ? "step" : undefined}
      className={[
        "min-w-0 rounded-2xl border p-4 transition-colors motion-reduce:transition-none",
        active
          ? "border-[var(--app-accent-border)] bg-[var(--app-accent-soft)]"
          : "border-[var(--app-border)] bg-[var(--app-surface)]",
      ].join(" ")}
    >
      <div
        className={[
          "flex h-9 w-9 items-center justify-center rounded-xl text-sm font-bold",
          active
            ? "bg-[var(--app-button-primary-bg)] text-[var(--app-button-primary-text)]"
            : "bg-[var(--app-panel)] text-[var(--app-muted)]",
        ].join(" ")}
      >
        {number}
      </div>

      <h3 className="mt-4 font-semibold text-[var(--app-text)]">{title}</h3>
      <p className="mt-2 text-sm leading-6 text-[var(--app-muted)]">
        {description}
      </p>
    </div>
  );
}
