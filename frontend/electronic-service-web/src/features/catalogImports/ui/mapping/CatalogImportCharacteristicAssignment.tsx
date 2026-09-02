"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { createCharacteristicDefinition } from "@/features/catalogCharacteristicDefinitions/api/createCharacteristicDefinition";
import type { CatalogCharacteristicDataType } from "@/features/catalogCharacteristicDefinitions/model/types";
import { addOptionalCharacteristicToProductType } from "@/features/catalogProductTypes/api/addOptionalCharacteristicToProductType";
import { getAvailableCharacteristicDefinitions } from "@/features/catalogProductTypes/api/getAvailableCharacteristicDefinitions";
import type { AvailableCharacteristicDefinition } from "@/features/catalogProductTypes/model/types";
import type { CatalogProductTypeCharacteristicMetadata } from "@/features/catalogMetadata/model/types";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppSelect } from "@/shared/ui/AppSelect";

type CharacteristicConfigurationMode = "closed" | "attach" | "create";

interface CatalogImportCharacteristicAssignmentProps {
  columnId: string;
  sourceHeader: string;
  productTypeCode: string;
  productTypeName: string;
  value: string;
  characteristics: CatalogProductTypeCharacteristicMetadata[];
  usedCharacteristicIds: ReadonlySet<string>;
  disabled: boolean;
  onChange: (characteristicDefinitionId: string) => void;
  onBusyChange: (columnId: string, isBusy: boolean) => void;
}

interface CreatedCharacteristicResult {
  definitionId: string;
  definitionName: string;
}

function formatCharacteristicDataType(dataType: string): string {
  switch (dataType) {
    case "Text":
      return "Текст";

    case "Number":
      return "Число";

    case "Boolean":
      return "Да / Нет";

    default:
      return dataType;
  }
}

function getCharacteristicLabel(
  characteristic: CatalogProductTypeCharacteristicMetadata,
): string {
  const unit = characteristic.unit ? `, ${characteristic.unit}` : "";
  const required = characteristic.isRequired ? " · обязательная" : "";

  return `${characteristic.name}${unit} · ${formatCharacteristicDataType(characteristic.dataType)}${required}`;
}

function getAvailableDefinitionLabel(
  definition: AvailableCharacteristicDefinition,
): string {
  const unit = definition.unit ? `, ${definition.unit}` : "";

  return `${definition.name}${unit} · ${formatCharacteristicDataType(definition.dataType)} · ${definition.code}`;
}

function transliterateRussianCharacter(character: string): string {
  const transliterationMap: Record<string, string> = {
    а: "A",
    б: "B",
    в: "V",
    г: "G",
    д: "D",
    е: "E",
    ё: "E",
    ж: "ZH",
    з: "Z",
    и: "I",
    й: "Y",
    к: "K",
    л: "L",
    м: "M",
    н: "N",
    о: "O",
    п: "P",
    р: "R",
    с: "S",
    т: "T",
    у: "U",
    ф: "F",
    х: "H",
    ц: "TS",
    ч: "CH",
    ш: "SH",
    щ: "SCH",
    ъ: "",
    ы: "Y",
    ь: "",
    э: "E",
    ю: "YU",
    я: "YA",
  };

  return transliterationMap[character.toLocaleLowerCase("ru-RU")] ?? character;
}

function createCharacteristicCodeFromHeader(sourceHeader: string): string {
  const transliteratedValue = Array.from(sourceHeader)
    .map(transliterateRussianCharacter)
    .join("");

  const normalizedCode = transliteratedValue
    .toUpperCase()
    .replace(/[^A-Z0-9]+/g, "_")
    .replace(/_+/g, "_")
    .replace(/^_+|_+$/g, "")
    .slice(0, 100);

  return normalizedCode.length > 0 ? normalizedCode : "NEW_CHARACTERISTIC";
}

export function CatalogImportCharacteristicAssignment({
  columnId,
  sourceHeader,
  productTypeCode,
  productTypeName,
  value,
  characteristics,
  usedCharacteristicIds,
  disabled,
  onChange,
  onBusyChange,
}: CatalogImportCharacteristicAssignmentProps) {
  const queryClient = useQueryClient();

  const [mode, setMode] = useState<CharacteristicConfigurationMode>("closed");

  const [searchDraft, setSearchDraft] = useState(sourceHeader);
  const [appliedSearch, setAppliedSearch] = useState(sourceHeader);
  const [selectedAvailableDefinitionId, setSelectedAvailableDefinitionId] =
    useState("");

  const [newDefinitionCode, setNewDefinitionCode] = useState(() =>
    createCharacteristicCodeFromHeader(sourceHeader),
  );
  const [newDefinitionName, setNewDefinitionName] = useState(sourceHeader);
  const [newDefinitionDataType, setNewDefinitionDataType] =
    useState<CatalogCharacteristicDataType>("Text");
  const [newDefinitionUnit, setNewDefinitionUnit] = useState("");

  const [validationError, setValidationError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const availableDefinitionsQuery = useQuery({
    queryKey: [
      "catalog-product-type-available-definitions",
      productTypeCode,
      appliedSearch,
    ],
    queryFn: () =>
      getAvailableCharacteristicDefinitions(productTypeCode, appliedSearch),
    enabled: mode === "attach" && productTypeCode.length > 0,
  });

  const availableDefinitions = availableDefinitionsQuery.data ?? [];

  const selectedAvailableDefinition =
    availableDefinitions.find(
      (definition) => definition.id === selectedAvailableDefinitionId,
    ) ?? null;

  async function refreshProductTypeCharacteristics(): Promise<void> {
    await Promise.all([
      queryClient.invalidateQueries({
        queryKey: [
          "catalog-product-type-characteristic-schema",
          productTypeCode,
        ],
      }),
      queryClient.invalidateQueries({
        queryKey: [
          "catalog-product-type-available-definitions",
          productTypeCode,
        ],
      }),
      queryClient.invalidateQueries({
        queryKey: ["catalog-product-type-characteristics", productTypeCode],
      }),
      queryClient.invalidateQueries({
        queryKey: ["catalog-characteristic-definitions"],
      }),
    ]);

    await queryClient.refetchQueries({
      queryKey: ["catalog-product-type-characteristics", productTypeCode],
      type: "active",
    });
  }

  const attachMutation = useMutation({
    mutationFn: async (
      definition: AvailableCharacteristicDefinition,
    ): Promise<AvailableCharacteristicDefinition> => {
      await addOptionalCharacteristicToProductType(productTypeCode, {
        characteristicDefinitionId: definition.id,
      });

      return definition;
    },

    onSuccess: async (definition) => {
      await refreshProductTypeCharacteristics();

      onChange(definition.id);

      setSelectedAvailableDefinitionId("");
      setValidationError(null);
      setSuccessMessage(
        `Характеристика «${definition.name}» добавлена к типу «${productTypeName}» и назначена колонке Excel.`,
      );
      setMode("closed");
    },

    onError: () => {
      setSuccessMessage(null);
    },

    onSettled: () => {
      onBusyChange(columnId, false);
    },
  });

  const createMutation = useMutation({
    mutationFn: async (): Promise<CreatedCharacteristicResult> => {
      const createdDefinition = await createCharacteristicDefinition({
        code: newDefinitionCode.trim(),
        name: newDefinitionName.trim(),
        dataType: newDefinitionDataType,
        unit:
          newDefinitionUnit.trim().length > 0 ? newDefinitionUnit.trim() : null,
      });

      await addOptionalCharacteristicToProductType(productTypeCode, {
        characteristicDefinitionId: createdDefinition.id,
      });

      return {
        definitionId: createdDefinition.id,
        definitionName: newDefinitionName.trim(),
      };
    },

    onSuccess: async (result) => {
      await refreshProductTypeCharacteristics();

      onChange(result.definitionId);

      setValidationError(null);
      setSuccessMessage(
        `Характеристика «${result.definitionName}» создана, добавлена к типу «${productTypeName}» и назначена колонке Excel.`,
      );
      setMode("closed");
    },

    onError: () => {
      setSuccessMessage(null);
    },

    onSettled: () => {
      onBusyChange(columnId, false);
    },
  });

  const isMutationPending =
    attachMutation.isPending || createMutation.isPending;
  const isControlDisabled =
    disabled || isMutationPending || productTypeCode.length === 0;

  function resetMessages(): void {
    setValidationError(null);
    setSuccessMessage(null);
    attachMutation.reset();
    createMutation.reset();
  }

  function openAttachMode(): void {
    resetMessages();

    setMode("attach");
    setSearchDraft(sourceHeader);
    setAppliedSearch(sourceHeader);
    setSelectedAvailableDefinitionId("");
  }

  function openCreateMode(): void {
    resetMessages();

    setMode("create");
    setNewDefinitionCode(createCharacteristicCodeFromHeader(sourceHeader));
    setNewDefinitionName(sourceHeader);
    setNewDefinitionDataType("Text");
    setNewDefinitionUnit("");
  }

  function closeConfiguration(): void {
    if (isMutationPending) {
      return;
    }

    resetMessages();
    setMode("closed");
    setSelectedAvailableDefinitionId("");
  }

  function handleSearch(): void {
    resetMessages();

    setAppliedSearch(searchDraft.trim());
    setSelectedAvailableDefinitionId("");
  }

  function handleAttach(): void {
    resetMessages();

    if (!selectedAvailableDefinition) {
      setValidationError("Выберите существующее определение характеристики.");

      return;
    }

    onBusyChange(columnId, true);
    attachMutation.mutate(selectedAvailableDefinition);
  }

  function handleCreate(): void {
    resetMessages();

    const normalizedCode = newDefinitionCode.trim();
    const normalizedName = newDefinitionName.trim();

    if (normalizedCode.length === 0) {
      setValidationError("Введите код новой характеристики.");

      return;
    }

    if (normalizedName.length === 0) {
      setValidationError("Введите название новой характеристики.");

      return;
    }

    onBusyChange(columnId, true);
    createMutation.mutate();
  }

  return (
    <div className="grid gap-3">
      <AppSelect
        ariaLabel={`Характеристика колонки ${sourceHeader}`}
        value={value}
        disabled={isControlDisabled}
        onChange={(characteristicDefinitionId) => {
          resetMessages();
          onChange(characteristicDefinitionId);
        }}
        options={[
          {
            value: "",
            label: "Выберите характеристику",
          },
          ...characteristics.map((characteristic) => ({
            value: characteristic.id,
            label: getCharacteristicLabel(characteristic),
            disabled:
              characteristic.id !== value &&
              usedCharacteristicIds.has(characteristic.id),
          })),
        ]}
      />

      {!disabled && productTypeCode.length > 0 && value.length === 0 && (
        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            disabled={isMutationPending}
            onClick={openAttachMode}
            className="rounded-xl border border-teal-500/30 bg-teal-500/10 px-3 py-2 text-xs font-medium text-teal-200 transition hover:bg-teal-500/20 disabled:cursor-not-allowed disabled:opacity-50"
          >
            Добавить из глобальных
          </button>

          <button
            type="button"
            disabled={isMutationPending}
            onClick={openCreateMode}
            className="rounded-xl border border-white/10 bg-white/[0.05] px-3 py-2 text-xs font-medium text-slate-200 transition hover:bg-white/[0.1] disabled:cursor-not-allowed disabled:opacity-50"
          >
            Создать новую
          </button>
        </div>
      )}

      {mode === "attach" && (
        <div className="rounded-2xl border border-teal-500/20 bg-teal-500/[0.05] p-4">
          <div className="flex items-start justify-between gap-3">
            <div>
              <h4 className="text-sm font-semibold text-teal-100">
                Добавить характеристику из глобального каталога
              </h4>

              <p className="mt-1 text-xs leading-5 text-slate-400">
                Здесь отображаются только глобальные определения, которые ещё не
                подключены к выбранному типу товара. Выбранное определение будет
                добавлено к типу как необязательное и сразу назначено этой
                колонке Excel.
              </p>
            </div>

            <button
              type="button"
              disabled={isMutationPending}
              onClick={closeConfiguration}
              className="shrink-0 rounded-lg border border-white/10 px-2 py-1 text-xs text-slate-400 transition hover:bg-white/[0.06] hover:text-white disabled:opacity-50"
            >
              Закрыть
            </button>
          </div>

          <div className="mt-4 grid gap-3">
            <div className="flex flex-col gap-2 sm:flex-row">
              <input
                value={searchDraft}
                disabled={isMutationPending}
                onChange={(event) => {
                  setSearchDraft(event.target.value);
                  setValidationError(null);
                  setSuccessMessage(null);
                }}
                placeholder="Название или код характеристики"
                className="min-w-0 flex-1 rounded-xl border border-white/10 bg-black/30 px-3 py-2 text-sm text-slate-100 outline-none placeholder:text-slate-600 focus:border-teal-400 disabled:opacity-50"
              />

              <button
                type="button"
                disabled={
                  availableDefinitionsQuery.isFetching || isMutationPending
                }
                onClick={handleSearch}
                className="rounded-xl border border-white/10 bg-white/[0.06] px-4 py-2 text-sm text-slate-200 transition hover:bg-white/[0.1] disabled:cursor-not-allowed disabled:opacity-50"
              >
                {availableDefinitionsQuery.isFetching ? "Ищем..." : "Найти"}
              </button>
            </div>

            {availableDefinitionsQuery.isLoading && (
              <p className="text-xs text-slate-400">
                Загружаем доступные определения...
              </p>
            )}

            {availableDefinitionsQuery.isError && (
              <div className="rounded-xl border border-red-500/30 bg-red-500/10 p-3 text-xs leading-5 text-red-200">
                {getApiErrorMessage(
                  availableDefinitionsQuery.error,
                  "Не удалось загрузить доступные определения характеристик.",
                )}
              </div>
            )}

            {!availableDefinitionsQuery.isLoading &&
              !availableDefinitionsQuery.isError &&
              availableDefinitions.length === 0 && (
                <div className="rounded-xl border border-white/10 bg-black/20 p-3 text-xs leading-5 text-slate-400">
                  Среди ещё не подключённых определений ничего не найдено. Уже
                  подключённые характеристики находятся в основном списке выше.
                  Также можно создать новую характеристику из заголовка Excel.
                </div>
              )}

            {availableDefinitions.length > 0 && (
              <>
                <AppSelect
                  ariaLabel={`Существующее определение для колонки ${sourceHeader}`}
                  value={selectedAvailableDefinitionId}
                  disabled={
                    availableDefinitionsQuery.isFetching || isMutationPending
                  }
                  onChange={(definitionId) => {
                    setSelectedAvailableDefinitionId(definitionId);
                    setValidationError(null);
                    setSuccessMessage(null);
                    attachMutation.reset();
                  }}
                  options={[
                    {
                      value: "",
                      label: "Выберите глобальное определение",
                    },
                    ...availableDefinitions.map((definition) => ({
                      value: definition.id,
                      label: getAvailableDefinitionLabel(definition),
                    })),
                  ]}
                />

                <button
                  type="button"
                  disabled={!selectedAvailableDefinition || isMutationPending}
                  onClick={handleAttach}
                  className="rounded-xl bg-teal-500 px-4 py-2 text-sm font-medium text-white transition hover:bg-teal-400 disabled:cursor-not-allowed disabled:opacity-50"
                >
                  {attachMutation.isPending
                    ? "Подключаем..."
                    : "Добавить к типу и назначить"}
                </button>
              </>
            )}
          </div>
        </div>
      )}

      {mode === "create" && (
        <div className="rounded-2xl border border-amber-500/20 bg-amber-500/[0.05] p-4">
          <div className="flex items-start justify-between gap-3">
            <div>
              <h4 className="text-sm font-semibold text-amber-100">
                Создать новую характеристику
              </h4>

              <p className="mt-1 text-xs leading-5 text-slate-400">
                Название и код предварительно сформированы из заголовка Excel.
                Перед созданием их можно изменить.
              </p>
            </div>

            <button
              type="button"
              disabled={isMutationPending}
              onClick={closeConfiguration}
              className="shrink-0 rounded-lg border border-white/10 px-2 py-1 text-xs text-slate-400 transition hover:bg-white/[0.06] hover:text-white disabled:opacity-50"
            >
              Закрыть
            </button>
          </div>

          <div className="mt-4 grid gap-3">
            <label className="grid gap-1.5">
              <span className="text-xs font-medium text-slate-300">
                Название
              </span>

              <input
                value={newDefinitionName}
                maxLength={200}
                disabled={isMutationPending}
                onChange={(event) => {
                  setNewDefinitionName(event.target.value);
                  setValidationError(null);
                  setSuccessMessage(null);
                }}
                className="rounded-xl border border-white/10 bg-black/30 px-3 py-2 text-sm text-slate-100 outline-none focus:border-teal-400 disabled:opacity-50"
              />
            </label>

            <label className="grid gap-1.5">
              <span className="text-xs font-medium text-slate-300">Код</span>

              <input
                value={newDefinitionCode}
                maxLength={100}
                disabled={isMutationPending}
                onChange={(event) => {
                  setNewDefinitionCode(event.target.value);
                  setValidationError(null);
                  setSuccessMessage(null);
                }}
                className="rounded-xl border border-white/10 bg-black/30 px-3 py-2 font-mono text-sm text-slate-100 outline-none focus:border-teal-400 disabled:opacity-50"
              />

              <span className="text-xs leading-5 text-slate-500">
                Код создаётся латиницей и используется как стабильный
                технический идентификатор.
              </span>
            </label>

            <div className="grid gap-1.5">
              <span className="text-xs font-medium text-slate-300">
                Тип данных
              </span>

              <AppSelect
                ariaLabel={`Тип данных новой характеристики ${sourceHeader}`}
                value={newDefinitionDataType}
                disabled={isMutationPending}
                onChange={(dataType) => {
                  setNewDefinitionDataType(
                    dataType as CatalogCharacteristicDataType,
                  );
                  setValidationError(null);
                  setSuccessMessage(null);
                }}
                options={[
                  {
                    value: "Text",
                    label: "Текст",
                  },
                  {
                    value: "Number",
                    label: "Число",
                  },
                  {
                    value: "Boolean",
                    label: "Да / Нет",
                  },
                ]}
              />
            </div>

            <label className="grid gap-1.5">
              <span className="text-xs font-medium text-slate-300">
                Единица измерения
              </span>

              <input
                value={newDefinitionUnit}
                maxLength={50}
                disabled={isMutationPending}
                onChange={(event) => {
                  setNewDefinitionUnit(event.target.value);
                  setValidationError(null);
                  setSuccessMessage(null);
                }}
                placeholder="A, V, мм или пусто"
                className="rounded-xl border border-white/10 bg-black/30 px-3 py-2 text-sm text-slate-100 outline-none placeholder:text-slate-600 focus:border-teal-400 disabled:opacity-50"
              />
            </label>

            <button
              type="button"
              disabled={isMutationPending}
              onClick={handleCreate}
              className="rounded-xl bg-amber-500 px-4 py-2 text-sm font-medium text-slate-950 transition hover:bg-amber-400 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {createMutation.isPending
                ? "Создаём и подключаем..."
                : "Создать, добавить к типу и назначить"}
            </button>
          </div>
        </div>
      )}

      {validationError && (
        <div className="rounded-xl border border-amber-500/30 bg-amber-500/10 p-3 text-xs leading-5 text-amber-200">
          {validationError}
        </div>
      )}

      {attachMutation.isError && (
        <div className="rounded-xl border border-red-500/30 bg-red-500/10 p-3 text-xs leading-5 text-red-200">
          {getApiErrorMessage(
            attachMutation.error,
            "Не удалось подключить характеристику к типу товара.",
          )}
        </div>
      )}

      {createMutation.isError && (
        <div className="rounded-xl border border-red-500/30 bg-red-500/10 p-3 text-xs leading-5 text-red-200">
          {getApiErrorMessage(
            createMutation.error,
            "Не удалось создать и подключить характеристику.",
          )}
        </div>
      )}

      {successMessage && (
        <div className="rounded-xl border border-green-500/30 bg-green-500/10 p-3 text-xs leading-5 text-green-200">
          {successMessage}
        </div>
      )}
    </div>
  );
}
