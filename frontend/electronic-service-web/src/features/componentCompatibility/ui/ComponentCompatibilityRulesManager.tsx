"use client";

import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { getCatalogProductTypeCharacteristicSchema } from "@/features/catalogProductTypes/api/getCatalogProductTypeCharacteristicSchema";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppButton } from "@/shared/ui/AppButton";
import { downloadCurrentComponentCompatibilityRules } from "../api/componentCompatibilityImport";
import {
  deleteComponentCompatibilityRule,
  getComponentCompatibilityRules,
  updateComponentCompatibilityRule,
} from "../api/componentCompatibilityManagement";
import type {
  ComponentCompatibilityComparisonOperator,
  ComponentCompatibilityRule,
  ComponentCompatibilityRuleGroupInput,
} from "../model/types";

interface ComponentCompatibilityRulesManagerProps {
  productTypeCode: string;
}

interface EditableConstraint {
  characteristicDefinitionId: string;
  value: string;
  comparisonOperator: ComponentCompatibilityComparisonOperator;
}

interface EditableGroup {
  key: string;
  constraints: EditableConstraint[];
}

interface ManufacturerSelection {
  productTypeCode: string;
  ids: string[] | null;
}

function toEditableGroups(rule: ComponentCompatibilityRule): EditableGroup[] {
  return rule.conditionGroups.map((group) => ({
    key: group.id,
    constraints: group.constraints.map((constraint) => ({
      characteristicDefinitionId: constraint.characteristicDefinitionId,
      value: constraint.value,
      comparisonOperator: constraint.comparisonOperator ?? "Equal",
    })),
  }));
}

function toRequestGroups(groups: EditableGroup[]): ComponentCompatibilityRuleGroupInput[] {
  return groups.map((group) => ({
    constraints: group.constraints.map((constraint) => ({
      characteristicDefinitionId: constraint.characteristicDefinitionId,
      value: constraint.value.trim(),
      comparisonOperator: constraint.comparisonOperator,
    })),
  }));
}

function formatRuleValue(value: string, dataType?: string): string {
  if (dataType?.toLowerCase() === "boolean") {
    return value.toLowerCase() === "true" ? "Да" : "Нет";
  }

  if (dataType?.toLowerCase() === "number") {
    const number = Number(value);
    return Number.isFinite(number) ? String(number) : value;
  }

  return value;
}

function formatRuleGroup(
  rule: ComponentCompatibilityRule,
  groupIndex: number,
  dataTypesByDefinitionId: Map<string, string>,
) {
  const group = rule.conditionGroups[groupIndex];

  if (!group || group.constraints.length === 0) {
    return "Подходит всем товарам выбранного типа";
  }

  const byCharacteristic = new Map<
    string,
    {
      name: string;
      values: Array<{
        value: string;
        comparisonOperator: ComponentCompatibilityComparisonOperator;
      }>;
    }
  >();

  for (const constraint of group.constraints) {
    const entry = byCharacteristic.get(constraint.characteristicDefinitionId) ?? {
      name: constraint.characteristicName,
      values: [],
    };
    entry.values.push({
      value: formatRuleValue(
        constraint.value,
        dataTypesByDefinitionId.get(constraint.characteristicDefinitionId),
      ),
      comparisonOperator: constraint.comparisonOperator ?? "Equal",
    });
    byCharacteristic.set(constraint.characteristicDefinitionId, entry);
  }

  return Array.from(byCharacteristic.values())
    .map(({ name, values }) => {
      const exactValues = values
        .filter((item) => item.comparisonOperator === "Equal")
        .map((item) => item.value);
      const lower = values.find(
        (item) => item.comparisonOperator === "GreaterThanOrEqual",
      );
      const upper = values.find(
        (item) => item.comparisonOperator === "LessThanOrEqual",
      );
      if (lower || upper) {
        const range = [
          lower ? `от ${lower.value}` : null,
          upper ? `до ${upper.value}` : null,
        ]
          .filter(Boolean)
          .join(" ");
        return `${name}: ${range}`;
      }

      return `${name}: ${exactValues.join(" или ")}`;
    })
    .join(" · И · ");
}

export function ComponentCompatibilityRulesManager({
  productTypeCode,
}: ComponentCompatibilityRulesManagerProps) {
  const queryClient = useQueryClient();
  const [editingRuleId, setEditingRuleId] = useState<string | null>(null);
  const [editableGroups, setEditableGroups] = useState<EditableGroup[]>([]);
  const [ruleSearch, setRuleSearch] = useState("");
  const [activityFilter, setActivityFilter] = useState<
    "all" | "active" | "inactive"
  >("all");
  const [manufacturerSearch, setManufacturerSearch] = useState("");
  const [manufacturerSelection, setManufacturerSelection] =
    useState<ManufacturerSelection>({
      productTypeCode,
      ids: null,
    });
  const selectedManufacturerIds =
    manufacturerSelection.productTypeCode === productTypeCode
      ? manufacturerSelection.ids
      : null;

  const rulesQuery = useQuery({
    queryKey: ["component-compatibility-rules", productTypeCode],
    queryFn: () => getComponentCompatibilityRules(productTypeCode),
    enabled: productTypeCode.length > 0,
  });
  const schemaQuery = useQuery({
    queryKey: ["catalog-product-type-characteristic-schema", productTypeCode],
    queryFn: () => getCatalogProductTypeCharacteristicSchema(productTypeCode),
    enabled: productTypeCode.length > 0,
  });
  const mutation = useMutation({
    mutationFn: ({
      ruleId,
      isActive,
      groups,
    }: {
      ruleId: string;
      isActive: boolean;
      groups: ComponentCompatibilityRuleGroupInput[];
    }) => updateComponentCompatibilityRule(ruleId, isActive, groups),
    onSuccess: async () => {
      setEditingRuleId(null);
      await queryClient.invalidateQueries({
        queryKey: ["component-compatibility-rules", productTypeCode],
      });
      await queryClient.invalidateQueries({ queryKey: ["component-offers"] });
    },
  });
  const deleteMutation = useMutation({
    mutationFn: deleteComponentCompatibilityRule,
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ["component-compatibility-rules", productTypeCode],
      });
      await queryClient.invalidateQueries({ queryKey: ["component-offers"] });
    },
  });
  const exportMutation = useMutation({
    mutationFn: () =>
      downloadCurrentComponentCompatibilityRules(
        productTypeCode,
        selectedManufacturerIds ?? undefined,
      ),
  });

  const characteristicOptions = useMemo(
    () => schemaQuery.data?.characteristics ?? [],
    [schemaQuery.data?.characteristics],
  );
  const dataTypesByDefinitionId = useMemo(
    () =>
      new Map(
        characteristicOptions.map((item) => [item.definitionId, item.dataType]),
      ),
    [characteristicOptions],
  );
  const rules = useMemo(() => rulesQuery.data ?? [], [rulesQuery.data]);
  const manufacturerOptions = useMemo(() => {
    const manufacturers = new Map<
      string,
      { id: string; name: string; rulesCount: number }
    >();

    for (const rule of rules) {
      const current = manufacturers.get(rule.componentManufacturerId);
      manufacturers.set(rule.componentManufacturerId, {
        id: rule.componentManufacturerId,
        name: rule.componentManufacturerName,
        rulesCount: (current?.rulesCount ?? 0) + 1,
      });
    }

    return Array.from(manufacturers.values()).sort((left, right) =>
      left.name.localeCompare(right.name, "ru"),
    );
  }, [rules]);
  const selectedManufacturerSet = useMemo(
    () =>
      selectedManufacturerIds === null
        ? null
        : new Set(selectedManufacturerIds),
    [selectedManufacturerIds],
  );
  const selectedRulesCount =
    selectedManufacturerSet === null
      ? rules.length
      : rules.filter((rule) =>
          selectedManufacturerSet.has(rule.componentManufacturerId),
        ).length;
  const normalizedManufacturerSearch = manufacturerSearch
    .trim()
    .toLocaleLowerCase("ru");
  const visibleManufacturerOptions = manufacturerOptions.filter(
    (manufacturer) =>
      normalizedManufacturerSearch.length === 0 ||
      manufacturer.name
        .toLocaleLowerCase("ru")
        .includes(normalizedManufacturerSearch),
  );

  if (rulesQuery.isLoading || schemaQuery.isLoading) {
    return <p className="text-sm text-[var(--app-muted)]">Загружаем правила…</p>;
  }

  if (rulesQuery.isError || schemaQuery.isError) {
    return (
      <div className="rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm text-[var(--app-danger)]">
        {getApiErrorMessage(
          rulesQuery.error ?? schemaQuery.error,
          "Не удалось загрузить правила совместимости.",
        )}
      </div>
    );
  }

  const normalizedSearch = ruleSearch.trim().toLocaleLowerCase("ru");
  const visibleRules = rules.filter((rule) => {
    const matchesSearch =
      normalizedSearch.length === 0 ||
      rule.componentArticle.toLocaleLowerCase("ru").includes(normalizedSearch) ||
      rule.componentName.toLocaleLowerCase("ru").includes(normalizedSearch) ||
      rule.componentManufacturerName
        .toLocaleLowerCase("ru")
        .includes(normalizedSearch) ||
      (rule.needName ?? "совместимые аксессуары")
        .toLocaleLowerCase("ru")
        .includes(normalizedSearch);
    const matchesActivity =
      activityFilter === "all" ||
      (activityFilter === "active" && rule.isActive) ||
      (activityFilter === "inactive" && !rule.isActive);
    const matchesManufacturer =
      selectedManufacturerSet === null ||
      selectedManufacturerSet.has(rule.componentManufacturerId);
    return matchesSearch && matchesActivity && matchesManufacturer;
  });

  function toggleManufacturer(manufacturerId: string): void {
    setManufacturerSelection((currentSelection) => {
      const allManufacturerIds = manufacturerOptions.map((item) => item.id);
      const current =
        currentSelection.productTypeCode === productTypeCode
          ? currentSelection.ids
          : null;
      const selected = new Set(current ?? allManufacturerIds);

      if (selected.has(manufacturerId)) {
        selected.delete(manufacturerId);
      } else {
        selected.add(manufacturerId);
      }

      return {
        productTypeCode,
        ids:
          selected.size === allManufacturerIds.length
            ? null
            : allManufacturerIds.filter((id) => selected.has(id)),
      };
    });
  }

  return (
    <section className="grid gap-4">
      <div className="rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4 text-sm leading-6 text-[var(--app-muted)]">
        <strong className="text-[var(--app-text)]">Как читается правило:</strong>{" "}
        тип товара задаёт область поиска. Внутри одной группы все разные
        характеристики обязательны одновременно. Несколько значений одной
        характеристики и несколько групп — альтернативы.
      </div>

      <div className="grid gap-4 rounded-2xl border border-[var(--app-accent)]/45 bg-[var(--app-accent-soft)] p-5 lg:grid-cols-[minmax(0,1fr)_minmax(300px,420px)] lg:items-end">
        <div>
          <p className="font-semibold text-[var(--app-text)]">
            Выгрузка правил в Excel
          </p>
          <p className="mt-1 text-sm leading-6 text-[var(--app-muted)]">
            Выберите производителей комплектующих. Фильтр применяется и к
            списку ниже, и к скачиваемому файлу. Правила, которых нет в файле,
            при обратной загрузке не удаляются.
          </p>
        </div>

        <div className="grid gap-3 sm:grid-cols-[minmax(0,1fr)_auto]">
          <details className="group relative">
            <summary className="flex min-h-12 cursor-pointer list-none items-center justify-between gap-3 rounded-xl border border-[var(--app-border-strong)] bg-[var(--app-panel)] px-4 text-sm font-semibold text-[var(--app-text)] transition-colors hover:bg-[var(--app-surface-hover)]">
              <span className="min-w-0 truncate">
                {selectedManufacturerIds === null
                  ? `Все производители (${rules.length})`
                  : selectedManufacturerIds.length === 0
                    ? "Производители не выбраны"
                    : `Выбрано: ${selectedManufacturerIds.length} · правил: ${selectedRulesCount}`}
              </span>
              <span
                aria-hidden="true"
                className="text-[var(--app-muted)] transition-transform group-open:rotate-180"
              >
                ▾
              </span>
            </summary>

            <div className="absolute right-0 z-20 mt-2 max-h-80 w-full min-w-72 overflow-y-auto rounded-2xl border border-[var(--app-border-strong)] bg-[var(--app-panel)] p-3 shadow-2xl">
              <div className="mb-2 flex items-center justify-between gap-2 border-b border-[var(--app-border)] pb-2">
                <button
                  type="button"
                  className="rounded-lg px-2 py-1 text-xs font-semibold text-[var(--app-accent)] hover:bg-[var(--app-accent-soft)]"
                  onClick={() =>
                    setManufacturerSelection({ productTypeCode, ids: null })
                  }
                >
                  Выбрать всех
                </button>
                <button
                  type="button"
                  className="rounded-lg px-2 py-1 text-xs font-semibold text-[var(--app-muted)] hover:bg-[var(--app-surface-hover)] hover:text-[var(--app-text)]"
                  onClick={() =>
                    setManufacturerSelection({ productTypeCode, ids: [] })
                  }
                >
                  Снять выбор
                </button>
              </div>

              <input
                value={manufacturerSearch}
                onChange={(event) => setManufacturerSearch(event.target.value)}
                placeholder="Найти производителя"
                aria-label="Найти производителя комплектующего"
                className="mb-2 min-h-10 w-full rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] px-3 text-sm text-[var(--app-text)]"
              />

              <div className="grid gap-1">
                {visibleManufacturerOptions.map((manufacturer) => (
                  <label
                    key={manufacturer.id}
                    className="flex cursor-pointer items-center gap-3 rounded-xl px-2 py-2 text-sm text-[var(--app-text)] hover:bg-[var(--app-surface-hover)]"
                  >
                    <input
                      type="checkbox"
                      checked={
                        selectedManufacturerSet === null ||
                        selectedManufacturerSet.has(manufacturer.id)
                      }
                      onChange={() => toggleManufacturer(manufacturer.id)}
                      className="h-4 w-4 accent-[var(--app-accent)]"
                    />
                    <span className="min-w-0 flex-1 truncate">
                      {manufacturer.name}
                    </span>
                    <span className="rounded-full bg-[var(--app-surface)] px-2 py-0.5 text-xs text-[var(--app-muted)]">
                      {manufacturer.rulesCount}
                    </span>
                  </label>
                ))}
                {visibleManufacturerOptions.length === 0 && (
                  <p className="px-2 py-3 text-center text-sm text-[var(--app-muted)]">
                    Производители не найдены.
                  </p>
                )}
              </div>
            </div>
          </details>

          <AppButton
            type="button"
            variant="primary"
            loading={exportMutation.isPending}
            disabled={selectedRulesCount === 0}
            className="min-h-12 px-6 shadow-lg shadow-black/20"
            onClick={() => exportMutation.mutate()}
          >
            <svg
              aria-hidden="true"
              viewBox="0 0 24 24"
              fill="none"
              className="h-5 w-5"
            >
              <path
                d="M12 3v12m0 0 4-4m-4 4-4-4M5 17v3h14v-3"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            </svg>
            Скачать {selectedRulesCount} правил
          </AppButton>
        </div>
      </div>

      {exportMutation.isError && (
        <div className="rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm text-[var(--app-danger)]">
          {getApiErrorMessage(
            exportMutation.error,
            "Не удалось выгрузить текущие правила.",
          )}
        </div>
      )}

      {exportMutation.isSuccess && (
        <div className="rounded-2xl border border-[var(--app-success-border)] bg-[var(--app-success-soft)] p-4 text-sm text-[var(--app-success)]">
          Файл «{exportMutation.data}» скачан.
        </div>
      )}

      {rules.length > 0 && (
        <div className="grid gap-3 rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4 md:grid-cols-[minmax(240px,1fr)_220px]">
          <input
            value={ruleSearch}
            onChange={(event) => setRuleSearch(event.target.value)}
            placeholder="Найти по артикулу, названию, производителю или потребности"
            className="min-h-11 rounded-xl border border-[var(--app-border)] bg-[var(--app-panel)] px-3 text-sm text-[var(--app-text)]"
          />
          <select
            value={activityFilter}
            onChange={(event) =>
              setActivityFilter(
                event.target.value as "all" | "active" | "inactive",
              )
            }
            className="min-h-11 rounded-xl border border-[var(--app-border)] bg-[var(--app-panel)] px-3 text-sm text-[var(--app-text)]"
          >
            <option value="all">Все состояния</option>
            <option value="active">Только работающие</option>
            <option value="inactive">Только отключённые</option>
          </select>
        </div>
      )}

      {rules.length === 0 && (
        <div className="rounded-3xl border border-dashed border-[var(--app-border)] p-8 text-center text-sm text-[var(--app-muted)]">
          Для выбранного типа правил пока нет. Создайте их через вкладку
          «Excel».
        </div>
      )}


      {rules.length > 0 && visibleRules.length === 0 && (
        <div className="rounded-3xl border border-dashed border-[var(--app-border)] p-8 text-center text-sm text-[var(--app-muted)]">
          По выбранным условиям правила не найдены.
        </div>
      )}

      {visibleRules.map((rule) => {
        const isEditing = editingRuleId === rule.id;

        return (
          <article
            key={rule.id}
            className="rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5"
          >
            <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
              <div>
                <div className="flex flex-wrap items-center gap-2">
                  <h3 className="font-semibold text-[var(--app-text)]">
                    {rule.componentArticle} · {rule.componentName}
                  </h3>
                  <span
                    className={`rounded-full border px-2.5 py-1 text-xs font-semibold ${
                      rule.isActive
                        ? "border-[var(--app-success-border)] bg-[var(--app-success-soft)] text-[var(--app-success)]"
                        : "border-[var(--app-border)] bg-[var(--app-surface)] text-[var(--app-muted)]"
                    }`}
                  >
                    {rule.isActive ? "Работает" : "Отключено"}
                  </span>
                </div>
                <p className="mt-2 text-sm text-[var(--app-muted)]">
                  Производитель комплектующего: {rule.componentManufacturerName}
                </p>
                <p className="mt-1 text-sm text-[var(--app-muted)]">
                  {rule.needName
                    ? `Потребность: ${rule.needName}`
                    : "Совместимый аксессуар без потребности"}
                </p>
              </div>

              <div className="flex flex-wrap gap-2">
                <AppButton
                  size="sm"
                  onClick={() => {
                    setEditingRuleId(rule.id);
                    setEditableGroups(toEditableGroups(rule));
                    mutation.reset();
                  }}
                >
                  Изменить условия
                </AppButton>
                <AppButton
                  size="sm"
                  variant={rule.isActive ? "warning" : "primary"}
                  loading={mutation.isPending}
                  onClick={() =>
                    mutation.mutate({
                      ruleId: rule.id,
                      isActive: !rule.isActive,
                      groups: rule.conditionGroups.map((group) => ({
                        constraints: group.constraints.map((constraint) => ({
                          characteristicDefinitionId:
                            constraint.characteristicDefinitionId,
                          value: constraint.value,
                        })),
                      })),
                    })
                  }
                >
                  {rule.isActive ? "Отключить" : "Включить"}
                </AppButton>
                <AppButton
                  size="sm"
                  variant="danger"
                  loading={deleteMutation.isPending}
                  onClick={() => {
                    if (window.confirm("Удалить это правило совместимости?")) {
                      deleteMutation.mutate(rule.id);
                    }
                  }}
                >
                  Удалить
                </AppButton>
              </div>
            </div>

            {!isEditing && (
              <div className="mt-4 grid gap-3">
                {rule.conditionGroups.map((group, groupIndex) => (
                  <div
                    key={group.id}
                    className="rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4"
                  >
                    <p className="text-xs font-semibold uppercase tracking-wide text-[var(--app-muted)]">
                      Вариант {groupIndex + 1}
                    </p>
                    <p className="mt-2 text-sm text-[var(--app-text)]">
                      {formatRuleGroup(
                        rule,
                        groupIndex,
                        dataTypesByDefinitionId,
                      )}
                    </p>
                  </div>
                ))}
              </div>
            )}

            {isEditing && (
              <div className="mt-5 grid gap-4">
                {editableGroups.map((group, groupIndex) => (
                  <div
                    key={group.key}
                    className="rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4"
                  >
                    <div className="flex items-center justify-between gap-3">
                      <p className="font-semibold text-[var(--app-text)]">
                        Вариант {groupIndex + 1}
                      </p>
                      {editableGroups.length > 1 && (
                        <AppButton
                          size="sm"
                          variant="ghost"
                          onClick={() =>
                            setEditableGroups((current) =>
                              current.filter((_, index) => index !== groupIndex),
                            )
                          }
                        >
                          Удалить вариант
                        </AppButton>
                      )}
                    </div>

                    {group.constraints.length === 0 ? (
                      <p className="mt-3 text-sm text-[var(--app-muted)]">
                        Без условий — комплектующее подходит всем товарам этого
                        типа.
                      </p>
                    ) : (
                      <div className="mt-3 grid gap-3">
                        {group.constraints.map((constraint, constraintIndex) => {
                          const selectedCharacteristic =
                            characteristicOptions.find(
                              (item) =>
                                item.definitionId ===
                                constraint.characteristicDefinitionId,
                            );
                          const dataType =
                            selectedCharacteristic?.dataType.toLowerCase();

                          return (
                            <div
                            key={`${group.key}-${constraintIndex}`}
                            className="grid gap-2 md:grid-cols-[minmax(220px,1fr)_minmax(150px,0.6fr)_minmax(160px,1fr)_auto]"
                          >
                            <select
                              value={constraint.characteristicDefinitionId}
                              onChange={(event) =>
                                setEditableGroups((current) =>
                                  current.map((item, index) =>
                                    index !== groupIndex
                                      ? item
                                      : {
                                          ...item,
                                          constraints: item.constraints.map(
                                            (value, valueIndex) =>
                                              valueIndex === constraintIndex
                                                ? {
                                                    ...value,
                                                    characteristicDefinitionId:
                                                      event.target.value,
                                                    value: "",
                                                    comparisonOperator: "Equal",
                                                  }
                                                : value,
                                          ),
                                        },
                                  ),
                                )
                              }
                              className="min-h-11 rounded-xl border border-[var(--app-border)] bg-[var(--app-panel)] px-3 text-sm text-[var(--app-text)]"
                            >
                              {characteristicOptions.map((characteristic) => (
                                <option
                                  key={characteristic.definitionId}
                                  value={characteristic.definitionId}
                                >
                                  {characteristic.name}
                                </option>
                              ))}
                            </select>
                            <select
                              value={constraint.comparisonOperator}
                              disabled={dataType !== "number"}
                              onChange={(event) =>
                                setEditableGroups((current) =>
                                  current.map((item, index) =>
                                    index !== groupIndex
                                      ? item
                                      : {
                                          ...item,
                                          constraints: item.constraints.map(
                                            (value, valueIndex) =>
                                              valueIndex === constraintIndex
                                                ? {
                                                    ...value,
                                                    comparisonOperator:
                                                      event.target.value as ComponentCompatibilityComparisonOperator,
                                                  }
                                                : value,
                                          ),
                                        },
                                  ),
                                )
                              }
                              className="min-h-11 rounded-xl border border-[var(--app-border)] bg-[var(--app-panel)] px-3 text-sm text-[var(--app-text)] disabled:opacity-60"
                            >
                              <option value="Equal">Равно</option>
                              <option value="GreaterThanOrEqual">От (включительно)</option>
                              <option value="LessThanOrEqual">До (включительно)</option>
                            </select>
                            {dataType === "boolean" ? (
                              <select
                                value={constraint.value.toLowerCase()}
                                onChange={(event) =>
                                  setEditableGroups((current) =>
                                    current.map((item, index) =>
                                      index !== groupIndex
                                        ? item
                                        : {
                                            ...item,
                                            constraints: item.constraints.map(
                                              (value, valueIndex) =>
                                                valueIndex === constraintIndex
                                                  ? {
                                                      ...value,
                                                      value: event.target.value,
                                                    }
                                                  : value,
                                            ),
                                          },
                                    ),
                                  )
                                }
                                className="min-h-11 rounded-xl border border-[var(--app-border)] bg-[var(--app-panel)] px-3 text-sm text-[var(--app-text)]"
                              >
                                <option value="">Выберите Да или Нет</option>
                                <option value="true">Да</option>
                                <option value="false">Нет</option>
                              </select>
                            ) : (
                              <div className="relative">
                                <input
                                  type={dataType === "number" ? "number" : "text"}
                                  step={dataType === "number" ? "any" : undefined}
                                  value={constraint.value}
                                  placeholder={
                                    dataType === "number"
                                      ? "Введите число"
                                      : "Введите значение"
                                  }
                                  onChange={(event) =>
                                    setEditableGroups((current) =>
                                      current.map((item, index) =>
                                        index !== groupIndex
                                          ? item
                                          : {
                                              ...item,
                                              constraints: item.constraints.map(
                                                (value, valueIndex) =>
                                                  valueIndex === constraintIndex
                                                    ? {
                                                        ...value,
                                                        value: event.target.value,
                                                      }
                                                    : value,
                                              ),
                                            },
                                      ),
                                    )
                                  }
                                  className="min-h-11 w-full rounded-xl border border-[var(--app-border)] bg-[var(--app-panel)] px-3 text-sm text-[var(--app-text)]"
                                />
                                {selectedCharacteristic?.unit && (
                                  <span className="mt-1 block text-xs text-[var(--app-muted)]">
                                    Единица: {selectedCharacteristic.unit}
                                  </span>
                                )}
                              </div>
                            )}
                            <AppButton
                              size="sm"
                              variant="ghost"
                              onClick={() =>
                                setEditableGroups((current) =>
                                  current.map((item, index) =>
                                    index !== groupIndex
                                      ? item
                                      : {
                                          ...item,
                                          constraints: item.constraints.filter(
                                            (_, valueIndex) =>
                                              valueIndex !== constraintIndex,
                                          ),
                                        },
                                  ),
                                )
                              }
                            >
                              Убрать
                            </AppButton>
                          </div>
                          );
                        })}
                      </div>
                    )}

                    <AppButton
                      size="sm"
                      className="mt-3"
                      disabled={characteristicOptions.length === 0}
                      onClick={() =>
                        setEditableGroups((current) =>
                          current.map((item, index) =>
                            index !== groupIndex
                              ? item
                              : {
                                  ...item,
                                  constraints: [
                                    ...item.constraints,
                                    {
                                      characteristicDefinitionId:
                                        characteristicOptions[0]?.definitionId ?? "",
                                      value: "",
                                      comparisonOperator: "Equal",
                                    },
                                  ],
                                },
                          ),
                        )
                      }
                    >
                      Добавить условие
                    </AppButton>
                  </div>
                ))}

                <div className="flex flex-wrap gap-2">
                  <AppButton
                    onClick={() =>
                      setEditableGroups((current) => [
                        ...current,
                        { key: crypto.randomUUID(), constraints: [] },
                      ])
                    }
                  >
                    Добавить альтернативный вариант
                  </AppButton>
                  <AppButton
                    variant="primary"
                    loading={mutation.isPending}
                    disabled={
                      editableGroups.length === 0 ||
                      (editableGroups.length > 1 &&
                        editableGroups.some(
                          (group) => group.constraints.length === 0,
                        )) ||
                      editableGroups.some((group) =>
                        group.constraints.some(
                          (constraint) =>
                            !constraint.characteristicDefinitionId ||
                            !constraint.value.trim(),
                        ),
                      )
                    }
                    onClick={() =>
                      mutation.mutate({
                        ruleId: rule.id,
                        isActive: rule.isActive,
                        groups: toRequestGroups(editableGroups),
                      })
                    }
                  >
                    Сохранить правило
                  </AppButton>
                  <AppButton
                    variant="ghost"
                    onClick={() => setEditingRuleId(null)}
                  >
                    Отмена
                  </AppButton>
                </div>

                {mutation.isError && (
                  <div className="rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm text-[var(--app-danger)]">
                    {getApiErrorMessage(
                      mutation.error,
                      "Не удалось сохранить правило.",
                    )}
                  </div>
                )}
              </div>
            )}
          </article>
        );
      })}
    </section>
  );
}
