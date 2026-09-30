"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import Link from "next/link";
import { AppButton } from "@/shared/ui/AppButton";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import {
  getEvaluationReport,
  type EvaluationMetrics,
} from "../api/evaluationReports";
import { LearningProvenancePanel } from "./LearningProvenancePanel";

const states: Record<string, string> = {
  Ready: "Готово к включению",
  Failed: "Проверка не пройдена",
  Stale: "Отчёт устарел",
  InsufficientData: "Недостаточно контрольных примеров",
  Historical: "Воспроизведение исторического снимка",
};
const outcomes: Record<string, string> = {
  Correct: "Правильно",
  Incorrect: "Неправильно",
  Missing: "Нет значения",
  Conflict: "Конфликт",
  Improved: "Исправлено",
  Regressed: "Ухудшено",
  Unchanged: "Без изменения",
  Changed: "Изменено",
};

function Metrics({ metrics }: { metrics: EvaluationMetrics }) {
  return (
    <div className="overflow-x-auto">
      <p>
        Имен: {metrics.names}. Исправлений: {metrics.improvements}. Регрессий:{" "}
        {metrics.regressions}.
      </p>
      <table className="w-full text-left text-sm">
        <thead>
          <tr>
            <th>Результат</th>
            <th>Текущая конфигурация</th>
            <th>Предлагаемая</th>
          </tr>
        </thead>
        <tbody>
          {(["correct", "incorrect", "missing", "conflicts"] as const).map(
            (key, i) => (
              <tr key={key}>
                <td>
                  {["Правильно", "Неправильно", "Нет значения", "Конфликт"][i]}
                </td>
                {[metrics.current, metrics.candidate].map((value, side) => (
                  <td key={side}>
                    {value[key]} / {value.total} (
                    {value.total
                      ? `${((value[key] / value.total) * 100).toFixed(1)}%`
                      : "нет данных"}
                    )
                  </td>
                ))}
              </tr>
            ),
          )}
        </tbody>
      </table>
    </div>
  );
}

export function EvaluationReportPanel({
  reportId,
  kind = "rules",
}: {
  reportId: string;
  kind?: "rules" | "dictionary";
}) {
  const [page, setPage] = useState(1);
  const [replay, setReplay] = useState(false);
  const [example, setExample] = useState<string | null>(null);
  const query = useQuery({
    queryKey:
      kind === "rules"
        ? ["recognition-evaluation", reportId, page, replay]
        : ["dictionary-evaluation", reportId, page, replay],
    queryFn: () => getEvaluationReport(reportId, page, replay, kind),
    retry: false,
  });
  const data = query.data;
  return (
    <section className="grid gap-3 rounded border border-[var(--app-border)] p-4">
      <div>
        <h3 className="font-semibold">Сравнительная оценка качества</h3>

        <p className="mt-1 text-sm text-[var(--app-muted)]">
          Отчёт сравнивает распознавание до изменения и с предложенным
          изменением. Перед выпуском сервер повторно проверяет его актуальность.
        </p>

        <details className="mt-2 text-xs text-[var(--app-muted)]">
          <summary className="cursor-pointer">Технические сведения</summary>
          <p className="mt-2 break-all">Идентификатор отчёта: {reportId}</p>
        </details>
      </div>

      {query.error && (
        <p role="alert">
          {getApiErrorMessage(query.error, "Не удалось прочитать оценку.")}
        </p>
      )}
      {query.isFetching && <p role="status">Проверяем отчёт…</p>}
      <div className="flex flex-wrap gap-2">
        <AppButton
          variant="secondary"
          disabled={query.isFetching}
          onClick={() => void query.refetch()}
        >
          Обновить проверку
        </AppButton>
        <AppButton
          variant="secondary"
          disabled={query.isFetching}
          onClick={() => {
            setReplay(!replay);
            setPage(1);
          }}
        >
          {replay
            ? "Вернуться к актуальности отчёта"
            : "Воспроизвести сохранённый снимок"}
        </AppButton>
      </div>
      {data && !query.isError && (
        <>
          <section className="grid gap-2 rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4">
            <p className="font-semibold">
              {data.usedForApproval && !replay
                ? "Изменение уже выпущено"
                : kind === "dictionary" && data.readiness.state === "Ready"
                  ? "Готово к одобрению словарного решения"
                  : (states[data.readiness.state] ?? data.readiness.state)}
            </p>

            {data.readiness.state === "Stale" &&
              !data.usedForApproval &&
              !replay && (
                <p className="text-sm text-[var(--app-muted)]">
                  После создания отчёта изменились правила, словарь, основания
                  или проверяемое решение. Этот отчёт остаётся в истории, но для
                  выпуска необходимо создать новую оценку.
                </p>
              )}

            {data.readiness.state === "InsufficientData" && !replay && (
              <p className="text-sm text-[var(--app-muted)]">
                Для безопасного сравнения пока недостаточно подтверждений,
                назначенных для контроля правил.
              </p>
            )}

            {replay && (
              <p className="text-sm text-[var(--app-muted)]">
                Сейчас показан сохранённый исторический снимок. Он объясняет
                результат прошлой оценки, но не разрешает новый выпуск.
              </p>
            )}

            {data.usedForApproval && !replay && (
              <p className="text-sm text-[var(--app-muted)]">
                Этот отчёт связан с уже выполненным выпуском и подтверждает его
                историю. Для другого выпуска потребуется новая оценка.
              </p>
            )}
          </section>

          {data.manufacturerName && (
            <p>
              Производитель и тип товара: {data.manufacturerName} ·{" "}
              {data.productTypeName}
            </p>
          )}

          {!data.usedForApproval &&
            data.readiness.reasons.map((reason, index) => (
              <p
                key={`${reason.code}-${index}`}
                className="rounded-lg border border-[var(--app-border)] p-3 text-sm"
              >
                {reason.message}
              </p>
            ))}

          <p>
            На момент оценки использовалась{" "}
            {data.currentVersionId
              ? "действовавшая тогда версия правил"
              : "базовая конфигурация без активной версии правил"}
            .
          </p>

          {kind === "rules" && (
            <p>
              Отчёт оценивает выбранную версию правил относительно этой
              конфигурации.
            </p>
          )}

          {data.decisionSummary && (
            <p>Проверенное словарное решение: {data.decisionSummary}</p>
          )}

          <details className="text-xs text-[var(--app-muted)]">
            <summary className="cursor-pointer">
              Идентификаторы сравниваемых конфигураций
            </summary>

            <div className="mt-2 grid gap-1 break-all">
              <p>
                Действовавшая версия:{" "}
                {data.currentVersionId ?? "базовая конфигурация"}
              </p>
              <p>Проверяемое изменение: {data.candidateVersionId}</p>
            </div>
          </details>
          <p>
            Подтверждений: {data.inputExamples}; повторов:{" "}
            {data.duplicateExamples}; противоречий разметки:{" "}
            {data.labelConflicts}; единиц с учебными пересечениями:{" "}
            {data.overlapUnits}.
          </p>
          <p>
            Учебные основания на момент отчёта:{" "}
            {data.trainingPassed == null
              ? "нет данных"
              : data.trainingPassed
                ? "проверены"
                : "не прошли проверку"}
            .
          </p>
          {data.policy && (
            <p>
              Минимум: {data.policy.minimumControlNames} контрольных имён,{" "}
              {data.policy.minimumControlUnits} значений,{" "}
              {data.policy.minimumImprovements} исправлений. Регрессии и
              увеличение ошибок/конфликтов запрещены.
            </p>
          )}
          <h4 className="font-semibold">
            Контроль без известных пересечений с основаниями
          </h4>
          <p className="text-sm">
            Это не доказательство полной независимости: происхождение всех
            исторических записей словаря может быть неизвестно. Учебная проверка
            и предложения в импорте не являются точностью.
          </p>
          {data.control && <Metrics metrics={data.control} />}
          {data.characteristics.map((c) => (
            <details key={c.characteristicId}>
              <summary>Характеристика {c.characteristicId}</summary>
              <Metrics metrics={c.metrics} />
            </details>
          ))}
          {data.trainingDiagnostic && (
            <details>
              <summary>
                Диагностика имён с пересечением учебных оснований
              </summary>
              <Metrics metrics={data.trainingDiagnostic} />
            </details>
          )}
          <Link
            href={`/catalog/recognition/learning?${new URLSearchParams({ manufacturerId: data.manufacturerId, productTypeId: data.productTypeId, branch: "examples" })}`}
          >
            Назначить подтверждения для контроля
          </Link>
          {data.items.map((row, i) => (
            <article
              key={`${page}-${i}`}
              className="grid gap-1 rounded border border-[var(--app-border)] p-2"
            >
              <p>
                {row.productName} · {row.characteristicCode} · ожидается:{" "}
                {row.expectedValues.join(" / ")}
              </p>
              <p>
                {outcomes[row.current.status]} {row.current.value} →{" "}
                {outcomes[row.candidate.status]} {row.candidate.value}:{" "}
                {outcomes[row.change]}
              </p>
              {row.trainingOverlap && (
                <p>Учебное пересечение — не включено в контроль.</p>
              )}
              {row.labelConflict && (
                <p>Противоречивая разметка — блокирует включение.</p>
              )}
              {row.sources.map((source, sourceIndex) => (
                <button
                  key={source.exampleId}
                  type="button"
                  className="text-left underline"
                  onClick={() => setExample(source.exampleId)}
                >
                  Открыть происхождение контрольного примера {sourceIndex + 1}
                </button>
              ))}
            </article>
          ))}
          {example && (
            <LearningProvenancePanel key={example} exampleId={example} />
          )}
          <div className="flex items-center gap-2">
            <AppButton
              variant="secondary"
              disabled={page === 1 || query.isFetching}
              onClick={() => setPage(page - 1)}
            >
              Назад
            </AppButton>
            <span>Страница {page}</span>
            <AppButton
              variant="secondary"
              disabled={page * 25 >= data.total || query.isFetching}
              onClick={() => setPage(page + 1)}
            >
              Далее
            </AppButton>
          </div>
        </>
      )}
    </section>
  );
}
