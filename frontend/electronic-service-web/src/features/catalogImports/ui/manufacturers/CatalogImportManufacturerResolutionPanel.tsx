import type {
  AnalyzeCatalogImportBatchResponse,
  CatalogImportManufacturerResolutionGroup,
  CatalogImportManufacturerResolutionSource,
  CatalogImportManufacturerResolutionSummary,
} from "../../model/types";
import { CatalogImportManufacturerAliasCreationForm } from "./CatalogImportManufacturerAliasCreationForm";
import { CatalogImportManufacturerCreationForm } from "./CatalogImportManufacturerCreationForm";
import { CatalogImportManufacturerNoiseForm } from "./CatalogImportManufacturerNoiseForm";

interface CatalogImportManufacturerResolutionPanelProps {
  batchId: string;
  productTypeId?: string | null;
  summary: CatalogImportManufacturerResolutionSummary | null;
  onAnalysisChange: (analysis: AnalyzeCatalogImportBatchResponse) => void;
}

interface ManufacturerSummaryCardProps {
  label: string;
  value: number;
  description: string;
  tone?: "Neutral" | "Success" | "Warning" | "Danger";
}

interface ManufacturerResolutionPresentation {
  label: string;
  explanation: string;
  badgeClassName: string;
  cardClassName: string;
}

function getSummaryCardClassName(
  tone: ManufacturerSummaryCardProps["tone"],
): string {
  switch (tone) {
    case "Success":
      return "border-green-500/25 bg-green-500/[0.06]";

    case "Warning":
      return "border-amber-500/25 bg-amber-500/[0.06]";

    case "Danger":
      return "border-red-500/25 bg-red-500/[0.06]";

    case "Neutral":
    default:
      return "border-white/10 bg-black/20";
  }
}

function getResolutionPresentation(
  source: CatalogImportManufacturerResolutionSource,
): ManufacturerResolutionPresentation {
  switch (source) {
    case "ExactName":
      return {
        label: "Точное имя",
        explanation:
          "Исходное значение совпало с именем Manufacturer после нормализации.",
        badgeClassName: "border-green-500/30 bg-green-500/10 text-green-200",
        cardClassName: "border-green-500/20 bg-green-500/[0.03]",
      };

    case "ApprovedAlias":
      return {
        label: "Подтверждённый alias",
        explanation:
          "Исходное значение найдено среди Approved ManufacturerAlias и приведено к каноническому производителю.",
        badgeClassName: "border-violet-500/30 bg-violet-500/10 text-violet-200",
        cardClassName: "border-violet-500/20 bg-violet-500/[0.03]",
      };

    case "IgnoredNoise":
      return {
        label: "Подтверждённый шум",
        explanation:
          "Technical-пользователь подтвердил, что исходное значение не является названием производителя.",
        badgeClassName: "border-orange-500/30 bg-orange-500/10 text-orange-200",
        cardClassName: "border-orange-500/20 bg-orange-500/[0.03]",
      };

    case "None":
    default:
      return {
        label: "Не разрешено",
        explanation:
          "Такого точного имени или подтверждённого alias пока нет в справочнике.",
        badgeClassName: "border-red-500/30 bg-red-500/10 text-red-200",
        cardClassName: "border-red-500/20 bg-red-500/[0.03]",
      };
  }
}

function ManufacturerSummaryCard({
  label,
  value,
  description,
  tone = "Neutral",
}: ManufacturerSummaryCardProps) {
  return (
    <div
      className={["rounded-2xl border p-4", getSummaryCardClassName(tone)].join(
        " ",
      )}
    >
      <p className="text-sm text-slate-400">{label}</p>

      <p className="mt-2 text-2xl font-semibold text-white">{value}</p>

      <p className="mt-2 text-xs leading-5 text-slate-500">{description}</p>
    </div>
  );
}

function ManufacturerResolutionGroupCard({
  batchId,
  productTypeId,
  group,
  onAnalysisChange,
}: {
  batchId: string;
  productTypeId?: string | null;
  group: CatalogImportManufacturerResolutionGroup;
  onAnalysisChange: (analysis: AnalyzeCatalogImportBatchResponse) => void;
}) {
  const presentation = getResolutionPresentation(group.source);
  const isUnresolved = group.status === "Unresolved";
  const isIgnoredNoise = group.status === "IgnoredNoise";

  const resolutionTarget = isIgnoredNoise
    ? "Не является производителем"
    : (group.resolvedManufacturerName ?? "Производитель не определён");

  return (
    <article
      className={["rounded-2xl border p-5", presentation.cardClassName].join(
        " ",
      )}
    >
      <div className="flex flex-col justify-between gap-4 lg:flex-row lg:items-start">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <span
              className={[
                "rounded-full border px-3 py-1 text-xs font-medium",
                presentation.badgeClassName,
              ].join(" ")}
            >
              {presentation.label}
            </span>

            <span className="rounded-full border border-white/10 bg-white/[0.04] px-3 py-1 text-xs text-slate-300">
              Строк: {group.occurrenceCount}
            </span>
          </div>

          <div className="mt-4 flex flex-wrap items-center gap-3">
            <p className="break-words font-mono text-lg font-semibold text-white">
              {group.sourceValue}
            </p>

            <span className="text-slate-600">→</span>

            <p
              className={[
                "break-words font-mono text-lg font-semibold",
                isUnresolved ? "text-red-300" : "text-teal-300",
              ].join(" ")}
            >
              {resolutionTarget}
            </p>
          </div>

          <p className="mt-3 max-w-4xl text-sm leading-6 text-slate-400">
            {presentation.explanation}
          </p>
        </div>

        <div className="rounded-xl border border-white/10 bg-black/20 px-4 py-3 lg:text-right">
          <p className="text-xs text-slate-500">Количество строк</p>

          <p className="mt-1 text-2xl font-semibold text-white">
            {group.occurrenceCount}
          </p>
        </div>
      </div>

      <dl className="mt-5 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <div className="rounded-xl border border-white/10 bg-black/20 p-4">
          <dt className="text-xs text-slate-500">Исходное значение Excel</dt>

          <dd className="mt-2 break-words font-mono text-sm text-white">
            {group.sourceValue}
          </dd>
        </div>

        <div className="rounded-xl border border-white/10 bg-black/20 p-4">
          <dt className="text-xs text-slate-500">Нормализованное значение</dt>

          <dd className="mt-2 break-words font-mono text-sm text-slate-200">
            {group.normalizedSourceValue}
          </dd>
        </div>

        <div className="rounded-xl border border-white/10 bg-black/20 p-4">
          <dt className="text-xs text-slate-500">Итоговый Manufacturer</dt>

          <dd
            className={[
              "mt-2 break-words text-sm font-medium",
              isUnresolved ? "text-red-300" : "text-green-300",
            ].join(" ")}
          >
            {isIgnoredNoise
              ? "Не требуется: значение признано шумом"
              : (group.resolvedManufacturerName ?? "Не найден")}
          </dd>
        </div>

        <div className="rounded-xl border border-white/10 bg-black/20 p-4">
          <dt className="text-xs text-slate-500">Источник решения</dt>

          <dd className="mt-2 font-mono text-sm text-slate-200">
            {group.source}
          </dd>
        </div>
      </dl>

      <dl className="mt-4 grid gap-4 lg:grid-cols-2">
        <div className="rounded-xl border border-white/10 bg-black/20 p-4">
          <dt className="text-xs text-slate-500">ManufacturerId</dt>

          <dd className="mt-2 break-all font-mono text-xs text-slate-300">
            {group.manufacturerId ?? "Отсутствует"}
          </dd>
        </div>

        <div className="rounded-xl border border-white/10 bg-black/20 p-4">
          <dt className="text-xs text-slate-500">ManufacturerAliasId</dt>

          <dd className="mt-2 break-all font-mono text-xs text-slate-300">
            {group.manufacturerAliasId ?? "Alias не использовался"}
          </dd>
        </div>
      </dl>

      {isIgnoredNoise && (
        <dl className="mt-4 grid gap-4 lg:grid-cols-2">
          <div className="rounded-xl border border-orange-500/20 bg-orange-500/[0.04] p-4">
            <dt className="text-xs text-orange-300/70">
              ManufacturerNoisePhraseId
            </dt>

            <dd className="mt-2 break-all font-mono text-xs text-orange-100">
              {group.manufacturerNoisePhraseId ?? "Идентификатор отсутствует"}
            </dd>
          </div>

          <div className="rounded-xl border border-orange-500/20 bg-orange-500/[0.04] p-4">
            <dt className="text-xs text-orange-300/70">
              Причина решения Technical
            </dt>

            <dd className="mt-2 text-sm leading-6 text-orange-100">
              {group.noiseReason ?? "Причина не указана"}
            </dd>
          </div>
        </dl>
      )}

      <div className="mt-4 grid gap-4 xl:grid-cols-2">
        <div className="rounded-xl border border-white/10 bg-black/20 p-4">
          <p className="text-xs text-slate-500">Примеры строк Excel</p>

          {group.exampleRowNumbers.length === 0 ? (
            <p className="mt-2 text-sm text-slate-500">
              Номера строк отсутствуют.
            </p>
          ) : (
            <div className="mt-3 flex flex-wrap gap-2">
              {group.exampleRowNumbers.map((rowNumber) => (
                <span
                  key={rowNumber}
                  className="rounded-lg border border-white/10 bg-white/[0.04] px-3 py-1 font-mono text-xs text-slate-300"
                >
                  Строка {rowNumber}
                </span>
              ))}
            </div>
          )}
        </div>

        <div className="rounded-xl border border-white/10 bg-black/20 p-4">
          <p className="text-xs text-slate-500">Примеры наименований товаров</p>

          {group.exampleProductNames.length === 0 ? (
            <p className="mt-2 text-sm text-slate-500">
              Наименования отсутствуют.
            </p>
          ) : (
            <ul className="mt-3 grid gap-2">
              {group.exampleProductNames.map((productName) => (
                <li
                  key={productName}
                  className="rounded-lg border border-white/10 bg-white/[0.03] px-3 py-2 font-mono text-xs leading-5 text-slate-300"
                >
                  {productName}
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>

      {isUnresolved && (
        <div className="mt-4">
          <div className="rounded-2xl border border-sky-500/25 bg-sky-500/[0.06] p-4">
            <p className="text-sm font-medium text-sky-100">
              Выберите один способ решения
            </p>

            <p className="mt-2 text-sm leading-6 text-sky-200/80">
              Если бренд уже существует в справочнике, создайте Approved alias.
              Если это действительно новый бренд, создайте нового Manufacturer.
              Если значение вообще не является производителем, пометьте его как
              шум. Выполните только одно из трёх действий.
            </p>
          </div>

          <CatalogImportManufacturerAliasCreationForm
            batchId={batchId}
            productTypeId={productTypeId}
            group={group}
            onAnalysisChange={onAnalysisChange}
          />

          <CatalogImportManufacturerCreationForm
            batchId={batchId}
            productTypeId={productTypeId}
            group={group}
            onAnalysisChange={onAnalysisChange}
          />

          <CatalogImportManufacturerNoiseForm
            batchId={batchId}
            productTypeId={productTypeId}
            group={group}
            onAnalysisChange={onAnalysisChange}
          />
        </div>
      )}
    </article>
  );
}

export function CatalogImportManufacturerResolutionPanel({
  batchId,
  productTypeId,
  summary,
  onAnalysisChange,
}: CatalogImportManufacturerResolutionPanelProps) {
  if (summary === null) {
    return (
      <section className="rounded-3xl border border-violet-500/20 bg-violet-500/[0.04] p-6">
        <h2 className="text-xl font-semibold text-white">
          Разбор производителей
        </h2>

        <p className="mt-2 max-w-4xl text-sm leading-6 text-slate-400">
          После запуска анализа здесь появится отдельная сводка по значениям из
          колонки производителя. Она не относится к характеристикам товара и не
          является частью Recognition Shadow Mode.
        </p>

        <div className="mt-5 rounded-2xl border border-sky-500/25 bg-sky-500/[0.06] p-4">
          <p className="text-sm font-medium text-sky-100">
            Справочник производителей пока не изменяется
          </p>

          <p className="mt-2 text-sm leading-6 text-sky-200/80">
            Нажмите «Повторить автораспознавание» или сохраните сопоставление
            колонок. Backend повторно прочитает Excel и вернёт результат
            ManufacturerResolver.
          </p>
        </div>
      </section>
    );
  }

  const unresolvedGroups = summary.groups.filter(
    (group) => group.status === "Unresolved",
  );
  const ignoredNoiseGroups = summary.groups.filter(
    (group) => group.status === "IgnoredNoise",
  );
  const aliasGroups = summary.groups.filter(
    (group) => group.source === "ApprovedAlias",
  );
  const exactNameGroups = summary.groups.filter(
    (group) => group.source === "ExactName",
  );
  const orderedGroups = [
    ...unresolvedGroups,
    ...ignoredNoiseGroups,
    ...aliasGroups,
    ...exactNameGroups,
  ];

  return (
    <section className="rounded-3xl border border-violet-500/25 bg-violet-500/[0.04] p-6">
      <div>
        <h2 className="text-xl font-semibold text-white">
          Разбор производителей
        </h2>

        <p className="mt-2 max-w-4xl text-sm leading-6 text-slate-400">
          Этот раздел показывает, как значения из колонки производителя были
          сопоставлены со справочником Manufacturer и таблицей
          ManufacturerAlias. Здесь производитель рассматривается как отдельная
          сущность каталога, а не как характеристика товара.
        </p>
      </div>

      <div className="mt-5 rounded-2xl border border-green-500/25 bg-green-500/[0.06] p-4">
        <p className="text-sm font-medium text-green-100">
          Режим безопасного чтения
        </p>

        <p className="mt-2 text-sm leading-6 text-green-200/80">
          Анализ ничего не создаёт и не исправляет автоматически. Даже
          неизвестное значение, встретившееся много раз, не станет Manufacturer
          или ManufacturerAlias без явного решения Technical-пользователя.
        </p>
      </div>

      <div className="mt-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
        <ManufacturerSummaryCard
          label="Строк с производителем"
          value={summary.rowsWithManufacturerValueCount}
          description="Количество строк, в которых Excel содержал непустое значение производителя."
        />

        <ManufacturerSummaryCard
          label="Найдено по точному имени"
          value={summary.resolvedByExactNameRowsCount}
          description="Значение Excel совпало с каноническим именем Manufacturer."
          tone="Success"
        />

        <ManufacturerSummaryCard
          label="Найдено через alias"
          value={summary.resolvedByApprovedAliasRowsCount}
          description="Использован подтверждённый ManufacturerAlias."
          tone="Success"
        />

        <ManufacturerSummaryCard
          label="Подтверждённый шум"
          value={summary.ignoredNoiseRowsCount}
          description="Technical подтвердил, что значение не является названием производителя."
          tone={summary.ignoredNoiseRowsCount > 0 ? "Warning" : "Success"}
        />

        <ManufacturerSummaryCard
          label="Не разрешено"
          value={summary.unresolvedRowsCount}
          description="Нет ни точного Manufacturer, ни Approved ManufacturerAlias."
          tone={summary.unresolvedRowsCount > 0 ? "Danger" : "Success"}
        />
      </div>

      <div className="mt-8">
        <div className="flex flex-col justify-between gap-3 lg:flex-row lg:items-end">
          <div>
            <h3 className="text-lg font-semibold text-white">
              Группы исходных значений
            </h3>

            <p className="mt-2 max-w-4xl text-sm leading-6 text-slate-400">
              Одинаковые нормализованные значения объединены в одну карточку.
              Неизвестные группы показаны первыми, затем alias и точные
              совпадения.
            </p>
          </div>

          <div className="flex flex-wrap gap-2">
            <span className="rounded-full border border-red-500/30 bg-red-500/10 px-3 py-1 text-xs text-red-200">
              Неизвестных групп: {unresolvedGroups.length}
            </span>

            <span className="rounded-full border border-violet-500/30 bg-violet-500/10 px-3 py-1 text-xs text-violet-200">
              Alias-групп: {aliasGroups.length}
            </span>

            <span className="rounded-full border border-green-500/30 bg-green-500/10 px-3 py-1 text-xs text-green-200">
              Точных групп: {exactNameGroups.length}
            </span>

            <span className="rounded-full border border-orange-500/30 bg-orange-500/10 px-3 py-1 text-xs text-orange-200">
              Шумовых групп: {ignoredNoiseGroups.length}
            </span>
          </div>
        </div>

        {orderedGroups.length === 0 ? (
          <div className="mt-4 rounded-2xl border border-slate-500/20 bg-black/20 p-5">
            <p className="font-medium text-slate-200">
              Значения производителей отсутствуют
            </p>

            <p className="mt-2 text-sm leading-6 text-slate-400">
              В проанализированных строках не найдено непустых значений в
              колонке производителя.
            </p>
          </div>
        ) : (
          <div className="mt-4 grid gap-4">
            {orderedGroups.map((group) => (
              <ManufacturerResolutionGroupCard
                key={[
                  group.normalizedSourceValue,
                  group.source,
                  group.manufacturerId ?? "none",
                  group.manufacturerAliasId ?? "none",
                  group.manufacturerNoisePhraseId ?? "none",
                ].join("-")}
                batchId={batchId}
                productTypeId={productTypeId}
                group={group}
                onAnalysisChange={onAnalysisChange}
              />
            ))}
          </div>
        )}
      </div>
    </section>
  );
}
