"use client";

import { EvaluationReportPanel } from "./EvaluationReportPanel";
import { useState } from "react";
import Link from "next/link";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AppButton } from "@/shared/ui/AppButton";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { excludeObservation, getLearningProvenance, type LearningProvenance } from "../api/learningProvenance";

const stateLabels: Record<string, string> = {
  Active: "активен", Revoked: "подтверждение отозвано", SourceExcluded: "источник исключён",
  Pending: "ожидает решения", Approved: "одобрено", Rejected: "отклонено",
};
const kindLabels: Record<string, string> = {
  Literal: "Точное совпадение", NumericCapture: "Числовой шаблон", MultipleNumericCaptures: "Шаблон нескольких чисел",
};

export function LearningProvenancePanel({ exampleId }: { exampleId: string }) {
  const client = useQueryClient();
  const [dictionaryReport, setDictionaryReport] = useState<string | null>(null);
  const [open, setOpen] = useState(true);
  const [page, setPage] = useState(1);
  const [confirm, setConfirm] = useState(false);
  const [reason, setReason] = useState("");
  const [actual, setActual] = useState<LearningProvenance | null>(null);
  const query = useQuery({ queryKey: ["learning-provenance", exampleId, page], queryFn: () => getLearningProvenance(exampleId, page), enabled: open });
  const exclude = useMutation({
    mutationFn: () => excludeObservation(query.data!.feedbackId, reason.trim()),
    onSuccess: async data => {
      setActual(data); setConfirm(false); setPage(1);
      await client.invalidateQueries();
    },
  });
  const data = actual ?? query.data;
  const error = query.error ?? exclude.error;
  return <div className="grid gap-3">
    <AppButton variant="secondary" onClick={() => setOpen(!open)}>Использование в обучении</AppButton>
    {open && <div className="grid gap-3 rounded border border-[var(--app-border)] p-3">
      {error && <p role="alert">{getApiErrorMessage(error, "Не удалось получить происхождение.")}</p>}
      {query.isPending && <p>Загрузка связей…</p>}
      {data && <>
        {actual && <p role="status">Наблюдение исключено. Ниже — фактические зависимости на момент выполнения.</p>}
        <p>Наблюдение: {data.feedbackId}</p>
        {data.excludedAtUtc && <p>Источник исключён: {new Date(data.excludedAtUtc).toLocaleString("ru-RU")}. {data.exclusionReason}</p>}
        <p>Связано примеров: {data.examples.total}, кандидатов: {data.candidates.total}, связей с черновиками: {data.drafts.total}, связей с версиями: {data.versions.total}.</p>
        <h4 className="font-semibold">Наблюдение → кандидаты → словарь</h4>
        {!data.candidates.total && <p>Связь не сохранена.</p>}
        {data.candidates.items.map(c => <div key={c.id} className="break-all">
          <p>Кандидат {c.id}: наблюдений {c.occurrenceCount}, разных имён {c.distinctProductCount}. {c.sufficientEvidence ? "Оснований достаточно" : "Оснований недостаточно"}.</p>
          {c.suggestionId && <p>Предложение {c.suggestionId}: {stateLabels[c.suggestionStatus ?? ""] ?? c.suggestionStatus}. <Link href="/catalog/assistant-suggestions">Открыть предложения</Link></p>}
          {c.evaluationReportId && <button type="button" className="underline" onClick={() => setDictionaryReport(c.evaluationReportId)}>Отчёт выпуска словаря {c.evaluationReportId}</button>}
          {c.dictionaryTermId && <p>Выпущен термин {c.dictionaryTermId}. <Link href="/catalog/recognition#dictionary-management">Управление словарём</Link> {c.needsReview && "Основания изменились — требуется пересмотр."}</p>}
        </div>)}
        {dictionaryReport && <EvaluationReportPanel key={dictionaryReport} reportId={dictionaryReport} kind="dictionary" />}
        <h4 className="font-semibold">Примеры → черновики → версии</h4>
        {data.examples.items.map(e => <p key={e.id}>Пример {e.id}: {stateLabels[e.state] ?? e.state}</p>)}
        {!data.drafts.total && <p>Связь с черновиками не сохранена.</p>}
        {data.drafts.items.map(d => <p key={`${d.kind}-${d.id}-${d.exampleId}`} className="break-all">{kindLabels[d.kind] ?? d.kind}, черновик {d.id} ← пример {d.exampleId}: {d.supporting ? "поддерживал правило" : "входил в проверочную выборку"}.</p>)}
        {data.versions.items.map(v => <p key={`${v.id}-${v.draftId}`} className="break-all">Версия {v.id} ← черновик {v.draftId}: {v.active ? "действует" : "не активна"}. {v.needsReview && "Основания изменились — требуется пересмотр."}</p>)}
        {data.versions.total > 0 && <Link href={data.importBatchId ? `/catalog/imports/${data.importBatchId}` : "/catalog/imports"}>Проверка и переключение правил в импорте</Link>}
        {data.switches.items.map(s => <p key={s.id}>Переключение №{s.sequenceNumber}: {s.previousVersionId ?? "без версии"} → {s.newVersionId ?? "без версии"}</p>)}
        <div className="flex gap-2"><AppButton variant="secondary" disabled={page === 1} onClick={() => { setActual(null); setPage(page - 1); }}>Назад</AppButton><span>Страница {page}</span><AppButton variant="secondary" disabled={Math.max(data.examples.total, data.candidates.total, data.drafts.total, data.versions.total, data.switches.total) <= page * 25} onClick={() => { setActual(null); setPage(page + 1); }}>Далее</AppButton></div>
        {data.canExclude && !data.excludedAtUtc && <AppButton variant="secondary" onClick={() => setConfirm(true)}>Исключить наблюдение из обучения</AppButton>}
        {confirm && <div className="grid gap-2">
          <p>Все связанные примеры перестанут участвовать в новых учебных выборках. Счётчики кандидатов будут пересчитаны. Товары, выпущенные термины и действующие версии сохранятся до отдельного решения. Перечень выше предварительный; при выполнении он проверяется заново.</p>
          <label>Причина исключения<textarea className="block w-full border bg-transparent p-2" value={reason} maxLength={1000} onChange={e => setReason(e.target.value)} /></label>
          <AppButton disabled={!reason.trim() || exclude.isPending} onClick={() => exclude.mutate()}>Подтвердить исключение</AppButton>
          <AppButton variant="secondary" disabled={exclude.isPending} onClick={() => setConfirm(false)}>Отмена</AppButton>
        </div>}
      </>}
    </div>}
  </div>;
}
