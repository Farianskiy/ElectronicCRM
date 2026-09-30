import type { CatalogImportRecognitionShadowConflictGroup } from "../../model/types";

interface CatalogImportRecognitionConflictSummaryProps {
  conflictGroups: CatalogImportRecognitionShadowConflictGroup[];
  totalConflictsCount: number;
}

interface RecognitionSourcePresentation {
  label: string;
  className: string;
}

function formatConfidence(confidence: number): string {
  return `${(confidence * 100).toFixed(1)}%`;
}

function getRecognitionSourcePresentation(
  recognitionSource: string,
): RecognitionSourcePresentation {
  switch (recognitionSource) {
    case "Dictionary":
      return {
        label: "Словарь",
        className: "border-violet-500/40 bg-violet-500/10 text-violet-200",
      };

    case "Rule":
      return {
        label: "Правило",
        className: "border-blue-500/40 bg-blue-500/10 text-blue-200",
      };

    case "Heuristic":
      return {
        label: "Эвристика",
        className: "border-amber-500/40 bg-amber-500/10 text-amber-200",
      };

    case "MachineLearning":
      return {
        label: "Машинное обучение",
        className: "border-fuchsia-500/40 bg-fuchsia-500/10 text-fuchsia-200",
      };

    case "None":
      return {
        label: "Источник не определён",
        className: "border-slate-500/40 bg-slate-500/10 text-slate-300",
      };

    default:
      return {
        label: recognitionSource,
        className: "border-slate-500/40 bg-slate-500/10 text-slate-300",
      };
  }
}

function CatalogImportRecognitionConflictGroupCard({
  conflictGroup,
}: {
  conflictGroup: CatalogImportRecognitionShadowConflictGroup;
}) {
  const recognitionSourcePresentation = getRecognitionSourcePresentation(
    conflictGroup.recognitionSource,
  );

  const spanEnd = conflictGroup.spanStart + conflictGroup.spanLength;

  return (
    <article className="rounded-2xl border border-red-500/25 bg-red-500/[0.035] p-5">
      <div className="flex flex-col gap-4 xl:flex-row xl:items-start xl:justify-between">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <span className="rounded-full border border-red-500/40 bg-red-500/10 px-3 py-1 text-xs font-semibold text-red-200">
              Конфликт Excel и наименования
            </span>

            <span
              className={`rounded-full border px-3 py-1 text-xs font-semibold ${recognitionSourcePresentation.className}`}
            >
              Источник: {recognitionSourcePresentation.label}
            </span>
          </div>

          <h4 className="mt-4 text-lg font-semibold text-slate-100">
            {conflictGroup.characteristicName}
          </h4>

          <p className="mt-1 font-mono text-xs text-cyan-300">
            {conflictGroup.characteristicCode}
          </p>
        </div>

        <div className="rounded-xl border border-red-500/30 bg-red-500/10 px-5 py-3 text-center">
          <div className="text-xs text-red-200">Количество строк</div>

          <div className="mt-1 text-2xl font-semibold text-white">
            {conflictGroup.occurrenceCount.toLocaleString("ru-RU")}
          </div>
        </div>
      </div>

      <div className="mt-5 grid gap-3 md:grid-cols-2 xl:grid-cols-4">
        <div className="rounded-xl border border-slate-700 bg-slate-950/30 p-4">
          <div className="text-xs text-slate-500">Значение в Excel</div>

          <div className="mt-2 break-words font-mono text-base font-semibold text-red-200">
            {conflictGroup.excelValue}
          </div>
        </div>

        <div className="rounded-xl border border-slate-700 bg-slate-950/30 p-4">
          <div className="text-xs text-slate-500">Итог Recognition Engine</div>

          <div className="mt-2 break-words font-mono text-base font-semibold text-emerald-200">
            {conflictGroup.recognizedValue}
          </div>
        </div>

        <div className="rounded-xl border border-slate-700 bg-slate-950/30 p-4">
          <div className="text-xs text-slate-500">
            Исходный найденный фрагмент
          </div>

          <div className="mt-2 break-words font-mono text-base font-semibold text-slate-100">
            {conflictGroup.rawRecognizedValue}
          </div>
        </div>

        <div className="rounded-xl border border-slate-700 bg-slate-950/30 p-4">
          <div className="text-xs text-slate-500">Уверенность</div>

          <div className="mt-2 font-mono text-base font-semibold text-slate-100">
            {formatConfidence(conflictGroup.confidence)}
          </div>
        </div>
      </div>

      <div className="mt-3 grid gap-3 md:grid-cols-3">
        <div className="rounded-xl border border-slate-700 bg-slate-950/30 p-4">
          <div className="text-xs text-slate-500">Span — позиции фрагмента</div>

          <div className="mt-2 font-mono text-sm font-semibold text-slate-100">
            {conflictGroup.spanStart}..{spanEnd}
          </div>

          <p className="mt-2 text-xs leading-5 text-slate-500">
            Начальная позиция включается, конечная позиция не включается. Длина
            фрагмента: {conflictGroup.spanLength}.
          </p>
        </div>

        <div className="rounded-xl border border-slate-700 bg-slate-950/30 p-4">
          <div className="text-xs text-slate-500">Приоритет кандидата</div>

          <div className="mt-2 font-mono text-sm font-semibold text-slate-100">
            {conflictGroup.priority}
          </div>

          <p className="mt-2 text-xs leading-5 text-slate-500">
            Используется при выборе между несколькими кандидатами одного
            источника.
          </p>
        </div>

        <div className="rounded-xl border border-slate-700 bg-slate-950/30 p-4">
          <div className="text-xs text-slate-500">Строки Excel в примере</div>

          <div className="mt-2 break-words font-mono text-sm font-semibold text-slate-100">
            {conflictGroup.exampleRowNumbers.length > 0
              ? conflictGroup.exampleRowNumbers.join(", ")
              : "Нет примеров"}
          </div>

          <p className="mt-2 text-xs leading-5 text-slate-500">
            Backend сохраняет ограниченное количество номеров строк, чтобы ответ
            API не становился слишком большим.
          </p>
        </div>
      </div>

      <div className="mt-3 rounded-xl border border-slate-700 bg-slate-950/30 p-4">
        <div className="text-xs text-slate-500">
          RecognizerKey — точное доказательство распознавания
        </div>

        <div className="mt-2 break-all font-mono text-sm text-cyan-200">
          {conflictGroup.recognizerKey}
        </div>

        <p className="mt-2 text-xs leading-5 text-slate-500">
          По этому ключу можно определить конкретный словарный термин, профиль
          или правило, которое создало результат.
        </p>
      </div>

      <div className="mt-3 rounded-xl border border-slate-700 bg-slate-950/30 p-4">
        <div className="text-sm font-semibold text-slate-200">
          Примеры наименований
        </div>

        {conflictGroup.exampleProductNames.length > 0 ? (
          <ul className="mt-3 space-y-2">
            {conflictGroup.exampleProductNames.map(
              (productName, productNameIndex) => (
                <li
                  key={`${conflictGroup.characteristicCode}-${conflictGroup.excelValue}-${conflictGroup.recognizedValue}-${productNameIndex}`}
                  className="rounded-lg border border-slate-800 bg-slate-950/40 px-4 py-3 font-mono text-sm text-slate-200"
                >
                  {productName}
                </li>
              ),
            )}
          </ul>
        ) : (
          <p className="mt-3 text-sm text-slate-500">
            Примеры наименований отсутствуют.
          </p>
        )}
      </div>
    </article>
  );
}

export function CatalogImportRecognitionConflictSummary({
  conflictGroups,
  totalConflictsCount,
}: CatalogImportRecognitionConflictSummaryProps) {
  const groupedOccurrencesCount = conflictGroups.reduce(
    (currentCount, conflictGroup) =>
      currentCount + conflictGroup.occurrenceCount,
    0,
  );

  return (
    <section className="mt-10">
      <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
        <div>
          <h3 className="text-xl font-semibold text-slate-100">
            Сводка конфликтов
          </h3>

          <p className="mt-2 max-w-4xl text-sm leading-6 text-slate-400">
            Одинаковые конфликты объединены в группы по характеристике, значению
            Excel, распознанному значению, источнику и RecognizerKey. Поэтому
            сотни одинаковых строк больше не приходится анализировать по одной.
          </p>
        </div>

        <div className="flex flex-wrap gap-2">
          <span className="rounded-full border border-red-500/30 bg-red-500/10 px-3 py-1 text-xs font-semibold text-red-200">
            Всего конфликтов: {totalConflictsCount.toLocaleString("ru-RU")}
          </span>

          <span className="rounded-full border border-cyan-500/30 bg-cyan-500/10 px-3 py-1 text-xs font-semibold text-cyan-200">
            Групп: {conflictGroups.length.toLocaleString("ru-RU")}
          </span>

          <span className="rounded-full border border-slate-600 bg-slate-800/50 px-3 py-1 text-xs font-semibold text-slate-300">
            Сгруппировано строк:{" "}
            {groupedOccurrencesCount.toLocaleString("ru-RU")}
          </span>
        </div>
      </div>

      {conflictGroups.length > 0 ? (
        <div className="mt-5 space-y-4">
          {conflictGroups.map((conflictGroup) => (
            <CatalogImportRecognitionConflictGroupCard
              key={`${conflictGroup.characteristicCode}-${conflictGroup.excelValue}-${conflictGroup.recognizedValue}-${conflictGroup.recognitionSource}-${conflictGroup.recognizerKey}`}
              conflictGroup={conflictGroup}
            />
          ))}
        </div>
      ) : totalConflictsCount === 0 ? (
        <div className="mt-5 rounded-2xl border border-emerald-500/30 bg-emerald-500/10 p-5">
          <div className="font-semibold text-emerald-200">
            Конфликтов не обнаружено
          </div>

          <p className="mt-2 text-sm leading-6 text-emerald-100/70">
            Значения Excel не противоречат результатам Recognition Engine.
          </p>
        </div>
      ) : (
        <div className="mt-5 rounded-2xl border border-amber-500/30 bg-amber-500/10 p-5">
          <div className="font-semibold text-amber-200">
            Конфликты есть, но агрегированная сводка не получена
          </div>

          <p className="mt-2 text-sm leading-6 text-amber-100/70">
            Перезапустите backend и повторно запустите анализ импорта. Такая
            ситуация возможна, если frontend получил старый ответ API без поля
            conflictGroups.
          </p>
        </div>
      )}

      <div className="mt-5 rounded-2xl border border-blue-500/25 bg-blue-500/[0.06] p-5">
        <div className="font-semibold text-blue-200">
          Как использовать эту сводку
        </div>

        <p className="mt-2 text-sm leading-6 text-blue-100/70">
          Сводка не решает, какое значение правильное. Она показывает
          повторяющиеся закономерности и техническое доказательство каждого
          распознавания. После этого Technical-пользователь решает: исправлять
          исходный Excel, менять словарь, уточнять область действия термина или
          разделять характеристику на несколько разных понятий.
        </p>
      </div>
    </section>
  );
}
