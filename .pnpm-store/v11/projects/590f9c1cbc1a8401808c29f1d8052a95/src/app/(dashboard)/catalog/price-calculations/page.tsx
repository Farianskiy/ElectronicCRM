"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import { createCatalogPriceCalculation } from "@/features/catalogPriceCalculations/api/createCatalogPriceCalculation";
import { getMyCatalogPriceCalculations } from "@/features/catalogPriceCalculations/api/getMyCatalogPriceCalculations";
import { catalogPriceCalculationQueryKeys } from "@/features/catalogPriceCalculations/model/queryKeys";
import {
  catalogPriceCalculationStatuses,
  type CatalogPriceCalculationListItem,
  type CatalogPriceCalculationStatus,
} from "@/features/catalogPriceCalculations/model/types";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { formatDate, formatPrice } from "@/shared/lib/formatters";
import { AppButton } from "@/shared/ui/AppButton";
import { AppInput } from "@/shared/ui/AppInput";
import { AppSelect } from "@/shared/ui/AppSelect";
import { PageWorkspace } from "@/shared/ui/PageWorkspace";

const pageSize = 20;

function getStatusLabel(status: CatalogPriceCalculationStatus): string {
  switch (status) {
    case "Draft":
      return "Черновик";
    case "Completed":
      return "Завершён";
    case "Archived":
      return "В архиве";
  }
}

function CalculationStatusBadge({
  status,
}: {
  status: CatalogPriceCalculationStatus;
}) {
  const className =
    status === "Draft"
      ? "border-[var(--app-warning-border)] bg-[var(--app-warning-soft)] text-[var(--app-warning)]"
      : status === "Completed"
        ? "border-[var(--app-success-border)] bg-[var(--app-success-soft)] text-[var(--app-success)]"
        : "border-[var(--app-border)] bg-[var(--app-surface)] text-[var(--app-muted)]";

  return (
    <span
      className={`inline-flex rounded-full border px-3 py-1 text-xs font-semibold ${className}`}
    >
      {getStatusLabel(status)}
    </span>
  );
}

function CalculationRow({
  calculation,
}: {
  calculation: CatalogPriceCalculationListItem;
}) {
  return (
    <tr className="bg-[var(--app-panel)] align-top transition-colors hover:bg-[var(--app-panel-hover)] motion-reduce:transition-none">
      <td className="px-4 py-4">
        <p className="font-medium text-[var(--app-text)] [overflow-wrap:anywhere]">
          {calculation.title}
        </p>

        <p className="mt-1 text-xs text-[var(--app-muted)]">
          Создан: {formatDate(calculation.createdAtUtc)}
        </p>
      </td>

      <td className="px-4 py-4">
        <CalculationStatusBadge status={calculation.status} />
      </td>

      <td className="px-4 py-4 tabular-nums text-[var(--app-text)]">
        {calculation.linesCount}
      </td>

      <td className="px-4 py-4 tabular-nums text-[var(--app-text)]">
        {calculation.manufacturersCount}
      </td>

      <td className="whitespace-nowrap px-4 py-4 font-semibold tabular-nums text-[var(--app-text)]">
        {formatPrice(calculation.totalAmount, calculation.currency)}
      </td>

      <td className="px-4 py-4 text-[var(--app-muted)]">
        {formatDate(calculation.updatedAtUtc ?? calculation.createdAtUtc)}
      </td>

      <td className="px-4 py-4">
        <Link
          href={`/catalog/price-calculations/${calculation.calculationId}`}
          className="inline-flex min-h-10 items-center justify-center rounded-xl border border-[var(--app-button-primary-border)] bg-[var(--app-button-primary-bg)] px-3 py-2 text-xs font-semibold text-[var(--app-button-primary-text)] transition-colors hover:border-[var(--app-button-primary-hover-border)] hover:bg-[var(--app-button-primary-hover-bg)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--app-accent)] motion-reduce:transition-none"
        >
          {calculation.status === "Draft" ? "Продолжить" : "Открыть"}
        </Link>
      </td>
    </tr>
  );
}

export default function CatalogPriceCalculationsPage() {
  const router = useRouter();
  const queryClient = useQueryClient();

  const [title, setTitle] = useState("");
  const [status, setStatus] = useState<CatalogPriceCalculationStatus | null>(
    null,
  );
  const [page, setPage] = useState(1);

  const calculationsQuery = useQuery({
    queryKey: catalogPriceCalculationQueryKeys.list(status, page, pageSize),
    queryFn: () =>
      getMyCatalogPriceCalculations({
        status,
        page,
        pageSize,
      }),
    placeholderData: (previousData) => previousData,
  });

  const createMutation = useMutation({
    mutationFn: createCatalogPriceCalculation,
    onSuccess: async (createdCalculation) => {
      setTitle("");

      await queryClient.invalidateQueries({
        queryKey: catalogPriceCalculationQueryKeys.listRoot,
      });

      router.push(
        `/catalog/price-calculations/${createdCalculation.calculationId}`,
      );
    },
  });

  const calculations = calculationsQuery.data?.items ?? [];
  const totalCount = calculationsQuery.data?.totalCount ?? 0;
  const backendTotalPages = calculationsQuery.data?.totalPages ?? 0;
  const totalPages = Math.max(1, backendTotalPages);

  function handleCreate(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    const normalizedTitle = title.trim();

    if (normalizedTitle.length === 0) {
      return;
    }

    createMutation.mutate({
      title: normalizedTitle,
    });
  }

  function handleStatusChange(value: string): void {
    setStatus(
      value.length === 0 ? null : (value as CatalogPriceCalculationStatus),
    );
    setPage(1);
  }

  return (
    <PageWorkspace
      eyebrow="Работа с каталогом"
      title="Расчёт цен"
      description="Создавайте расчёты по активным прайс-листам и применяйте проектные скидки производителей."
      contentClassName="grid min-w-0 gap-6"
    >
      <section
        aria-labelledby="new-calculation-title"
        className="min-w-0 rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5 shadow-sm shadow-[var(--app-shadow)] sm:p-6"
      >
        <h2
          id="new-calculation-title"
          className="text-xl font-semibold text-[var(--app-text)]"
        >
          Новый расчёт
        </h2>

        <p className="mt-2 text-sm leading-6 text-[var(--app-muted)]">
          Создайте пустой черновик. Товары и скидки будут добавлены на следующем
          экране.
        </p>

        <form
          onSubmit={handleCreate}
          className="mt-5 grid min-w-0 gap-4 md:grid-cols-[minmax(0,1fr)_auto] md:items-end"
        >
          <label className="grid min-w-0 gap-2">
            <span className="text-sm font-medium text-[var(--app-text)]">
              Название расчёта
            </span>

            <AppInput
              value={title}
              maxLength={200}
              disabled={createMutation.isPending}
              onChange={(event) => setTitle(event.target.value)}
              placeholder="Например: Щитовая для объекта на Ленина"
            />
          </label>

          <AppButton
            type="submit"
            variant="primary"
            loading={createMutation.isPending}
            disabled={title.trim().length === 0}
          >
            Создать черновик
          </AppButton>
        </form>

        {createMutation.isError && (
          <div
            role="alert"
            className="mt-4 rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm text-[var(--app-danger)]"
          >
            {getApiErrorMessage(
              createMutation.error,
              "Не удалось создать расчёт цен.",
            )}
          </div>
        )}

        {createMutation.isSuccess && (
          <div
            role="status"
            className="mt-4 rounded-2xl border border-[var(--app-success-border)] bg-[var(--app-success-soft)] p-4 text-sm text-[var(--app-success)]"
          >
            Черновик создан и добавлен в список.
          </div>
        )}
      </section>

      <section
        aria-label="Фильтры расчётов"
        className="min-w-0 rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5 shadow-sm shadow-[var(--app-shadow)] sm:p-6"
      >
        <div className="grid min-w-0 gap-4 md:grid-cols-[minmax(0,320px)_1fr] md:items-end">
          <label className="grid min-w-0 gap-2">
            <span className="text-sm font-medium text-[var(--app-text)]">
              Статус расчёта
            </span>

            <AppSelect
              ariaLabel="Статус расчёта"
              value={status ?? ""}
              onChange={handleStatusChange}
              options={[
                {
                  value: "",
                  label: "Все статусы",
                },
                ...catalogPriceCalculationStatuses.map((calculationStatus) => ({
                  value: calculationStatus,
                  label: getStatusLabel(calculationStatus),
                })),
              ]}
            />
          </label>

          <p className="text-sm text-[var(--app-muted)] md:text-right">
            Найдено расчётов:{" "}
            <span className="font-semibold tabular-nums text-[var(--app-text)]">
              {totalCount}
            </span>
          </p>
        </div>
      </section>

      {calculationsQuery.isError && (
        <section
          role="alert"
          className="min-w-0 rounded-3xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-5 text-[var(--app-danger)]"
        >
          <h2 className="font-semibold">Не удалось загрузить расчёты</h2>

          <p className="mt-2 whitespace-pre-wrap text-sm [overflow-wrap:anywhere]">
            {getApiErrorMessage(
              calculationsQuery.error,
              "Не удалось получить список расчётов цен.",
            )}
          </p>
        </section>
      )}

      <section
        aria-labelledby="calculations-list-title"
        className="min-w-0 rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5 shadow-sm shadow-[var(--app-shadow)] sm:p-6"
      >
        <div className="flex flex-col justify-between gap-3 sm:flex-row sm:items-center">
          <div>
            <h2
              id="calculations-list-title"
              className="text-xl font-semibold text-[var(--app-text)]"
            >
              Мои расчёты
            </h2>

            <p className="mt-1 text-sm text-[var(--app-muted)]">
              Страница {page} из {totalPages}
            </p>
          </div>

          {calculationsQuery.isFetching && !calculationsQuery.isLoading && (
            <p role="status" className="text-sm text-[var(--app-accent)]">
              Обновляем список...
            </p>
          )}
        </div>

        {calculationsQuery.isLoading ? (
          <div
            role="status"
            className="mt-6 flex min-h-32 items-center justify-center gap-3 rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-5 text-sm text-[var(--app-muted)]"
          >
            <span
              aria-hidden="true"
              className="h-5 w-5 animate-spin rounded-full border-2 border-[var(--app-accent-border)] border-t-[var(--app-accent)] motion-reduce:animate-none"
            />
            Загружаем расчёты...
          </div>
        ) : calculations.length > 0 ? (
          <div
            role="region"
            aria-label="Таблица расчётов цен"
            tabIndex={0}
            className="mt-6 min-w-0 max-w-full overflow-x-auto rounded-2xl border border-[var(--app-border)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--app-accent)]"
          >
            <table className="w-full min-w-[1100px] table-fixed border-collapse text-left text-sm">
              <caption className="sr-only">Сохранённые расчёты цен</caption>

              <colgroup>
                <col className="w-[26%]" />
                <col className="w-[13%]" />
                <col className="w-[9%]" />
                <col className="w-[12%]" />
                <col className="w-[15%]" />
                <col className="w-[15%]" />
                <col className="w-[10%]" />
              </colgroup>

              <thead className="bg-[var(--app-surface)] text-[var(--app-muted)]">
                <tr>
                  <th scope="col" className="px-4 py-3 font-medium">
                    Расчёт
                  </th>
                  <th scope="col" className="px-4 py-3 font-medium">
                    Статус
                  </th>
                  <th scope="col" className="px-4 py-3 font-medium">
                    Позиций
                  </th>
                  <th scope="col" className="px-4 py-3 font-medium">
                    Производителей
                  </th>
                  <th scope="col" className="px-4 py-3 font-medium">
                    Сумма
                  </th>
                  <th scope="col" className="px-4 py-3 font-medium">
                    Изменён
                  </th>
                  <th scope="col" className="px-4 py-3 font-medium">
                    Действие
                  </th>
                </tr>
              </thead>

              <tbody className="divide-y divide-[var(--app-border)]">
                {calculations.map((calculation) => (
                  <CalculationRow
                    key={calculation.calculationId}
                    calculation={calculation}
                  />
                ))}
              </tbody>
            </table>
          </div>
        ) : !calculationsQuery.isError ? (
          <div
            role="status"
            className="mt-6 rounded-2xl border border-dashed border-[var(--app-border-strong)] bg-[var(--app-surface)] p-6"
          >
            <h3 className="text-lg font-semibold text-[var(--app-text)]">
              {status ? "Расчёты не найдены" : "Расчётов пока нет"}
            </h3>

            <p className="mt-2 text-sm text-[var(--app-muted)]">
              {status
                ? "Нет расчётов с выбранным статусом."
                : "Введите название выше и создайте первый черновик."}
            </p>
          </div>
        ) : null}

        <nav
          aria-label="Страницы расчётов цен"
          className="mt-5 grid grid-cols-2 gap-3 sm:flex sm:items-center sm:justify-between"
        >
          <AppButton
            type="button"
            variant="secondary"
            disabled={page <= 1 || calculationsQuery.isFetching}
            onClick={() =>
              setPage((currentPage) => Math.max(1, currentPage - 1))
            }
          >
            Назад
          </AppButton>

          <AppButton
            type="button"
            variant="secondary"
            disabled={
              page >= totalPages ||
              backendTotalPages === 0 ||
              calculationsQuery.isFetching
            }
            onClick={() =>
              setPage((currentPage) => Math.min(totalPages, currentPage + 1))
            }
          >
            Вперёд
          </AppButton>
        </nav>
      </section>
    </PageWorkspace>
  );
}
