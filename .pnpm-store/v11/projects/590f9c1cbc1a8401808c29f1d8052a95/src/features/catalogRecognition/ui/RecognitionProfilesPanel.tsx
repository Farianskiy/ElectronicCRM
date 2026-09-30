import type {
  CatalogCharacteristicRecognitionProfile,
  CatalogProductNameRecognitionPreview,
  CatalogRecognizedCharacteristic,
} from "../model/types";

interface RecognitionProfilesPanelProps {
  preview: CatalogProductNameRecognitionPreview;
}

interface ParsedProfileConfiguration {
  units: string[] | null;
  minimum: number | null;
  maximum: number | null;
  allowDecimal: boolean | null;
  trueAliases: string[] | null;
  falseAliases: string[] | null;
  formattedJson: string;
  parseError: string | null;
}

interface ProfileUsage {
  allCandidates: CatalogRecognizedCharacteristic[];
  acceptedCandidates: CatalogRecognizedCharacteristic[];
  conflictCandidates: CatalogRecognizedCharacteristic[];
}

export function RecognitionProfilesPanel({
  preview,
}: RecognitionProfilesPanelProps) {
  if (!preview.hasProductTypeScope) {
    return (
      <section className="rounded-3xl border border-amber-500/25 bg-amber-500/[0.06] p-6">
        <h2 className="text-xl font-semibold text-amber-100">
          Профили распознавания не используются
        </h2>

        <p className="mt-2 text-sm text-amber-200/80">
          Проверка запущена без типа товара. Recognition Engine работает с
          базовыми настройками стратегий и не загружает
          CatalogCharacteristicRecognitionProfile из PostgreSQL.
        </p>

        <p className="mt-4 text-sm text-slate-400">
          Чтобы проверить единицы, диапазоны и MinimumConfidence конкретного
          типа, выберите тип товара и повторно запустите распознавание.
        </p>
      </section>
    );
  }

  const activeProfilesCount = preview.recognitionProfiles.filter(
    (profile) => profile.isActive,
  ).length;

  const inactiveProfilesCount =
    preview.recognitionProfiles.length - activeProfilesCount;

  return (
    <section className="grid gap-5">
      <div className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
        <div className="flex flex-col justify-between gap-5 lg:flex-row lg:items-start">
          <div>
            <h2 className="text-xl font-semibold text-white">
              Профили распознавания
            </h2>

            <p className="mt-2 max-w-3xl text-sm text-slate-400">
              Профиль определяет, как конкретная характеристика распознаётся
              внутри выбранного типа товара. Здесь отображаются фактические
              записи CatalogCharacteristicRecognitionProfile, загруженные из
              PostgreSQL.
            </p>
          </div>

          <div className="flex flex-wrap gap-2">
            <CountBadge
              label="Всего"
              value={preview.recognitionProfiles.length}
              className="border-blue-500/30 bg-blue-500/10 text-blue-200"
            />

            <CountBadge
              label="Активных"
              value={activeProfilesCount}
              className="border-teal-500/30 bg-teal-500/10 text-teal-200"
            />

            {inactiveProfilesCount > 0 && (
              <CountBadge
                label="Отключённых"
                value={inactiveProfilesCount}
                className="border-slate-500/30 bg-slate-500/10 text-slate-300"
              />
            )}
          </div>
        </div>

        <div className="mt-5 rounded-2xl border border-blue-500/20 bg-blue-500/[0.05] p-4 text-sm text-blue-100/80">
          Профиль считается использованным в этом запуске, если его
          идентификатор присутствует в RecognizerKey кандидата в формате
          <span className="mx-1 font-mono text-blue-200">
            :profile:&#123;ProfileId&#125;
          </span>
          .
        </div>
      </div>

      {preview.recognitionProfiles.length === 0 ? (
        <section className="rounded-3xl border border-amber-500/25 bg-amber-500/[0.06] p-6">
          <h3 className="text-lg font-semibold text-amber-100">
            Для типа товара нет профилей
          </h3>

          <p className="mt-2 text-sm text-amber-200/80">
            Тип товара выбран, но для него не найдены записи
            CatalogCharacteristicRecognitionProfile.
          </p>

          <p className="mt-4 text-sm text-slate-400">
            Характеристики всё ещё могут распознаваться базовыми настройками
            стратегий. Поэтому отсутствие профиля не означает, что результат
            обязательно будет пустым.
          </p>
        </section>
      ) : (
        <div className="grid gap-5">
          {preview.recognitionProfiles.map((profile) => (
            <RecognitionProfileCard
              key={profile.id}
              profile={profile}
              preview={preview}
            />
          ))}
        </div>
      )}
    </section>
  );
}

function RecognitionProfileCard({
  profile,
  preview,
}: {
  profile: CatalogCharacteristicRecognitionProfile;
  preview: CatalogProductNameRecognitionPreview;
}) {
  const configuration = parseProfileConfiguration(profile.configurationJson);

  const usage = getProfileUsage(profile, preview);
  const usageState = getProfileUsageState(profile, usage);

  return (
    <article
      className={
        profile.isActive
          ? "rounded-3xl border border-teal-500/20 bg-white/[0.04] p-6"
          : "rounded-3xl border border-slate-500/20 bg-slate-500/[0.04] p-6 opacity-80"
      }
    >
      <div className="flex flex-col justify-between gap-5 xl:flex-row xl:items-start">
        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-2">
            <h3 className="text-xl font-semibold text-white">
              {profile.characteristicName}
            </h3>

            <ProfileStatusBadge isActive={profile.isActive} />

            <ProfileUsageBadge
              label={usageState.label}
              className={usageState.className}
            />
          </div>

          <p className="mt-2 break-all font-mono text-sm text-teal-300">
            {profile.characteristicCode}
          </p>

          <p className="mt-2 text-sm text-slate-400">
            Стратегия:
            <span className="ml-2 font-mono text-slate-200">
              {profile.strategyKind}
            </span>
          </p>
        </div>

        <div className="grid min-w-64 grid-cols-2 gap-3">
          <ProfileMetric label="Priority" value={profile.priority.toString()} />

          <ProfileMetric
            label="MinimumConfidence"
            value={formatConfidence(profile.minimumConfidence)}
          />
        </div>
      </div>

      {profile.strategyKind === "NumericWithUnit" && (
        <>
          <div className="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            <ConfigurationValue
              label="Допустимые единицы"
              value={
                configuration.units && configuration.units.length > 0
                  ? configuration.units.join(", ")
                  : "Не указаны"
              }
              monospace
            />

            <ConfigurationValue
              label="Минимальное значение"
              value={formatOptionalNumber(configuration.minimum)}
            />

            <ConfigurationValue
              label="Максимальное значение"
              value={formatOptionalNumber(configuration.maximum)}
            />

            <ConfigurationValue
              label="Разрешены дробные числа"
              value={formatOptionalBoolean(configuration.allowDecimal)}
            />
          </div>

          {configuration.units && configuration.units.length > 0 && (
            <div className="mt-4">
              <p className="text-xs text-slate-500">
                Единицы из ConfigurationJson
              </p>

              <div className="mt-2 flex flex-wrap gap-2">
                {configuration.units.map((unit, index) => (
                  <span
                    key={`${unit}-${index}`}
                    className="rounded-full border border-violet-500/30 bg-violet-500/10 px-3 py-1 font-mono text-xs text-violet-200"
                  >
                    {unit}
                  </span>
                ))}
              </div>
            </div>
          )}
        </>
      )}

      {profile.strategyKind === "BooleanAlias" && (
        <div className="mt-6 grid gap-4 lg:grid-cols-2">
          <AliasConfigurationList
            title="Фразы, означающие наличие"
            propertyName="trueAliases"
            aliases={configuration.trueAliases}
            value="true"
          />

          <AliasConfigurationList
            title="Фразы, означающие отсутствие"
            propertyName="falseAliases"
            aliases={configuration.falseAliases}
            value="false"
          />
        </div>
      )}

      {configuration.units && configuration.units.length > 0 && (
        <div className="mt-4">
          <p className="text-xs text-slate-500">Единицы из ConfigurationJson</p>

          <div className="mt-2 flex flex-wrap gap-2">
            {configuration.units.map((unit, index) => (
              <span
                key={`${unit}-${index}`}
                className="rounded-full border border-violet-500/30 bg-violet-500/10 px-3 py-1 font-mono text-xs text-violet-200"
              >
                {unit}
              </span>
            ))}
          </div>
        </div>
      )}

      {configuration.parseError && (
        <div className="mt-4 rounded-2xl border border-red-500/30 bg-red-500/10 p-4">
          <p className="text-sm font-medium text-red-200">
            ConfigurationJson не удалось разобрать
          </p>

          <p className="mt-2 text-sm text-red-200/80">
            {configuration.parseError}
          </p>
        </div>
      )}

      <div className="mt-6 rounded-2xl border border-white/10 bg-black/20 p-5">
        <div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-start">
          <div>
            <p className="text-sm font-medium text-white">
              Использование в текущем запуске
            </p>

            <p className="mt-1 text-sm text-slate-400">
              Связь найдена через ProfileId внутри RecognizerKey.
            </p>
          </div>

          <div className="flex flex-wrap gap-2">
            <UsageCount
              label="Всего кандидатов"
              value={usage.allCandidates.length}
              className="border-blue-500/30 bg-blue-500/10 text-blue-200"
            />

            <UsageCount
              label="Принято"
              value={usage.acceptedCandidates.length}
              className="border-teal-500/30 bg-teal-500/10 text-teal-200"
            />

            <UsageCount
              label="В конфликте"
              value={usage.conflictCandidates.length}
              className="border-red-500/30 bg-red-500/10 text-red-200"
            />
          </div>
        </div>

        {usage.allCandidates.length === 0 ? (
          <p className="mt-4 rounded-2xl border border-white/10 bg-white/[0.03] p-4 text-sm text-slate-400">
            Профиль был доступен движку, но не создал кандидатов для текущего
            наименования.
          </p>
        ) : (
          <div className="mt-4 grid gap-3">
            {usage.allCandidates.map((candidate, index) => (
              <div
                key={`${candidate.recognizerKey}-${candidate.startIndex}-${index}`}
                className="rounded-2xl border border-white/10 bg-white/[0.03] p-4"
              >
                <div className="flex flex-col justify-between gap-3 sm:flex-row sm:items-center">
                  <div>
                    <p className="font-mono text-sm text-white">
                      {candidate.characteristicCode}
                    </p>

                    <p className="mt-1 text-sm text-slate-400">
                      {candidate.rawValue}
                      <span className="mx-2 text-slate-600">→</span>
                      <span className="text-slate-200">
                        {candidate.normalizedValue}
                      </span>
                    </p>
                  </div>

                  <div className="flex flex-wrap gap-2">
                    <span className="rounded-full border border-blue-500/30 bg-blue-500/10 px-3 py-1 text-xs text-blue-200">
                      Confidence {formatConfidence(candidate.confidence)}
                    </span>

                    <span className="rounded-full border border-white/10 bg-white/[0.04] px-3 py-1 font-mono text-xs text-slate-300">
                      Span {candidate.startIndex}..
                      {candidate.startIndex + candidate.length}
                    </span>
                  </div>
                </div>

                <p className="mt-3 break-all font-mono text-xs text-slate-500">
                  {candidate.recognizerKey}
                </p>
              </div>
            ))}
          </div>
        )}
      </div>

      <details className="mt-5 rounded-2xl border border-white/10 bg-black/20">
        <summary className="cursor-pointer px-5 py-4 text-sm font-medium text-slate-200">
          Показать полный ConfigurationJson
        </summary>

        <div className="border-t border-white/10 p-5">
          <pre className="overflow-x-auto whitespace-pre-wrap break-words font-mono text-xs leading-6 text-slate-300">
            {configuration.formattedJson}
          </pre>
        </div>
      </details>

      <div className="mt-5 grid gap-3 md:grid-cols-2 xl:grid-cols-4">
        <ProfileIdentifier label="ProfileId" value={profile.id} />

        <ProfileIdentifier
          label="ProductTypeId"
          value={profile.productTypeId}
        />

        <ProfileIdentifier
          label="CharacteristicDefinitionId"
          value={profile.characteristicDefinitionId}
        />

        <ProfileIdentifier
          label="Создан / изменён"
          value={`${formatDate(profile.createdAtUtc)} / ${formatDate(
            profile.updatedAtUtc,
          )}`}
        />
      </div>
    </article>
  );
}

function parseProfileConfiguration(
  configurationJson: string,
): ParsedProfileConfiguration {
  try {
    const parsedConfiguration: unknown = JSON.parse(configurationJson);

    if (
      !parsedConfiguration ||
      typeof parsedConfiguration !== "object" ||
      Array.isArray(parsedConfiguration)
    ) {
      return {
        units: null,
        minimum: null,
        maximum: null,
        allowDecimal: null,
        trueAliases: null,
        falseAliases: null,
        formattedJson: configurationJson,
        parseError: "Корневое значение JSON должно быть объектом.",
      };
    }

    const configuration = parsedConfiguration as Record<string, unknown>;

    const units = getStringArray(configuration.units);

    const minimum =
      typeof configuration.minimum === "number" ? configuration.minimum : null;

    const maximum =
      typeof configuration.maximum === "number" ? configuration.maximum : null;

    const allowDecimal =
      typeof configuration.allowDecimal === "boolean"
        ? configuration.allowDecimal
        : null;

    const trueAliases = getStringArray(configuration.trueAliases);
    const falseAliases = getStringArray(configuration.falseAliases);

    return {
      units,
      minimum,
      maximum,
      allowDecimal,
      trueAliases,
      falseAliases,
      formattedJson: JSON.stringify(parsedConfiguration, null, 2),
      parseError: null,
    };
  } catch {
    return {
      units: null,
      minimum: null,
      maximum: null,
      allowDecimal: null,
      trueAliases: null,
      falseAliases: null,
      formattedJson: configurationJson,
      parseError: "Строка не является корректным JSON.",
    };
  }
}

function getStringArray(value: unknown): string[] | null {
  if (!Array.isArray(value)) {
    return null;
  }

  return value.filter((item): item is string => typeof item === "string");
}

function getProfileUsage(
  profile: CatalogCharacteristicRecognitionProfile,
  preview: CatalogProductNameRecognitionPreview,
): ProfileUsage {
  const allCandidates = preview.candidates.filter((candidate) =>
    candidateUsesProfile(candidate, profile.id),
  );

  const acceptedCandidates = preview.characteristics.filter((candidate) =>
    candidateUsesProfile(candidate, profile.id),
  );

  const conflictCandidates = preview.conflicts.flatMap((conflict) =>
    conflict.candidates.filter((candidate) =>
      candidateUsesProfile(candidate, profile.id),
    ),
  );

  return {
    allCandidates,
    acceptedCandidates,
    conflictCandidates,
  };
}

function candidateUsesProfile(
  candidate: CatalogRecognizedCharacteristic,
  profileId: string,
): boolean {
  const profileMarker = `:profile:${profileId}`.toLowerCase();

  return candidate.recognizerKey.toLowerCase().includes(profileMarker);
}

function getProfileUsageState(
  profile: CatalogCharacteristicRecognitionProfile,
  usage: ProfileUsage,
): {
  label: string;
  className: string;
} {
  if (!profile.isActive) {
    return {
      label: "Отключён",
      className: "border-slate-500/30 bg-slate-500/10 text-slate-300",
    };
  }

  if (usage.acceptedCandidates.length > 0) {
    return {
      label: "Использован в результате",
      className: "border-teal-500/30 bg-teal-500/10 text-teal-200",
    };
  }

  if (usage.conflictCandidates.length > 0) {
    return {
      label: "Создал конфликт",
      className: "border-red-500/30 bg-red-500/10 text-red-200",
    };
  }

  if (usage.allCandidates.length > 0) {
    return {
      label: "Создал кандидата",
      className: "border-blue-500/30 bg-blue-500/10 text-blue-200",
    };
  }

  return {
    label: "Не сработал",
    className: "border-amber-500/30 bg-amber-500/10 text-amber-200",
  };
}

function formatConfidence(confidence: number): string {
  return `${(confidence * 100).toFixed(1)}%`;
}

function formatOptionalNumber(value: number | null): string {
  if (value === null) {
    return "Не указано";
  }

  return new Intl.NumberFormat("ru-RU", {
    maximumFractionDigits: 6,
  }).format(value);
}

function formatOptionalBoolean(value: boolean | null): string {
  if (value === null) {
    return "Не указано";
  }

  return value ? "Да" : "Нет";
}

function formatDate(value: string): string {
  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return new Intl.DateTimeFormat("ru-RU", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(date);
}

function AliasConfigurationList({
  title,
  propertyName,
  aliases,
  value,
}: {
  title: string;
  propertyName: string;
  aliases: string[] | null;
  value: "true" | "false";
}) {
  const isTrueValue = value === "true";

  return (
    <section
      className={
        isTrueValue
          ? "rounded-2xl border border-green-500/25 bg-green-500/[0.06] p-5"
          : "rounded-2xl border border-red-500/25 bg-red-500/[0.06] p-5"
      }
    >
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <p
            className={
              isTrueValue
                ? "font-medium text-green-200"
                : "font-medium text-red-200"
            }
          >
            {title}
          </p>

          <p className="mt-1 font-mono text-xs text-slate-500">
            {propertyName}
          </p>
        </div>

        <span
          className={
            isTrueValue
              ? "rounded-full border border-green-500/30 bg-green-500/10 px-3 py-1 font-mono text-xs text-green-200"
              : "rounded-full border border-red-500/30 bg-red-500/10 px-3 py-1 font-mono text-xs text-red-200"
          }
        >
          Результат: {value}
        </span>
      </div>

      {aliases && aliases.length > 0 ? (
        <div className="mt-4 flex flex-wrap gap-2">
          {aliases.map((alias, index) => (
            <span
              key={`${propertyName}-${alias}-${index}`}
              className={
                isTrueValue
                  ? "rounded-full border border-green-500/30 bg-green-500/10 px-3 py-1 font-mono text-xs text-green-100"
                  : "rounded-full border border-red-500/30 bg-red-500/10 px-3 py-1 font-mono text-xs text-red-100"
              }
            >
              {alias}
            </span>
          ))}
        </div>
      ) : (
        <p className="mt-4 text-sm text-amber-200">
          В ConfigurationJson не указаны фразы.
        </p>
      )}
    </section>
  );
}

function ProfileStatusBadge({ isActive }: { isActive: boolean }) {
  return (
    <span
      className={
        isActive
          ? "rounded-full border border-green-500/30 bg-green-500/10 px-3 py-1 text-xs font-medium text-green-300"
          : "rounded-full border border-slate-500/30 bg-slate-500/10 px-3 py-1 text-xs font-medium text-slate-300"
      }
    >
      {isActive ? "Активен" : "Отключён"}
    </span>
  );
}

function ProfileUsageBadge({
  label,
  className,
}: {
  label: string;
  className: string;
}) {
  return (
    <span
      className={`rounded-full border px-3 py-1 text-xs font-medium ${className}`}
    >
      {label}
    </span>
  );
}

function CountBadge({
  label,
  value,
  className,
}: {
  label: string;
  value: number;
  className: string;
}) {
  return (
    <span
      className={`rounded-full border px-3 py-1 text-xs font-medium ${className}`}
    >
      {label}: {value}
    </span>
  );
}

function UsageCount({
  label,
  value,
  className,
}: {
  label: string;
  value: number;
  className: string;
}) {
  return (
    <span className={`rounded-full border px-3 py-1 text-xs ${className}`}>
      {label}: {value}
    </span>
  );
}

function ProfileMetric({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
      <p className="text-xs text-slate-500">{label}</p>

      <p className="mt-2 text-lg font-semibold text-white">{value}</p>
    </div>
  );
}

function ConfigurationValue({
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
        className={`mt-2 break-words text-sm text-slate-200 ${
          monospace ? "font-mono" : ""
        }`}
      >
        {value}
      </p>
    </div>
  );
}

function ProfileIdentifier({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
      <p className="text-xs text-slate-500">{label}</p>

      <p className="mt-2 break-all font-mono text-xs text-slate-300">{value}</p>
    </div>
  );
}
