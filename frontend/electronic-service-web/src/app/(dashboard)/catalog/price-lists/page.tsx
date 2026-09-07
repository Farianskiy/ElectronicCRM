"use client";

import Link from "next/link";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useRef, useState, type ChangeEvent, type FormEvent } from "react";
import { getCatalogManufacturers } from "@/features/catalogMetadata/api/getCatalogManufacturers";
import { getCatalogPriceListVersions } from "@/features/catalogPriceLists/api/getCatalogPriceListVersions";
import { uploadCatalogPriceList } from "@/features/catalogPriceLists/api/uploadCatalogPriceList";
import { catalogPriceListQueryKeys } from "@/features/catalogPriceLists/model/queryKeys";
import {
  catalogPriceListStatuses,
  type CatalogPriceListStatus,
  type CatalogPriceListVersion,
} from "@/features/catalogPriceLists/model/types";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { formatDate, formatFileSize } from "@/shared/lib/formatters";
import { AppButton } from "@/shared/ui/AppButton";
import { AppSelect } from "@/shared/ui/AppSelect";
import { PageWorkspace } from "@/shared/ui/PageWorkspace";

const pageSize = 20;
const maximumFileSizeBytes = 30 * 1024 * 1024;

function getPriceListStatusLabel(status: CatalogPriceListStatus): string {
  switch (status) {
    case "Uploaded":
      return "Загружен";
    case "Processing":
      return "Обрабатывается";
    case "NeedsCorrection":
      return "Требует исправления";
    case "Ready":
      return "Готов к активации";
    case "Active":
      return "Активный";
    case "Archived":
      return "Архивный";
    case "Failed":
      return "Ошибка обработки";
  }
}

function getStatusClassName(status: CatalogPriceListStatus): string {
  switch (status) {
    case "Active":
      return "border-[var(--app-success-border)] bg-[var(--app-success-soft)] text-[var(--app-success)]";
    case "Ready":
      return "border-[var(--app-accent-border)] bg-[var(--app-accent-soft)] text-[var(--app-accent)]";
    case "Processing":
    case "Uploaded":
      return "border-[var(--app-warning-border)] bg-[var(--app-warning-soft)] text-[var(--app-warning)]";
    case "NeedsCorrection":
    case "Failed":
      return "border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] text-[var(--app-danger)]";
    case "Archived":
      return "border-[var(--app-border)] bg-[var(--app-surface)] text-[var(--app-muted)]";
  }
}

function formatEffectiveDate(value?: string | null): string {
  if (!value) {
    return "Не определена";
  }

  return new Intl.DateTimeFormat("ru-RU", {
    dateStyle: "medium",
  }).format(new Date(`${value}T00:00:00`));
}

function PriceListStatusBadge({ status }: { status: CatalogPriceListStatus }) {
  return (
    <span
      className={`inline-flex rounded-full border px-3 py-1 text-xs font-semibold ${getStatusClassName(status)}`}
    >
      {getPriceListStatusLabel(status)}
    </span>
  );
}

function PriceListRow({ priceList }: { priceList: CatalogPriceListVersion }) {
  return (
    <tr className="bg-[var(--app-panel)] align-top transition-colors hover:bg-[var(--app-panel-hover)] motion-reduce:transition-none">
      <td className="px-4 py-4">
        <p className="font-medium text-[var(--app-text)] [overflow-wrap:anywhere]">
          {priceList.originalFileName}
        </p>

        <p className="mt-1 text-xs text-[var(--app-muted)]">
          {formatFileSize(priceList.fileSizeBytes)}
        </p>
      </td>

      <td className="px-4 py-4">
        <PriceListStatusBadge status={priceList.status} />
      </td>

      <td className="px-4 py-4 text-[var(--app-muted)]">
        {formatEffectiveDate(priceList.effectiveDate)}
      </td>

      <td className="px-4 py-4 tabular-nums text-[var(--app-text)]">
        <p>Всего: {priceList.rowsCount}</p>

        <p className="mt-1 text-xs text-[var(--app-success)]">
          Корректных: {priceList.validRowsCount}
        </p>

        <p className="mt-1 text-xs text-[var(--app-danger)]">
          С ошибками: {priceList.errorRowsCount}
        </p>
      </td>

      <td className="px-4 py-4 text-[var(--app-muted)]">
        {formatDate(
          priceList.activatedAtUtc ??
            priceList.processedAtUtc ??
            priceList.createdAtUtc,
        )}
      </td>

      <td className="px-4 py-4">
        {priceList.failureReason ? (
          <p className="line-clamp-3 text-sm text-[var(--app-danger)]">
            {priceList.failureReason}
          </p>
        ) : (
          <span className="text-[var(--app-muted)]">—</span>
        )}
      </td>

      <td className="px-4 py-4">
        <Link
          href={`/catalog/price-lists/${priceList.priceListId}`}
          className="inline-flex min-h-10 items-center justify-center rounded-xl border border-[var(--app-button-primary-border)] bg-[var(--app-button-primary-bg)] px-3 py-2 text-xs font-semibold text-[var(--app-button-primary-text)] transition-colors hover:border-[var(--app-button-primary-hover-border)] hover:bg-[var(--app-button-primary-hover-bg)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--app-accent)]"
        >
          Открыть
        </Link>
      </td>
    </tr>
  );
}

export default function CatalogPriceListsPage() {
  const queryClient = useQueryClient();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [manufacturerId, setManufacturerId] = useState("");
  const [status, setStatus] = useState<CatalogPriceListStatus | null>(null);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [validationError, setValidationError] = useState<string | null>(null);
  const [page, setPage] = useState(1);

  const manufacturersQuery = useQuery({
    queryKey: ["catalog-manufacturers"],
    queryFn: getCatalogManufacturers,
    staleTime: 5 * 60 * 1000,
  });

  const versionsQuery = useQuery({
    queryKey: catalogPriceListQueryKeys.versions(
      manufacturerId,
      status,
      page,
      pageSize,
    ),
    queryFn: () =>
      getCatalogPriceListVersions({
        manufacturerId,
        status,
        page,
        pageSize,
      }),
    enabled: manufacturerId.length > 0,
    placeholderData: (previousData) => previousData,
  });

  const uploadMutation = useMutation({
    mutationFn: uploadCatalogPriceList,
    onSuccess: async () => {
      setSelectedFile(null);
      setValidationError(null);
      setStatus(null);
      setPage(1);

      if (fileInputRef.current) {
        fileInputRef.current.value = "";
      }

      await queryClient.invalidateQueries({
        queryKey: catalogPriceListQueryKeys.versionsRoot,
      });
    },
  });

  const versions = versionsQuery.data?.items ?? [];
  const totalCount = versionsQuery.data?.totalCount ?? 0;
  const backendTotalPages = versionsQuery.data?.totalPages ?? 0;
  const totalPages = Math.max(1, backendTotalPages);

  function handleManufacturerChange(value: string): void {
    setManufacturerId(value);
    setStatus(null);
    setSelectedFile(null);
    setValidationError(null);
    setPage(1);
    uploadMutation.reset();

    if (fileInputRef.current) {
      fileInputRef.current.value = "";
    }
  }

  function handleStatusChange(value: string): void {
    setStatus(value.length === 0 ? null : (value as CatalogPriceListStatus));
    setPage(1);
  }

  function handleFileChange(event: ChangeEvent<HTMLInputElement>): void {
    const file = event.target.files?.[0] ?? null;

    uploadMutation.reset();

    if (!file) {
      setSelectedFile(null);
      setValidationError(null);
      return;
    }

    const lowerName = file.name.toLowerCase();

    if (!lowerName.endsWith(".zip") && !lowerName.endsWith(".xlsx")) {
      setSelectedFile(null);
      setValidationError("Поддерживаются только файлы ZIP и XLSX.");
      event.target.value = "";
      return;
    }

    if (file.size === 0) {
      setSelectedFile(null);
      setValidationError("Выбранный файл пуст.");
      event.target.value = "";
      return;
    }

    if (file.size > maximumFileSizeBytes) {
      setSelectedFile(null);
      setValidationError("Размер файла не должен превышать 30 МБ.");
      event.target.value = "";
      return;
    }

    setSelectedFile(file);
    setValidationError(null);
  }

  function handleUpload(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    if (!manufacturerId) {
      setValidationError("Выберите производителя.");
      return;
    }

    if (!selectedFile) {
      setValidationError("Выберите ZIP- или XLSX-файл.");
      return;
    }

    setValidationError(null);

    uploadMutation.mutate({
      manufacturerId,
      file: selectedFile,
    });
  }

  return (
    <PageWorkspace
      eyebrow="Настройка и качество"
      title="Прайс-листы"
      description="Загрузка, проверка и управление версиями прайс-листов производителей."
      contentClassName="grid min-w-0 gap-6"
    >
      <section
        aria-labelledby="price-list-manufacturer-title"
        className="rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5 shadow-sm shadow-[var(--app-shadow)] sm:p-6"
      >
        <h2
          id="price-list-manufacturer-title"
          className="text-xl font-semibold text-[var(--app-text)]"
        >
          Производитель
        </h2>

        <p className="mt-2 text-sm text-[var(--app-muted)]">
          Версии прайсов хранятся отдельно для каждого производителя.
        </p>

        <div className="mt-5 max-w-xl">
          <AppSelect
            ariaLabel="Производитель прайс-листа"
            value={manufacturerId}
            disabled={manufacturersQuery.isLoading}
            onChange={handleManufacturerChange}
            options={[
              {
                value: "",
                label: manufacturersQuery.isLoading
                  ? "Загружаем производителей..."
                  : "Выберите производителя",
                disabled: manufacturersQuery.isLoading,
              },
              ...(manufacturersQuery.data ?? []).map((manufacturer) => ({
                value: manufacturer.id,
                label: manufacturer.name,
              })),
            ]}
          />
        </div>

        {manufacturersQuery.isError && (
          <div
            role="alert"
            className="mt-4 rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm text-[var(--app-danger)]"
          >
            {getApiErrorMessage(
              manufacturersQuery.error,
              "Не удалось загрузить производителей.",
            )}
          </div>
        )}
      </section>

      <section
        aria-labelledby="price-list-upload-title"
        className="rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5 shadow-sm shadow-[var(--app-shadow)] sm:p-6"
      >
        <h2
          id="price-list-upload-title"
          className="text-xl font-semibold text-[var(--app-text)]"
        >
          Загрузить новую версию
        </h2>

        <p className="mt-2 text-sm leading-6 text-[var(--app-muted)]">
          Поддерживаются ZIP и XLSX размером до 30 МБ. Загрузка только сохраняет
          файл; обработка будет запускаться на странице версии.
        </p>

        <form onSubmit={handleUpload} className="mt-5 grid min-w-0 gap-4">
          <label className="grid min-w-0 gap-2">
            <span className="text-sm font-medium text-[var(--app-text)]">
              Файл прайс-листа
            </span>

            <input
              ref={fileInputRef}
              type="file"
              accept=".zip,.xlsx,application/zip,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
              disabled={!manufacturerId || uploadMutation.isPending}
              onChange={handleFileChange}
              className="min-h-11 w-full rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] px-4 py-2 text-sm text-[var(--app-text)] file:mr-4 file:rounded-lg file:border-0 file:bg-[var(--app-accent-soft)] file:px-3 file:py-2 file:font-semibold file:text-[var(--app-accent)] disabled:cursor-not-allowed disabled:opacity-60"
            />
          </label>

          {selectedFile && (
            <div className="rounded-2xl border border-[var(--app-accent-border)] bg-[var(--app-accent-soft)] p-4">
              <p className="font-medium text-[var(--app-text)]">
                {selectedFile.name}
              </p>

              <p className="mt-1 text-sm text-[var(--app-muted)]">
                {formatFileSize(selectedFile.size)}
              </p>
            </div>
          )}

          {validationError && (
            <div
              role="alert"
              className="rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm text-[var(--app-danger)]"
            >
              {validationError}
            </div>
          )}

          {uploadMutation.isError && (
            <div
              role="alert"
              className="rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm text-[var(--app-danger)]"
            >
              {getApiErrorMessage(
                uploadMutation.error,
                "Не удалось загрузить прайс-лист.",
              )}
            </div>
          )}

          {uploadMutation.isSuccess && (
            <div
              role="status"
              className="rounded-2xl border border-[var(--app-success-border)] bg-[var(--app-success-soft)] p-4 text-sm text-[var(--app-success)]"
            >
              Файл сохранён. Новая версия появилась в списке.
            </div>
          )}

          <div className="flex justify-end">
            <AppButton
              type="submit"
              variant="primary"
              loading={uploadMutation.isPending}
              disabled={!manufacturerId || !selectedFile}
            >
              Загрузить прайс
            </AppButton>
          </div>
        </form>
      </section>

      <section
        aria-labelledby="price-list-versions-title"
        className="rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5 shadow-sm shadow-[var(--app-shadow)] sm:p-6"
      >
        <div className="flex flex-col justify-between gap-4 md:flex-row md:items-end">
          <div>
            <h2
              id="price-list-versions-title"
              className="text-xl font-semibold text-[var(--app-text)]"
            >
              Версии прайса
            </h2>

            <p className="mt-1 text-sm text-[var(--app-muted)]">
              Найдено: {totalCount}. Страница {page} из {totalPages}.
            </p>
          </div>

          <div className="w-full md:w-72">
            <AppSelect
              ariaLabel="Статус версии прайс-листа"
              value={status ?? ""}
              disabled={!manufacturerId}
              onChange={handleStatusChange}
              options={[
                {
                  value: "",
                  label: "Все статусы",
                },
                ...catalogPriceListStatuses.map((priceListStatus) => ({
                  value: priceListStatus,
                  label: getPriceListStatusLabel(priceListStatus),
                })),
              ]}
            />
          </div>
        </div>

        {!manufacturerId ? (
          <div className="mt-6 rounded-2xl border border-dashed border-[var(--app-border-strong)] bg-[var(--app-surface)] p-6">
            <p className="text-sm text-[var(--app-muted)]">
              Выберите производителя, чтобы увидеть его прайс-листы.
            </p>
          </div>
        ) : versionsQuery.isLoading ? (
          <div
            role="status"
            className="mt-6 flex min-h-32 items-center justify-center rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] text-sm text-[var(--app-muted)]"
          >
            Загружаем версии прайса...
          </div>
        ) : versionsQuery.isError ? (
          <div
            role="alert"
            className="mt-6 rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm text-[var(--app-danger)]"
          >
            {getApiErrorMessage(
              versionsQuery.error,
              "Не удалось загрузить версии прайса.",
            )}
          </div>
        ) : versions.length > 0 ? (
          <div className="mt-6 overflow-x-auto rounded-2xl border border-[var(--app-border)]">
            <table className="w-full min-w-[1100px] table-fixed border-collapse text-left text-sm">
              <caption className="sr-only">
                Версии прайс-листа производителя
              </caption>

              <colgroup>
                <col className="w-[21%]" />
                <col className="w-[13%]" />
                <col className="w-[12%]" />
                <col className="w-[14%]" />
                <col className="w-[14%]" />
                <col className="w-[16%]" />
                <col className="w-[10%]" />
              </colgroup>

              <thead className="bg-[var(--app-surface)] text-[var(--app-muted)]">
                <tr>
                  <th className="px-4 py-3 font-medium">Файл</th>
                  <th className="px-4 py-3 font-medium">Статус</th>
                  <th className="px-4 py-3 font-medium">Дата прайса</th>
                  <th className="px-4 py-3 font-medium">Строки</th>
                  <th className="px-4 py-3 font-medium">Последнее действие</th>
                  <th className="px-4 py-3 font-medium">Ошибка</th>
                  <th className="px-4 py-3 font-medium">Действие</th>
                </tr>
              </thead>

              <tbody className="divide-y divide-[var(--app-border)]">
                {versions.map((priceList) => (
                  <PriceListRow
                    key={priceList.priceListId}
                    priceList={priceList}
                  />
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <div className="mt-6 rounded-2xl border border-dashed border-[var(--app-border-strong)] bg-[var(--app-surface)] p-6">
            <h3 className="font-semibold text-[var(--app-text)]">
              Версии не найдены
            </h3>

            <p className="mt-2 text-sm text-[var(--app-muted)]">
              Загрузите первый ZIP- или XLSX-файл этого производителя.
            </p>
          </div>
        )}

        {manufacturerId && (
          <nav
            aria-label="Страницы версий прайс-листа"
            className="mt-5 flex items-center justify-between border-t border-[var(--app-border)] pt-4"
          >
            <AppButton
              type="button"
              variant="secondary"
              disabled={page <= 1 || versionsQuery.isFetching}
              onClick={() => setPage((current) => Math.max(1, current - 1))}
            >
              Назад
            </AppButton>

            <AppButton
              type="button"
              variant="secondary"
              disabled={
                page >= totalPages ||
                backendTotalPages === 0 ||
                versionsQuery.isFetching
              }
              onClick={() =>
                setPage((current) => Math.min(totalPages, current + 1))
              }
            >
              Вперёд
            </AppButton>
          </nav>
        )}
      </section>
    </PageWorkspace>
  );
}
