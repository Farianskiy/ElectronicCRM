import type { CatalogRecognitionStrategyKind } from "./types";

export interface RecognitionStrategyDescriptor {
  strategyKind: CatalogRecognitionStrategyKind;
  description: string;
  defaultConfigurationJson: string;
}

const maximumBooleanAliasCountPerValue = 64;
const maximumBooleanAliasLength = 120;

const strategyDescriptorsByCharacteristicCode: Readonly<
  Record<string, RecognitionStrategyDescriptor>
> = {
  RATED_CURRENT: {
    strategyKind: "NumericWithUnit",
    description: "Числовое значение с единицей измерения номинального тока.",
    defaultConfigurationJson: JSON.stringify(
      {
        units: ["А", "A"],
        minimum: 0.1,
        maximum: 6300,
        allowDecimal: true,
      },
      null,
      2,
    ),
  },

  LEAKAGE_CURRENT: {
    strategyKind: "NumericWithUnit",
    description: "Числовое значение тока утечки с единицей измерения.",
    defaultConfigurationJson: JSON.stringify(
      {
        units: ["мА", "mA", "мA", "mА"],
        minimum: 1,
        maximum: 1000,
        allowDecimal: false,
      },
      null,
      2,
    ),
  },

  BREAKING_CAPACITY: {
    strategyKind: "NumericWithUnit",
    description: "Числовое значение коммутационной способности.",
    defaultConfigurationJson: JSON.stringify(
      {
        units: ["кА", "kA", "кA", "kА"],
        minimum: 1,
        maximum: 150,
        allowDecimal: true,
      },
      null,
      2,
    ),
  },

  HAS_THERMAL_RELEASE: {
    strategyKind: "BooleanAlias",
    description:
      "Логический признак наличия или отсутствия теплового расцепителя.",
    defaultConfigurationJson: JSON.stringify(
      {
        trueAliases: [
          "с ТР",
          "с тепловым расцепителем",
          "тепловой расцепитель",
        ],
        falseAliases: [
          "без ТР",
          "нет ТР",
          "без теплового расцепителя",
        ],
      },
      null,
      2,
    ),
  },

  POLE_COUNT: {
    strategyKind: "PoleCount",
    description: "Количество полюсов, например 1п, 2P, 3Р или 4P.",
    defaultConfigurationJson: JSON.stringify({}, null, 2),
  },

  TRIP_CURVE: {
    strategyKind: "EnumToken",
    description: "Перечислимый токен характеристики срабатывания.",
    defaultConfigurationJson: JSON.stringify({}, null, 2),
  },

  IP_RATING: {
    strategyKind: "EnumToken",
    description: "Перечислимый токен степени защиты IP.",
    defaultConfigurationJson: JSON.stringify({}, null, 2),
  },
};

export function getRecognitionStrategyDescriptor(
  characteristicCode: string,
): RecognitionStrategyDescriptor | null {
  const normalizedCode = characteristicCode.trim().toUpperCase();

  return strategyDescriptorsByCharacteristicCode[normalizedCode] ?? null;
}

export function formatRecognitionProfileConfiguration(
  configurationJson: string,
): string {
  try {
    const parsedConfiguration: unknown = JSON.parse(configurationJson);

    return JSON.stringify(parsedConfiguration, null, 2);
  } catch {
    return configurationJson;
  }
}

export function validateRecognitionProfileForm(
  strategyKind: CatalogRecognitionStrategyKind,
  priorityText: string,
  minimumConfidenceText: string,
  configurationJson: string,
): string | null {
  const priority = Number(priorityText);

  if (!Number.isInteger(priority) || priority < 1 || priority > 10000) {
    return "Priority должен быть целым числом от 1 до 10000.";
  }

  const minimumConfidence = Number(minimumConfidenceText);

  if (
    !Number.isFinite(minimumConfidence) ||
    minimumConfidence <= 0 ||
    minimumConfidence > 1
  ) {
    return "MinimumConfidence должен быть числом больше 0 и не больше 1.";
  }

  let parsedConfiguration: unknown;

  try {
    parsedConfiguration = JSON.parse(configurationJson);
  } catch {
    return "ConfigurationJson содержит некорректный JSON.";
  }

  if (
    !parsedConfiguration ||
    typeof parsedConfiguration !== "object" ||
    Array.isArray(parsedConfiguration)
  ) {
    return "Корневым значением ConfigurationJson должен быть JSON-объект.";
  }

  const configuration = parsedConfiguration as Record<string, unknown>;

  if (strategyKind === "NumericWithUnit") {
    return validateNumericWithUnitConfiguration(configuration);
  }

  if (strategyKind === "BooleanAlias") {
    return validateBooleanAliasConfiguration(configuration);
  }

  return null;
}

function validateNumericWithUnitConfiguration(
  configuration: Record<string, unknown>,
): string | null {
  if (
    !Array.isArray(configuration.units) ||
    configuration.units.length === 0 ||
    configuration.units.some(
      (unit) => typeof unit !== "string" || unit.trim().length === 0,
    )
  ) {
    return "Для NumericWithUnit поле units должно содержать непустой массив строк.";
  }

  if (typeof configuration.minimum !== "number") {
    return "Для NumericWithUnit поле minimum должно быть числом.";
  }

  if (typeof configuration.maximum !== "number") {
    return "Для NumericWithUnit поле maximum должно быть числом.";
  }

  if (configuration.minimum > configuration.maximum) {
    return "Minimum не может быть больше Maximum.";
  }

  if (typeof configuration.allowDecimal !== "boolean") {
    return "Для NumericWithUnit поле allowDecimal должно быть true или false.";
  }

  return null;
}

function validateBooleanAliasConfiguration(
  configuration: Record<string, unknown>,
): string | null {
  const trueAliasesError = validateBooleanAliasArray(
    configuration.trueAliases,
    "trueAliases",
  );

  if (trueAliasesError) {
    return trueAliasesError;
  }

  const falseAliasesError = validateBooleanAliasArray(
    configuration.falseAliases,
    "falseAliases",
  );

  if (falseAliasesError) {
    return falseAliasesError;
  }

  const trueAliases = configuration.trueAliases as string[];
  const falseAliases = configuration.falseAliases as string[];

  const normalizedTrueAliases = trueAliases.map(normalizeAliasForComparison);

  const normalizedFalseAliases = new Set(
    falseAliases.map(normalizeAliasForComparison),
  );

  const aliasUsedForBothValues = normalizedTrueAliases.find((alias) =>
    normalizedFalseAliases.has(alias),
  );

  if (aliasUsedForBothValues) {
    return `Фраза '${aliasUsedForBothValues}' не может одновременно присутствовать в trueAliases и falseAliases.`;
  }

  return null;
}

function validateBooleanAliasArray(
  value: unknown,
  propertyName: string,
): string | null {
  if (!Array.isArray(value) || value.length === 0) {
    return `Для BooleanAlias поле ${propertyName} должно содержать непустой массив строк.`;
  }

  if (value.length > maximumBooleanAliasCountPerValue) {
    return `Поле ${propertyName} не может содержать больше ${maximumBooleanAliasCountPerValue} фраз.`;
  }

  if (
    value.some(
      (alias) => typeof alias !== "string" || alias.trim().length === 0,
    )
  ) {
    return `Поле ${propertyName} должно содержать только непустые строки.`;
  }

  const aliases = value as string[];

  const aliasWithInvalidLength = aliases.find(
    (alias) => alias.trim().length > maximumBooleanAliasLength,
  );

  if (aliasWithInvalidLength) {
    return `Одна фраза в ${propertyName} не может быть длиннее ${maximumBooleanAliasLength} символов.`;
  }

  const normalizedAliases = aliases.map(normalizeAliasForComparison);

  if (new Set(normalizedAliases).size !== normalizedAliases.length) {
    return `Поле ${propertyName} содержит повторяющиеся фразы.`;
  }

  return null;
}

function normalizeAliasForComparison(value: string): string {
  return value.trim().replace(/\s+/g, " ").toUpperCase();
}