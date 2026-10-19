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
      downloadCurrentComponentCompatibilityRules(productTypeCode),
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

  const rules = rulesQuery.data ?? [];
  const normalizedSearch = ruleSearch.trim().toLocaleLowerCase("ru");
  const visibleRules = rules.filter((rule) => {
    const matchesSearch =
      normalizedSearch.length === 0 ||
      rule.componentArticle.toLocaleLowerCase("ru").includes(normalizedSearch) ||
      rule.componentName.toLocaleLowerCase("ru").includes(normalizedSearch) ||
      (rule.needName ?? "совместимые аксессуары")
        .toLocaleLowerCase("ru")
        .includes(normalizedSearch);
    const matchesActivity =
      activityFilter === "all" ||
      (activityFilter === "active" && rule.isActive) ||
      (activityFilter === "inactive" && !rule.isActive);
    return matchesSearch && matchesActivity;
  });

  return (
    <section className="grid gap-4">
      <div className="rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4 text-sm leading-6 text-[var(--app-muted)]">
        <strong className="text-[var(--app-text)]">Как читается правило:</strong>{" "}
        тип товара задаёт область поиска. Внутри одной группы все разные
        характеристики обязательны одновременно. Несколько значений одной
        характеристики и несколько групп — альтернативы.
      </div>

      <div className="flex flex-col gap-3 rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4 sm:flex-row sm:items-center sm:justify-between">
        <p className="text-sm text-[var(--app-muted)]">
          Выгрузите все текущие правила этого типа, измените Excel и загрузите
          его во вкладке «Excel». Правила, которых нет в файле, не удаляются.
        </p>
        <AppButton
          type="button"
          variant="secondary"
          loading={exportMutation.isPending}
          disabled={rules.length === 0}
          onClick={() => exportMutation.mutate()}
        >
          Скачать текущие правила
        </AppButton>
      </div>

      {exportMutation.isError && (
        <div className="rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm text-[var(--app-danger)]">
          {getApiErrorMessage(
            exportMutation.error,
            "Не удалось выгрузить текущие правила.",
          )}
        </div>
      )}

      {rules.length > 0 && (
        <div className="grid gap-3 rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4 md:grid-cols-[minmax(240px,1fr)_220px]">
          <input
            value={ruleSearch}
            onChange={(event) => setRuleSearch(event.target.value)}
            placeholder="Найти по артикулу, названию или потребности"
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
