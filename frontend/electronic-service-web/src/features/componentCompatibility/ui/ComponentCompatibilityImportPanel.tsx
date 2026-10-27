"use client";

import { useMemo, useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { getCatalogProductTypeCharacteristicSchema } from "@/features/catalogProductTypes/api/getCatalogProductTypeCharacteristicSchema";
import {
  applyComponentCompatibilityImport,
  downloadComponentCompatibilityTemplate,
  previewComponentCompatibilityImport,
} from "../api/componentCompatibilityImport";
import type { ComponentCompatibilityImportRowStatus } from "../model/types";
import { useCurrentUserAccess } from "@/features/auth/model/CurrentUserAccessContext";
import { CreateCatalogProductDialog } from "@/features/catalogProducts/ui/CreateCatalogProductDialog";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppButton } from "@/shared/ui/AppButton";

function getStatusLabel(status: ComponentCompatibilityImportRowStatus): string {
  switch (status) {
    case "Ready":
      return "Будет применено";
    case "WillCreate":
      return "Будет создано";
    case "WillUpdate":
      return "Будет изменено";
    case "Unchanged":
      return "Без изменений";
    case "RequiresConfirmation":
      return "Нужно подтверждение";
    case "AlreadyExists":
      return "Уже существует";
    case "ComponentNotFound":
      return "Комплектующее не найдено";
    case "MainProductTypeNotFound":
      return "Тип товара не найден";
    case "NeedNotFound":
      return "Потребность не найдена";
    case "CharacteristicNotFound":
      return "Характеристика не найдена";
    case "Duplicate":
      return "Повтор в файле";
    case "Invalid":
      return "Некорректная строка";
  }
}

function getStatusClassName(status: ComponentCompatibilityImportRowStatus): string {
  if (status === "Ready" || status === "WillCreate" || status === "WillUpdate") {
    return "border-[var(--app-success-border)] bg-[var(--app-success-soft)] text-[var(--app-success)]";
  }

  if (status === "AlreadyExists" || status === "Unchanged") {
    return "border-[var(--app-border)] bg-[var(--app-surface)] text-[var(--app-muted)]";
  }

  if (status === "RequiresConfirmation") {
    return "border-amber-500/40 bg-amber-500/10 text-amber-300";
  }

  return "border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] text-[var(--app-danger)]";
}

export function ComponentCompatibilityImportPanel({
  productTypeCode,
}: {
  productTypeCode?: string;
}) {
  const queryClient = useQueryClient();
  const { hasPermission } = useCurrentUserAccess();
  const canEditProducts = hasPermission("ProductsEdit");
  const [file, setFile] = useState<File | null>(null);
  const [allowRulesWithoutConditions, setAllowRulesWithoutConditions] =
    useState(false);
  const [showColumnSelection, setShowColumnSelection] = useState(false);
  const [columnSearch, setColumnSearch] = useState("");
  const [columnSelection, setColumnSelection] = useState<{
    productTypeCode?: string;
    codes: string[];
    rangeCodes: string[];
  } | null>(null);
  const [missingComponentArticle, setMissingComponentArticle] = useState<
    string | null
  >(null);
  const previewMutation = useMutation({
    mutationFn: previewComponentCompatibilityImport,
  });
  const schemaQuery = useQuery({
    queryKey: ["catalog-product-type-characteristic-schema", productTypeCode],
    queryFn: () =>
      getCatalogProductTypeCharacteristicSchema(productTypeCode as string),
    enabled: Boolean(productTypeCode),
  });
  const applyMutation = useMutation({
    mutationFn: applyComponentCompatibilityImport,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["component-offers"] });
    },
  });
  const templateMutation = useMutation({
    mutationFn: () =>
      downloadComponentCompatibilityTemplate(
        productTypeCode,
        productTypeCode ? selectedCharacteristicCodes : undefined,
        productTypeCode ? selectedRangeCharacteristicCodes : undefined,
      ),
  });
  const characteristics = useMemo(
    () => schemaQuery.data?.characteristics ?? [],
    [schemaQuery.data?.characteristics],
  );
  const selectedCharacteristicCodes =
    columnSelection !== null &&
    columnSelection.productTypeCode === productTypeCode
      ? columnSelection.codes
      : characteristics.map((characteristic) => characteristic.code);
  const selectedRangeCharacteristicCodes =
    columnSelection !== null &&
    columnSelection.productTypeCode === productTypeCode
      ? columnSelection.rangeCodes
      : [];
  const filteredCharacteristics = useMemo(() => {
    const search = columnSearch.trim().toLocaleLowerCase("ru");
    return search.length === 0
      ? characteristics
      : characteristics.filter(
          (item) =>
            item.name.toLocaleLowerCase("ru").includes(search) ||
            item.code.toLocaleLowerCase("ru").includes(search),
        );
  }, [characteristics, columnSearch]);

  const readyRows = useMemo(
    () =>
      (previewMutation.data?.rows ?? []).filter(
        (row) =>
          (row.status === "WillCreate" || row.status === "WillUpdate") &&
          row.componentProductId !== null &&
          row.mainProductTypeId !== null,
      ),
    [previewMutation.data?.rows],
  );
  const broadRows = useMemo(
    () =>
      (previewMutation.data?.rows ?? []).filter(
        (row) =>
          row.status === "RequiresConfirmation" &&
          row.componentProductId !== null &&
          row.mainProductTypeId !== null,
      ),
    [previewMutation.data?.rows],
  );
  const previewSummary = useMemo(() => {
    const rows = previewMutation.data?.rows ?? [];
    return {
      willCreate: rows.filter((row) => row.status === "WillCreate").length,
      willUpdate: rows.filter((row) => row.status === "WillUpdate").length,
      unchanged: rows.filter((row) => row.status === "Unchanged").length,
      errors: rows.filter(
        (row) =>
          row.status !== "WillCreate" &&
          row.status !== "WillUpdate" &&
          row.status !== "Unchanged" &&
          row.status !== "RequiresConfirmation",
      ).length,
      requiresConfirmation: rows.filter(
        (row) => row.status === "RequiresConfirmation",
      ).length,
    };
  }, [previewMutation.data?.rows]);

  function handlePreview(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    if (!file) {
      return;
    }

    applyMutation.reset();
    setAllowRulesWithoutConditions(false);
    previewMutation.mutate(file);
  }

  function handleApply(): void {
    const rowsToApply = allowRulesWithoutConditions
      ? [...readyRows, ...broadRows]
      : readyRows;

    if (rowsToApply.length === 0) {
      return;
    }

    applyMutation.mutate({
      rows: rowsToApply.map((row) => ({
        componentProductId: row.componentProductId as string,
        mainProductTypeId: row.mainProductTypeId as string,
        needDefinitionId: row.needDefinitionId,
        isActive: row.isActive,
        constraints: row.constraints,
      })),
      allowRulesWithoutConditions,
    });
  }

  async function handleComponentCreated(): Promise<void> {
    setMissingComponentArticle(null);
    await queryClient.invalidateQueries({ queryKey: ["catalog-products"] });

    if (!file) {
      return;
    }

    applyMutation.reset();
    previewMutation.mutate(file);
  }

  return (
    <section className="min-w-0 rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5 shadow-sm shadow-[var(--app-shadow)] sm:p-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <h2 className="text-lg font-semibold text-[var(--app-text)]">
          Правила совместимости комплектующих из Excel
        </h2>
        <AppButton
          type="button"
          size="sm"
          variant="secondary"
          onClick={() => setShowColumnSelection((current) => !current)}
        >
          {showColumnSelection ? "Закрыть настройку" : "Создать новый Excel"}
        </AppButton>
      </div>
      <p className="mt-2 text-sm leading-6 text-[var(--app-muted)]">
        Одна или несколько строк с одинаковыми артикулом, типом и потребностью
        образуют одно правило. Пустая потребность создаёт обычный совместимый
        аксессуар. Каждая повторная строка — альтернативный вариант.
        В одной строке разные характеристики работают одновременно. {" "}
        Обязательные колонки: «Артикул комплектующего» (можно «Артикул»),
        «Тип основного товара» и «Потребность» (значение можно не заполнять).
        Остальные заполненные колонки
        считаются характеристиками совместимости; варианты разделяются точкой с
        запятой.
      </p>

      {showColumnSelection && (
        <div className="mt-5 grid gap-4 rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4">
          <div>
            <h3 className="font-semibold text-[var(--app-text)]">
              Колонки условий
            </h3>
            <p className="mt-1 text-sm text-[var(--app-muted)]">
              Артикул, тип товара, необязательная потребность и состояние добавляются всегда.
              Отметьте только характеристики, которые нужны в новом файле.
            </p>
          </div>

          <input
            value={columnSearch}
            onChange={(event) => setColumnSearch(event.target.value)}
            placeholder="Найти характеристику по названию или коду"
            className="min-h-11 rounded-xl border border-[var(--app-border)] bg-[var(--app-panel)] px-3 text-sm text-[var(--app-text)]"
          />

          <div className="flex flex-wrap gap-2">
            <AppButton
              type="button"
              size="sm"
              variant="ghost"
              onClick={() =>
                setColumnSelection({
                  productTypeCode,
                  codes: characteristics.map((item) => item.code),
                  rangeCodes: [],
                })
              }
            >
              Выбрать все
            </AppButton>
            <AppButton
              type="button"
              size="sm"
              variant="ghost"
              onClick={() =>
                setColumnSelection({
                  productTypeCode,
                  codes: [],
                  rangeCodes: [],
                })
              }
            >
              Очистить
            </AppButton>
          </div>

          {schemaQuery.isLoading ? (
            <p className="text-sm text-[var(--app-muted)]">
              Загружаем характеристики…
            </p>
          ) : (
            <div className="grid max-h-72 gap-2 overflow-y-auto sm:grid-cols-2 xl:grid-cols-3">
              {filteredCharacteristics.map((characteristic) => (
                <div
                  key={characteristic.definitionId}
                  className="grid gap-3 rounded-xl border border-[var(--app-border)] bg-[var(--app-panel)] p-3"
                >
                  <label className="flex cursor-pointer items-start gap-3">
                    <input
                      type="checkbox"
                      checked={selectedCharacteristicCodes.includes(
                        characteristic.code,
                      )}
                      onChange={(event) =>
                        setColumnSelection({
                          productTypeCode,
                          codes: event.target.checked
                            ? [
                                ...selectedCharacteristicCodes,
                                characteristic.code,
                              ]
                            : selectedCharacteristicCodes.filter(
                                (code) => code !== characteristic.code,
                              ),
                          rangeCodes: event.target.checked
                            ? selectedRangeCharacteristicCodes
                            : selectedRangeCharacteristicCodes.filter(
                                (code) => code !== characteristic.code,
                              ),
                        })
                      }
                      className="mt-1"
                    />
                    <span className="min-w-0">
                      <span className="block text-sm font-medium text-[var(--app-text)]">
                        {characteristic.name}
                      </span>
                      <span className="block text-xs text-[var(--app-muted)]">
                        {characteristic.code} · {characteristic.dataType}
                      </span>
                    </span>
                  </label>
                  {characteristic.dataType === "Number" &&
                    selectedCharacteristicCodes.includes(characteristic.code) && (
                      <label className="ml-7 flex cursor-pointer items-center gap-2 text-xs text-[var(--app-muted)]">
                        <input
                          type="checkbox"
                          checked={selectedRangeCharacteristicCodes.includes(
                            characteristic.code,
                          )}
                          onChange={(event) =>
                            setColumnSelection({
                              productTypeCode,
                              codes: selectedCharacteristicCodes,
                              rangeCodes: event.target.checked
                                ? [
                                    ...selectedRangeCharacteristicCodes,
                                    characteristic.code,
                                  ]
                                : selectedRangeCharacteristicCodes.filter(
                                    (code) => code !== characteristic.code,
                                  ),
                            })
                          }
                        />
                        Добавить колонки «от/до»
                      </label>
                    )}
                </div>
              ))}
            </div>
          )}

          <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
            <p className="text-sm text-[var(--app-muted)]">
              Выбрано характеристик: {selectedCharacteristicCodes.length}.
              Диапазонов: {selectedRangeCharacteristicCodes.length}
            </p>
            <AppButton
              type="button"
              variant="primary"
              loading={templateMutation.isPending}
              disabled={!productTypeCode || schemaQuery.isLoading}
              onClick={() => templateMutation.mutate()}
            >
              Скачать новый Excel
            </AppButton>
          </div>

          {templateMutation.isError && (
            <p className="text-sm text-[var(--app-danger)]">
              {getApiErrorMessage(
                templateMutation.error,
                "Не удалось создать Excel-шаблон.",
              )}
            </p>
          )}
        </div>
      )}

      <form
        onSubmit={handlePreview}
        className="mt-5 grid min-w-0 gap-4 lg:grid-cols-[minmax(280px,1fr)_auto] lg:items-end"
      >
        <label className="grid min-w-0 gap-2">
          <span className="text-sm font-medium text-[var(--app-text)]">
            XLSX-файл правил
          </span>
          <input
            type="file"
            accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            onChange={(event) => {
              setFile(event.target.files?.[0] ?? null);
              previewMutation.reset();
              applyMutation.reset();
              setAllowRulesWithoutConditions(false);
            }}
            className="min-h-11 w-full rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] px-3 py-2 text-sm text-[var(--app-text)] file:mr-3 file:rounded-lg file:border-0 file:bg-[var(--app-accent-soft)] file:px-3 file:py-1.5 file:font-semibold file:text-[var(--app-accent)]"
          />
        </label>
        <AppButton
          type="submit"
          variant="primary"
          loading={previewMutation.isPending}
          disabled={!file}
        >
          Проверить правила
        </AppButton>
      </form>

      {previewMutation.isError && (
        <div
          role="alert"
          className="mt-4 rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm text-[var(--app-danger)]"
        >
          {getApiErrorMessage(
            previewMutation.error,
            "Не удалось проверить файл правил.",
          )}
        </div>
      )}

      {previewMutation.data && (
        <div className="mt-5 grid gap-4">
          <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-5">
            <div className="rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4">
              <p className="text-sm text-[var(--app-muted)]">Прочитано</p>
              <p className="mt-1 text-2xl font-bold text-[var(--app-text)]">
                {previewMutation.data.readRowsCount}
              </p>
            </div>
            <div className="rounded-2xl border border-[var(--app-success-border)] bg-[var(--app-success-soft)] p-4">
              <p className="text-sm text-[var(--app-success)]">
                Будет создано
              </p>
              <p className="mt-1 text-2xl font-bold text-[var(--app-success)]">
                {previewSummary.willCreate}
              </p>
            </div>
            <div className="rounded-2xl border border-[var(--app-success-border)] bg-[var(--app-success-soft)] p-4">
              <p className="text-sm text-[var(--app-success)]">Будет изменено</p>
              <p className="mt-1 text-2xl font-bold text-[var(--app-success)]">
                {previewSummary.willUpdate}
              </p>
            </div>
            <div className="rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4">
              <p className="text-sm text-[var(--app-muted)]">Без изменений</p>
              <p className="mt-1 text-2xl font-bold text-[var(--app-text)]">
                {previewSummary.unchanged}
              </p>
            </div>
            <div className="rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4">
              <p className="text-sm text-[var(--app-danger)]">Ошибок</p>
              <p className="mt-1 text-2xl font-bold text-[var(--app-danger)]">
                {previewSummary.errors}
              </p>
            </div>
          </div>

          <div className="flex flex-col gap-3 rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4 sm:flex-row sm:items-center sm:justify-between">
            <p className="text-sm text-[var(--app-muted)]">
              Сначала проверьте строки ниже. Повторные строки одного правила
              станут альтернативными вариантами. Ошибочные строки применяться
              не будут.
            </p>
            <AppButton
              type="button"
              variant="primary"
              loading={applyMutation.isPending}
              disabled={
                (readyRows.length === 0 &&
                  (!allowRulesWithoutConditions || broadRows.length === 0)) ||
                applyMutation.isSuccess
              }
              onClick={handleApply}
            >
              Создать или обновить правила
            </AppButton>
          </div>

          {broadRows.length > 0 && (
            <label className="flex items-start gap-3 rounded-2xl border border-amber-500/40 bg-amber-500/10 p-4 text-sm text-[var(--app-text)]">
              <input
                type="checkbox"
                checked={allowRulesWithoutConditions}
                onChange={(event) =>
                  setAllowRulesWithoutConditions(event.target.checked)
                }
                className="mt-1"
              />
              <span>
                Подтверждаю создание {previewSummary.requiresConfirmation} правил
                без условий. Они будут показывать комплектующее для всех товаров
                выбранного типа.
              </span>
            </label>
          )}

          {applyMutation.isError && (
            <div
              role="alert"
              className="rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm text-[var(--app-danger)]"
            >
              {getApiErrorMessage(
                applyMutation.error,
                "Не удалось создать правила совместимости.",
              )}
            </div>
          )}

          {applyMutation.data && (
            <div
              role="status"
              className="rounded-2xl border border-[var(--app-success-border)] bg-[var(--app-success-soft)] p-4 text-sm text-[var(--app-success)]"
            >
              Применено правил: {applyMutation.data.createdRulesCount}.
            </div>
          )}

          <div className="overflow-x-auto rounded-2xl border border-[var(--app-border)]">
            <table className="w-full min-w-[1050px] border-collapse text-left text-sm">
              <thead className="bg-[var(--app-surface)] text-[var(--app-muted)]">
                <tr>
                  <th className="px-4 py-3 font-medium">Строка</th>
                  <th className="px-4 py-3 font-medium">Комплектующее</th>
                  <th className="px-4 py-3 font-medium">Тип основного товара</th>
                  <th className="px-4 py-3 font-medium">Потребность</th>
                  <th className="px-4 py-3 font-medium">Условия</th>
                  <th className="px-4 py-3 font-medium">Результат</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[var(--app-border)]">
                {previewMutation.data.rows.slice(0, 200).map((row) => (
                  <tr key={`${row.rowNumber}-${row.componentArticle}`}>
                    <td className="px-4 py-4 text-[var(--app-muted)]">
                      {row.rowNumber}
                    </td>
                    <td className="px-4 py-4 font-medium text-[var(--app-text)]">
                      {row.componentArticle || "—"}
                      {canEditProducts &&
                        row.status === "ComponentNotFound" &&
                        row.componentArticle.trim().length > 0 && (
                          <AppButton
                            type="button"
                            size="sm"
                            variant="secondary"
                            className="mt-3"
                            onClick={() =>
                              setMissingComponentArticle(row.componentArticle)
                            }
                          >
                            Добавить в каталог
                          </AppButton>
                        )}
                    </td>
                    <td className="px-4 py-4 text-[var(--app-text)]">
                      {row.mainProductType || "—"}
                    </td>
                    <td className="px-4 py-4 text-[var(--app-text)]">
                      {row.need || "Совместимый аксессуар"}
                    </td>
                    <td className="px-4 py-4 text-[var(--app-muted)]">
                      {row.constraints.length === 0
                        ? "Без ограничений"
                        : row.constraints
                            .map(
                              (item) => {
                                const operator =
                                  item.comparisonOperator === "GreaterThanOrEqual"
                                    ? "от "
                                    : item.comparisonOperator === "LessThanOrEqual"
                                      ? "до "
                                      : "";
                                return `${item.characteristicName}: ${operator}${item.value}`;
                              },
                            )
                            .join("; ")}
                    </td>
                    <td className="px-4 py-4">
                      <span
                        className={`inline-flex rounded-full border px-3 py-1 text-xs font-semibold ${getStatusClassName(row.status)}`}
                      >
                        {getStatusLabel(row.status)}
                      </span>
                      <p className="mt-2 text-xs text-[var(--app-muted)]">
                        После применения: {row.isActive ? "работает" : "отключено"}
                      </p>
                      {row.message && (
                        <p className="mt-2 text-xs text-[var(--app-muted)]">
                          {row.message}
                        </p>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {missingComponentArticle && (
        <CreateCatalogProductDialog
          article={missingComponentArticle}
          name={missingComponentArticle}
          manufacturerName={null}
          kind="Component"
          priceAmount={0}
          onClose={() => setMissingComponentArticle(null)}
          onCreated={handleComponentCreated}
        />
      )}
    </section>
  );
}
