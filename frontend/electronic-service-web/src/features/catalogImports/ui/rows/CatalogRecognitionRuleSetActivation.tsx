"use client";

import { useRef, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { AppButton } from "@/shared/ui/AppButton";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import {
  createRecognitionRuleSetReport,
  getRecognitionRuleSetReport,
  getRecognitionRuleSetState,
  recognitionRuleSetStateQueryRoot,
  switchRecognitionRuleSet,
  type RecognitionRuleSetReport,
  getRecentRecognitionRuleSetReports,
  recognitionRuleSetReportsQueryRoot,
  checkRecognitionRuleSetTraining,
} from "../../api/recognitionRuleSetActivation";

interface Props {
  userId: string;
  batchId: string;
  manufacturerId: string;
  productTypeId: string;
  versionId: string;
  disabled: boolean;
}

function rowStatus(status: string): string {
  switch (status) {
    case "Proposed":
      return "Есть предложенные значения";
    case "Conflict":
      return "Конфликт";
    case "NoMatch":
      return "Нет результата";
    case "OutsideScope":
      return "Другой производитель или тип товара";
    default:
      return status;
  }
}

export function CatalogRecognitionRuleSetActivation({
  userId,
  batchId,
  manufacturerId,
  productTypeId,
  versionId,
  disabled,
}: Props) {
  const client = useQueryClient();
  const requestLock = useRef(false);
  const [busy, setBusy] = useState(false);
  const [report, setReport] = useState<RecognitionRuleSetReport | null>(null);
  const [page, setPage] = useState(1);
  const [reason, setReason] = useState("");
  const [acceptedSequence, setAcceptedSequence] = useState<number | null>(null);
  const [mustRefresh, setMustRefresh] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const stateQuery = useQuery({
    queryKey: [
      ...recognitionRuleSetStateQueryRoot,
      userId,
      manufacturerId,
      productTypeId,
    ],
    queryFn: ({ signal }) =>
      getRecognitionRuleSetState(manufacturerId, productTypeId, signal),
    enabled: !disabled,
    retry: false,
    staleTime: 0,
    refetchOnWindowFocus: false,
  });

  const recentReportsQuery = useQuery({
    queryKey: [
      ...recognitionRuleSetReportsQueryRoot,
      userId,
      versionId,
      batchId,
    ],
    queryFn: ({ signal }) =>
      getRecentRecognitionRuleSetReports(versionId, batchId, signal),
    enabled: !disabled,
    retry: false,
    staleTime: 0,
    refetchOnWindowFocus: false,
  });

  const reportId = report?.id;

  const reportQuery = useQuery({
    queryKey: ["recognition-saved-report", userId, reportId, page],
    queryFn: ({ signal }) => {
      if (!reportId) {
        throw new Error("Отчёт не выбран.");
      }
      return getRecognitionRuleSetReport(reportId, page, signal);
    },
    enabled: !disabled && Boolean(reportId),
    retry: false,
    refetchOnWindowFocus: false,
  });

  const trainingQuery = useQuery({
    queryKey: ["recognition-rule-set-training-check", userId, versionId],
    queryFn: ({ signal }) => checkRecognitionRuleSetTraining(versionId, signal),
    enabled: false,
    retry: false,
    staleTime: 0,
    gcTime: 0,
    refetchOnWindowFocus: false,
  });

  const trainingReady =
    trainingQuery.isSuccess &&
    !trainingQuery.isFetching &&
    trainingQuery.data.versionId === versionId &&
    trainingQuery.data.passedTrainingChecks;

  const currentState = stateQuery.data;
  const isActive = currentState?.activeVersionId === versionId;
  const stateReady =
    stateQuery.isSuccess && !stateQuery.isFetching && !mustRefresh;
  const locked = disabled || busy;

  const reportReady =
    report !== null &&
    report.ruleSetVersionId === versionId &&
    report.batchId === batchId &&
    report.proposedRowsCount > 0 &&
    report.conflictRowsCount === 0 &&
    reportQuery.isSuccess &&
    !reportQuery.isFetching;

  const confirmed =
    stateReady &&
    acceptedSequence !== null &&
    acceptedSequence === currentState?.sequenceNumber;

  async function refreshState(): Promise<void> {
    if (locked || requestLock.current) return;

    requestLock.current = true;
    setBusy(true);
    setAcceptedSequence(null);
    setError(null);

    try {
      const result = await stateQuery.refetch();
      if (result.isError) {
        setMustRefresh(true);
      } else {
        setMustRefresh(false);
      }
    } finally {
      requestLock.current = false;
      setBusy(false);
    }
  }

  async function createReport(): Promise<void> {
    if (locked || requestLock.current) return;

    requestLock.current = true;
    setBusy(true);
    setError(null);
    setMessage(null);
    setAcceptedSequence(null);
    setReport(null);
    setPage(1);

    try {
      const created = await createRecognitionRuleSetReport(versionId, batchId);
      setReport(created);
      setMessage("Отчёт сохранён. Просмотрите результаты перед активацией.");
    } catch (caught) {
      setError(
        getApiErrorMessage(caught, "Не удалось получить отчёт.") +
          " При обрыве связи отчёт мог сохраниться. Автоматического повтора нет.",
      );
    } finally {
      requestLock.current = false;
      setBusy(false);

      // Обновляем и после ошибки связи: отчёт мог сохраниться на сервере.
      void client.invalidateQueries({
        queryKey: [
          ...recognitionRuleSetReportsQueryRoot,
          userId,
          versionId,
          batchId,
        ],
      });
    }
  }

  async function changeActiveVersion(): Promise<void> {
    if (
      locked ||
      requestLock.current ||
      !stateReady ||
      !currentState ||
      !confirmed ||
      !reason.trim() ||
      (!isActive && (!reportReady || !trainingReady))
    ) {
      return;
    }

    requestLock.current = true;
    setBusy(true);
    setError(null);
    setMessage(null);

    try {
      await switchRecognitionRuleSet({
        manufacturerId,
        productTypeId,
        expectedSequenceNumber: currentState.sequenceNumber,
        newVersionId: isActive ? null : versionId,
        reportId: isActive ? null : (report?.id ?? null),
        reason: reason.trim(),
        confirmed: true,
      });

      setMessage(
        isActive
          ? "Версия отключена. Уже сохранённые данные не изменены."
          : "Версия активирована для следующих запусков анализа.",
      );

      setReason("");
    } catch (caught) {
      setError(
        getApiErrorMessage(caught, "Не удалось подтвердить переключение.") +
          " Обновите состояние перед следующей попыткой: запрос мог выполниться.",
      );
    } finally {
      setAcceptedSequence(null);
      setMustRefresh(true);
      requestLock.current = false;
      setBusy(false);

      void client.invalidateQueries({
        queryKey: recognitionRuleSetStateQueryRoot,
      });
    }
  }

  return (
    <section className="grid gap-3 rounded-lg border border-[var(--app-border)] p-3">
      <h5 className="font-semibold">Проверка и активация версии</h5>

      {stateQuery.isFetching && <p role="status">Читаем активную версию…</p>}

      {stateQuery.isError && (
        <p role="alert" className="text-[var(--app-danger)]">
          {getApiErrorMessage(
            stateQuery.error,
            "Не удалось прочитать состояние.",
          )}
        </p>
      )}

      {stateQuery.isSuccess && (
        <p className="break-all">
          {isActive
            ? "Эта версия активна."
            : currentState?.activeVersionId
              ? `Активна другая версия: ${currentState.activeVersionId}`
              : "В этой области нет активной версии."}
        </p>
      )}

      <AppButton
        type="button"
        variant="secondary"
        disabled={locked || stateQuery.isFetching}
        onClick={() => void refreshState()}
      >
        Обновить состояние правил
      </AppButton>

      {mustRefresh && (
        <p>Перед следующим переключением обновите состояние правил.</p>
      )}

      <AppButton
        type="button"
        variant="secondary"
        disabled={locked}
        onClick={() => void createReport()}
      >
        Проверить весь импорт и сохранить отчёт
      </AppButton>

      <p className="text-[var(--app-muted)]">
        Проверяются сохранённые строки, до 2000 строк в пакете. Несохранённые
        изменения формы не учитываются. Проверка не заполняет импорт и не
        активирует правила.
      </p>

      <div className="grid gap-2 rounded-lg border border-[var(--app-border)] p-3">
        <h6 className="font-semibold">Сохранённые отчёты</h6>

        <p className="text-[var(--app-muted)]">
          Последние 20 ваших отчётов для этой версии и этого импорта. Открытие
          отчёта не запускает новую проверку.
        </p>

        <AppButton
          type="button"
          variant="secondary"
          disabled={locked || recentReportsQuery.isFetching}
          onClick={() => void recentReportsQuery.refetch()}
        >
          Обновить список отчётов
        </AppButton>

        {recentReportsQuery.isFetching && (
          <p role="status">Загружаем список отчётов…</p>
        )}

        {recentReportsQuery.isError && (
          <p role="alert" className="text-[var(--app-danger)]">
            {getApiErrorMessage(
              recentReportsQuery.error,
              "Не удалось загрузить список отчётов.",
            )}
          </p>
        )}

        {recentReportsQuery.isSuccess &&
          recentReportsQuery.data.length === 0 && (
            <p>Сохранённых отчётов пока нет.</p>
          )}

        {recentReportsQuery.isSuccess &&
          recentReportsQuery.data.map((savedReport) => (
            <div
              key={savedReport.id}
              className="grid gap-1 rounded-lg border border-[var(--app-border)] p-2"
            >
              <p>
                Проверено:{" "}
                {new Date(savedReport.completedAtUtc).toLocaleString("ru-RU")}
              </p>
              <p>
                Строк: {savedReport.totalRowsCount}; с предложениями:{" "}
                {savedReport.proposedRowsCount}; конфликтов правил:{" "}
                {savedReport.conflictRowsCount}.
              </p>
              <p className="break-all text-[var(--app-muted)]">
                {savedReport.id}
              </p>

              <AppButton
                type="button"
                variant="secondary"
                disabled={locked}
                onClick={() => {
                  if (requestLock.current) return;

                  setAcceptedSequence(null);
                  setPage(1);
                  setReport(savedReport);
                  setError(null);
                  setMessage(
                    "Открыт сохранённый отчёт. Его результаты относятся к моменту проверки.",
                  );
                }}
              >
                {report?.id === savedReport.id
                  ? "Открыть с первой страницы"
                  : "Открыть отчёт"}
              </AppButton>
            </div>
          ))}
      </div>

      {report && (
        <div className="grid gap-2">
          <p className="break-all">Отчёт: {report.id}</p>
          <p>Всего строк: {report.totalRowsCount}</p>
          <p>С предложениями: {report.proposedRowsCount}</p>
          <p>С конфликтами правил: {report.conflictRowsCount}</p>
          <p>Без результата: {report.noMatchRowsCount}</p>
          <p>Вне области правил: {report.outsideScopeRowsCount}</p>
          <p className="text-[var(--app-muted)]">
            Эти числа показывают покрытие, а не доказанную точность. Сравните
            предложенные значения с правильными.
          </p>

          {reportQuery.isFetching && (
            <p role="status">Загружаем строки отчёта…</p>
          )}

          {reportQuery.isError && (
            <>
              <p role="alert" className="text-[var(--app-danger)]">
                {getApiErrorMessage(
                  reportQuery.error,
                  "Не удалось прочитать отчёт.",
                )}
              </p>
              <AppButton
                type="button"
                variant="secondary"
                disabled={locked || reportQuery.isFetching}
                onClick={() => void reportQuery.refetch()}
              >
                Повторить чтение отчёта
              </AppButton>
            </>
          )}

          {reportQuery.isSuccess &&
            reportQuery.data.items.map((item) => (
              <div
                key={item.rowId}
                className="grid gap-1 rounded-lg border border-[var(--app-border)] p-2"
              >
                <p>
                  Строка {item.rowNumber}: {item.productName}
                </p>
                <p>{rowStatus(item.status)}</p>
                {(item.comparisons ?? []).map((comparison) => (
                  <p key={comparison.characteristicDefinitionId}>
                    {comparison.name}: сохранено{" "}
                    {comparison.currentValue ?? "—"}; предложено{" "}
                    {comparison.proposedValue ?? "—"}
                  </p>
                ))}
              </div>
            ))}

          <div className="flex items-center gap-2">
            <AppButton
              type="button"
              variant="secondary"
              disabled={locked || reportQuery.isFetching || page <= 1}
              onClick={() => {
                setAcceptedSequence(null);
                setPage(page - 1);
              }}
            >
              Назад
            </AppButton>
            <span>Страница {page}</span>
            <AppButton
              type="button"
              variant="secondary"
              disabled={
                locked ||
                reportQuery.isFetching ||
                !reportQuery.isSuccess ||
                !reportQuery.data.hasMore
              }
              onClick={() => {
                setAcceptedSequence(null);
                setPage(page + 1);
              }}
            >
              Далее
            </AppButton>
          </div>
        </div>
      )}

      <div className="grid gap-2 rounded-lg border border-[var(--app-border)] p-3">
        <h6 className="font-semibold">Проверка учебных примеров</h6>

        <p className="text-[var(--app-muted)]">
          Проверяем, проходят ли шаблоны версии проверку на действующих
          подтверждённых примерах. Отозванные или изменившиеся основания могут
          потребовать создания нового черновика.
        </p>

        <AppButton
          type="button"
          variant="secondary"
          disabled={locked || trainingQuery.isFetching}
          onClick={() => {
            if (requestLock.current) return;

            setAcceptedSequence(null);
            void trainingQuery.refetch();
          }}
        >
          Проверить учебные примеры версии
        </AppButton>

        {trainingQuery.isFetching && (
          <p role="status">Проверяем учебные примеры…</p>
        )}

        {trainingQuery.isError && (
          <p role="alert" className="text-[var(--app-danger)]">
            {getApiErrorMessage(
              trainingQuery.error,
              "Не удалось проверить учебные примеры.",
            )}
          </p>
        )}

        {trainingQuery.isSuccess && (
          <>
            <p className="font-medium">
              {trainingQuery.data.passedTrainingChecks
                ? "Все шаблоны прошли проверку учебных примеров."
                : "Проверка не пройдена. Причины указаны ниже."}
            </p>

            <p className="text-[var(--app-muted)]">
              Проверено:{" "}
              {new Date(trainingQuery.data.checkedAtUtc).toLocaleString(
                "ru-RU",
              )}
            </p>

            {trainingQuery.data.items.map((item) => (
              <div
                key={`${item.ruleKind}:${item.draftId}`}
                className="grid gap-1 rounded-lg border border-[var(--app-border)] p-2"
              >
                <p>
                  {item.passed ? "Проверка пройдена" : "Требуется исправление"}
                </p>
                <p>{item.message}</p>
                <p className="break-all text-[var(--app-muted)]">
                  Черновик: {item.draftId}
                </p>
              </div>
            ))}

            <p className="text-[var(--app-muted)]">
              Это результат на момент проверки, а не оценка точности на новых
              товарах. При активации сервер повторит проверку.
            </p>
          </>
        )}
      </div>

      <label className="grid gap-1">
        Причина переключения
        <textarea
          className="rounded-lg border border-[var(--app-border)] bg-transparent p-2"
          value={reason}
          maxLength={1000}
          disabled={locked}
          onChange={(event) => setReason(event.target.value)}
        />
      </label>

      <label className="flex items-start gap-2">
        <input
          type="checkbox"
          checked={confirmed}
          disabled={
            locked ||
            !stateReady ||
            (!isActive && (!reportReady || !trainingReady))
          }
          onChange={(event) =>
            setAcceptedSequence(
              event.target.checked && currentState
                ? currentState.sequenceNumber
                : null,
            )
          }
        />
        <span>
          {isActive
            ? "Подтверждаю отключение этой версии для следующих запусков."
            : "Результаты проверены. Подтверждаю замену активной версии в этой области."}
        </span>
      </label>

      <AppButton
        type="button"
        disabled={
          locked ||
          !confirmed ||
          !reason.trim() ||
          (!isActive && (!reportReady || !trainingReady))
        }
        onClick={() => void changeActiveVersion()}
      >
        {isActive ? "Отключить эту версию" : "Активировать эту версию"}
      </AppButton>

      {busy && <p role="status">Выполняем запрос…</p>}
      {message && <p role="status">{message}</p>}
      {error && (
        <p role="alert" className="text-[var(--app-danger)]">
          {error}
        </p>
      )}
    </section>
  );
}
